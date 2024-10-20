using System.Net.WebSockets;
using System.Text;
using webapp.Data;

public static class WebsocketGate
{
    public static void AddWebSocketServices(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api");

        group.MapGet("sync/{id}", SyncClient);
    }

    public static async Task<IResult> SyncClient(
        IConfiguration config,
        HttpContext context,
        ApplicationDbContext db,
        Orquestrator orq,
        string id,
        CancellationToken cts
    )
    {

        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.BadRequest();
        }
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 8];
        while (!webSocket.CloseStatus.HasValue)
        {
            await using var memoryStream = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(buffer, cts);
                var block = buffer.AsMemory(0, result.Count);
                await memoryStream.WriteAsync(block);
            } while (!result.EndOfMessage);

            memoryStream.Seek(0, SeekOrigin.Begin);
            try{
            var candidate = await orq.StartOrContinueProcess(
                id,
                memoryStream,
                db,
                config["llama_key"] ?? throw new Exception("llama key"),
                cts
            );
            if (candidate.Length > 0)
            {
                await webSocket.SendAsync(
                    Encoding.UTF8.GetBytes(candidate),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                if(candidate != "free"){
                    await webSocket.SendAsync(
                        Encoding.UTF8.GetBytes("free"),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
            else { }
            }catch{ }
        }

         return Results.Empty;
    }
}
