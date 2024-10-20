using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class VideoToImageService
{
    private const int MaxRetries = 3; // Número máximo de tentativas
    private const int TimeoutMilliseconds = 10000; // Tempo limite (10 segundos)

    public static async Task<List<string>> ConvertVideoFragmentToImagesWithRetriesAsync(byte[] videoData)
    {
        int attempt = 0;
        List<string> imageBase64List = null;

        while (attempt < MaxRetries)
        {
            attempt++;
            try
            {
                imageBase64List = await ConvertVideoFragmentToImagesAsync(videoData, TimeoutMilliseconds);
                break; // Se funcionar, sai do loop
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"Tentativa {attempt} falhou: {ex.Message}");
                if (attempt == MaxRetries)
                {
                    throw new Exception("Máximo de tentativas atingido. O processo falhou.");
                }
            }
        }

        return imageBase64List;
    }

    private static async Task<List<string>> ConvertVideoFragmentToImagesAsync(byte[] videoData, int timeoutMilliseconds)
    {
        var imageBase64List = new List<string>();

        string outputDir = "temp_images";
        Directory.CreateDirectory(outputDir);

        using (var videoStream = new MemoryStream(videoData))
        {
            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-i pipe:0 -vf scale=800:-1,fps=1 -f image2pipe -vcodec mjpeg pipe:1",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false, // Isso ajuda a capturar erros
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            ffmpegProcess.Start();

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Configura o timeout para matar o processo se demorar muito
            var timeoutTask = Task.Delay(timeoutMilliseconds, cancellationToken);
            
            try
            {
                var processingTask = Task.Run(async () =>
                {
                    // Enviar os dados do vídeo para o processo de entrada
                    await videoStream.CopyToAsync(ffmpegProcess.StandardInput.BaseStream);
                    ffmpegProcess.StandardInput.Close();

                    // Ler a saída do processo (imagens) e salvar na lista
                    using (var memoryStream = new MemoryStream())
                    {
                        var outputTask = Task.Run(async () =>
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = await ffmpegProcess.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                            }
                        });

                        var errorTask = Task.Run(async () =>
                        {
                            // Consumir o stderr para evitar travamento
                            string errorOutput = await ffmpegProcess.StandardError.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(errorOutput))
                            {
                                Debug.WriteLine($"FFmpeg error: {errorOutput}");
                            }
                        });

                        await Task.WhenAll(outputTask, errorTask);

                        if (memoryStream.Length > 0)
                        {
                            byte[] imageData = memoryStream.ToArray();
                            string imageBase64 = Convert.ToBase64String(imageData);
                            imageBase64List.Add(imageBase64);
                        }
                    }

                    await ffmpegProcess.WaitForExitAsync();
                }, cancellationToken);

                // Espera ou o timeout ou o fim do processo
                var completedTask = await Task.WhenAny(processingTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    ffmpegProcess.Kill(true); // Força matar o processo
                    cancellationTokenSource.Cancel(); // Cancela a tarefa de processamento
                    throw new TimeoutException("Tempo limite atingido.");
                }

                // Se o processo terminar antes do timeout, cancela o timer de timeout
                cancellationTokenSource.Cancel();
            }
            catch (Exception)
            {
                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill(true); // Mata o processo se ainda estiver rodando
                }
                throw; // Relança a exceção para tentar novamente ou terminar o processo
            }
        }

        return imageBase64List;
    }
}