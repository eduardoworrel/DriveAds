using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.RegularExpressions;
using webapp.Data;

public class AdditionalData
{
    public int Quantity { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string Demographic1 { get; set; } = string.Empty;
    public string Demographic2 { get; set; } = string.Empty;
    public string Demographic3 { get; set; } = string.Empty;
    public List<Guid> LastTwoAdIds { get; set; } = new List<Guid>(); // Armazena os IDs dos últimos dois anúncios
}

public class Orquestrator
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private ApplicationDbContext? _db = null;
    private string _key = string.Empty;

    private AdditionalData _additionalData { get; set; } = new AdditionalData();
    
    public enum ClientState
    {
        AguardandoPassageiro,
        ColetandoDados,
        BuscandoPropaganda,
        PropagandaReproduzida
    }

    public class ClientData
    {
        public ClientState State { get; set; }
        public AdditionalData AdditionalData { get; set; }
    }

    public Orquestrator(string redisConnectionString)
    {
        _redis = ConnectionMultiplexer.Connect(redisConnectionString);
        _database = _redis.GetDatabase();
    }

    public async Task<string> StartOrContinueProcess(string id, MemoryStream stream, ApplicationDbContext db, string key)
    {
        var clientStateKey = GetRedisKeyForClient(id);
        var clientDataJson = _database.StringGet(clientStateKey);
        _db = db;
        _key = key;

        ClientData clientData;

        if (clientDataJson.IsNullOrEmpty)
        {
            clientData = new ClientData
            {
                State = ClientState.AguardandoPassageiro,
                AdditionalData = _additionalData
            };
            SaveClientData(id, clientData);
        }
        else
        {
            clientData = JsonSerializer.Deserialize<ClientData>(clientDataJson);
        }

        switch (clientData.State)
        {
            case ClientState.AguardandoPassageiro:
                return await HandleAguardandoPassageiro(id, stream, clientData);

            case ClientState.ColetandoDados:
                return await HandleColetandoDados(id, stream, clientData);
            default:
                throw new InvalidOperationException("Estado desconhecido");
        }
    }

    // Métodos para processar cada estado
    private async Task<string> HandleAguardandoPassageiro(string id, MemoryStream stream, ClientData clientData)
    {
        Console.WriteLine($"Cliente {id} aguardando passageiro...");

        var images = await VideoToImageService.ConvertVideoFragmentToImagesAsync(stream.ToArray());
        Console.WriteLine($"images..."+images.Count);

        string prompt = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompt1.txt"));

        var result = await LlamaService.SendImagesToApiAsync(_key, images, prompt);
       try
       {
            result = Regex.Replace(result.Trim(), @"\D", "");
            var quantity = int.Parse(result);
            if(quantity > 0)
            {
                clientData.AdditionalData.Quantity = quantity;
                UpdateClientState(id, ClientState.ColetandoDados, clientData.AdditionalData);
            }
       }
       catch
       {
            Console.WriteLine("erro result:" + result);
       }
       return "free";
    }

    private async Task<string> HandleColetandoDados(string id, MemoryStream stream, ClientData clientData)
    {
        var images = await VideoToImageService.ConvertVideoFragmentToImagesAsync(stream.ToArray());
        Console.WriteLine($"images2..."+images.Count);

        string prompt = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompt2.txt"));

        var result = await LlamaService.SendImagesToApiAsync(_key, images, prompt);

        if (clientData.AdditionalData.Demographic1 == string.Empty)
        {
            clientData.AdditionalData.Demographic1 = result;
            UpdateClientState(id, ClientState.ColetandoDados, clientData.AdditionalData);
            return "free";
        }
        else if (clientData.AdditionalData.Demographic2 == string.Empty)
        {
            clientData.AdditionalData.Demographic2 = result;
            UpdateClientState(id, ClientState.ColetandoDados, clientData.AdditionalData);
            return "free";
        }
        else
        {
            clientData.AdditionalData.Demographic3 = result;
            prompt = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompt3.txt"));
            
            var advertisements = await _db.Advertisements.Take(100).ToListAsync();

            // Filtra anúncios para evitar os dois últimos reproduzidos
            advertisements = advertisements.Where(ad => !clientData.AdditionalData.LastTwoAdIds.Contains(ad.Id)).ToList();

            string advertisementsJoined = string.Join(Environment.NewLine + Environment.NewLine, advertisements.ConvertAll(o =>
                $@"ID:{o.Id}, Text:{o.Text}, Preferences:{o.Pet}, when:{o.Times}, where:{o.Where}, Ages:{o.Ages};"
            ));

            var demographic = @$"
{clientData.AdditionalData.Demographic1}.
Now is: {clientData.AdditionalData.Time}:
            ";
            var finalPrompt = string.Format(prompt, clientData.AdditionalData.Quantity, demographic, advertisementsJoined);
            var trying = 0;
            Guid selection = Guid.Empty;
            while(trying < 4 && selection == Guid.Empty){

                var finalResult = await LlamaService.ChatAsync(_key, finalPrompt);
                Console.WriteLine("---GUID:" + finalResult);
                try{
                    selection = Guid.Parse(finalResult);
                }catch{
                    trying++;
                }
            }

            var ad = advertisements.Find(e => e.Id == selection);
            if (ad == null)
            {
                return "free";
            }

            _db.Tracks.Add(new Track()
            {
                AdvertisementId = ad.Id,
                DriverId = id,
                Quantity = clientData.AdditionalData.Quantity,
            });
            await _db.SaveChangesAsync();

            // Atualiza a lista dos últimos dois anúncios
            clientData.AdditionalData.LastTwoAdIds.Add(ad.Id);
            if (clientData.AdditionalData.LastTwoAdIds.Count > 2)
            {
                clientData.AdditionalData.LastTwoAdIds.RemoveAt(0); // Remove o mais antigo
            }

            UpdateClientState(id, ClientState.AguardandoPassageiro, _additionalData);
            return ad?.Text ?? string.Empty;
        }
    }

    private void UpdateClientState(string id, ClientState newState, AdditionalData additionalData)
    {
        var clientData = new ClientData
        {
            State = newState,
            AdditionalData = additionalData
        };
        SaveClientData(id, clientData);
    }

    private void SaveClientData(string id, ClientData clientData)
    {
        var clientStateKey = GetRedisKeyForClient(id);
        var json = JsonSerializer.Serialize(clientData);
        _database.StringSet(clientStateKey, json);
    }

    private string GetRedisKeyForClient(string id)
    {
        return $"client:{id}:state";
    }
}