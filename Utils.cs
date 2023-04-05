using System.IO;

public static class Utils
{
    public static long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

    public static void WriteFileToArbitraryPath(byte[] bytes, string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: could not create directory for file {path}: {ex.Message}");
            return;
        }

        try
        {
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: could not write file {path}: {ex.Message}");
        }
    }
}