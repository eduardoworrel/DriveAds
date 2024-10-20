using System.Diagnostics;

public static class VideoToImageService
{
    public static async Task<List<string>> ConvertVideoFragmentToImagesAsync(byte[] videoData)
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
                    Arguments =
                        $"-i pipe:0 -vf scale=800:-1,fps=1 -f image2pipe -vcodec mjpeg pipe:1",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            ffmpegProcess.Start();

            await videoStream.CopyToAsync(ffmpegProcess.StandardInput.BaseStream);
            ffmpegProcess.StandardInput.Close();

            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while (
                    (
                        bytesRead = await ffmpegProcess.StandardOutput.BaseStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length
                        )
                    ) > 0
                )
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                if (memoryStream.Length > 0)
                {
                    byte[] imageData = memoryStream.ToArray();
                    string imageBase64 = Convert.ToBase64String(imageData);
                    imageBase64List.Add(imageBase64);
                }
            }

            await ffmpegProcess.WaitForExitAsync();
        }

        return imageBase64List;
    }
}
