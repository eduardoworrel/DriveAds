using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public static class LlamaService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const int MaxRetries = 2; // Número máximo de tentativas
    private const int RetryDelay = 3000; // Tempo de espera entre tentativas (em milissegundos)

    // Método para fazer a requisição à API
    public static async Task<string> ChatAsync(
        string apiKey,
        string textPrompt,
        CancellationToken cts
    )
    {
        // Definir o URL da API
        var url = "https://api.aimlapi.com/v1/chat/completions";

        var model = "gpt-4o-2024-08-06";

        var payload = new
        {
            model,
            max_tokens = 36,
            messages = new[] { new { role = "user", content = textPrompt } },
        };

        // Serializar o payload para JSON
        var jsonPayload = JsonSerializer.Serialize(payload);

        // Cria o conteúdo da requisição com os cabeçalhos e payload
        var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Adiciona o cabeçalho de autorização
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        int attempt = 0;
        while (attempt < MaxRetries && !cts.IsCancellationRequested)
        {
            try
            {
                attempt++;

                // Envia a requisição HTTP POST
                var response = await _httpClient.PostAsync(url, requestContent);

                // Garante que a resposta seja bem-sucedida
                var responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                // Parseia o resultado para JSON
                var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);

                if (result != null)
                {
                    var content = result.Choices[0].Message.Content;
                    return content;
                }
                else
                {
                    Console.WriteLine("Invalid JSON response.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro ao chamar a API: {ex.Message}");
                if (attempt < MaxRetries && !cts.IsCancellationRequested)
                {
                    Console.WriteLine(
                        $"Tentativa {attempt} falhou. Tentando novamente em {RetryDelay / 1000} segundos..."
                    );
                    await Task.Delay(RetryDelay, cts); // Espera antes de tentar novamente
                }
                else
                {
                    Console.WriteLine("Número máximo de tentativas alcançado. Abortando.");
                    return "0";
                }
            }
        }

        return "0"; // Retorna '0' se todas as tentativas falharem
    }

    public static async Task<string> SendImagesToApiAsync(
        string apiKey,
        List<string> images,
        string textPrompt,
        CancellationToken cts
    )
    {
        // Definir o URL da API
        var url = "https://api.aimlapi.com/v1/chat/completions";

        // Montar o conteúdo da mensagem para o payload, combinando o prompt e as imagens
        var contentList = new List<object> { new { type = "text", text = textPrompt } };

        foreach (var image in images)
        {
            contentList.Add(
                new
                {
                    role = "user",
                    type = "image_url",
                    image_url = new { url = "data:image/jpeg;base64," + image },
                }
            );
        }

        var model = "meta-llama/Llama-3.2-90B-Vision-Instruct-Turbo";

        var payload = new
        {
            model,
            max_tokens = 600,
            messages = new[] { new { role = "user", content = contentList } },
        };

        // Serializar o payload para JSON
        var jsonPayload = JsonSerializer.Serialize(payload);

        // Cria o conteúdo da requisição com os cabeçalhos e payload
        var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Adiciona o cabeçalho de autorização
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        int attempt = 0;
        while (attempt < MaxRetries && !cts.IsCancellationRequested)
        {
            try
            {
                attempt++;

                // Envia a requisição HTTP POST
                var response = await _httpClient.PostAsync(url, requestContent, cts);

                // Garante que a resposta seja bem-sucedida
                var responseBody = await response.Content.ReadAsStringAsync(cts);
                response.EnsureSuccessStatusCode();

                // Parseia o resultado para JSON
                var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);

                if (result != null)
                {
                    var content = result.Choices[0].Message.Content;
                    return content;
                }
                else
                {
                    Console.WriteLine("Invalid JSON response.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro ao chamar a API: {ex.Message}");
                if (attempt < MaxRetries && !cts.IsCancellationRequested)
                {
                    Console.WriteLine(
                        $"Tentativa {attempt} falhou. Tentando novamente em {RetryDelay / 1000} segundos..."
                    );
                    await Task.Delay(RetryDelay, cts);
                }
                else
                {
                    Console.WriteLine("Número máximo de tentativas alcançado. Abortando.");
                    return "0";
                }
            }
        }

        return "0"; // Retorna '0' se todas as tentativas falharem
    }
}

public class ChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; }
}

public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    public object Logprobs { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<object> ToolCalls { get; set; }
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
