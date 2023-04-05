

public static class Utils
{
    public static long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
}