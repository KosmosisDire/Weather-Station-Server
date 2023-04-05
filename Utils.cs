using System.IO;

public static class Utils
{
    public static long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

    public static void WriteFileToArbitraryPath(string filePath, byte[] file)
    {
        try
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: could not create directory for file {filePath}: {ex.Message}");
            return;
        }

        try
        {
            File.WriteAllBytes(filePath, file);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: could not write file {filePath}: {ex.Message}");
        }
    }
}