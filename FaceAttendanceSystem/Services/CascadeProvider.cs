namespace FaceAttendanceSystem.Services;

/// <summary>
/// Makes sure the Haar Cascade XML file (the pre-trained face detector) exists.
/// It is downloaded once from the official OpenCV repository.
/// </summary>
public static class CascadeProvider
{
    private const string FileName = "haarcascade_frontalface_default.xml";
    private const string Url =
        "https://raw.githubusercontent.com/opencv/opencv/4.x/data/haarcascades/" + FileName;

    public static string EnsureCascade(string dataDir)
    {
        var path = Path.Combine(dataDir, FileName);
        if (File.Exists(path)) return path;

        Console.WriteLine("Downloading face detector file (one time only)...");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var bytes = http.GetByteArrayAsync(Url).GetAwaiter().GetResult();
            File.WriteAllBytes(path, bytes);
            Console.WriteLine("Downloaded.");
            return path;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not download {FileName}: {ex.Message}\n" +
                $"Please download it manually from:\n{Url}\n" +
                $"and save it inside this folder: {dataDir}");
        }
    }
}
