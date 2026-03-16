namespace Common.Helpers;

public static class MemoryStreamHelper
{
    public static string ToCoverBase64(this System.IO.MemoryStream memoryStream)
    {
        byte[] imageBytes = new byte[memoryStream.Length];
        memoryStream.Position = 0;
        memoryStream.ReadExactly(imageBytes);
        string coverAsB64 = Convert.ToBase64String(imageBytes);
        return coverAsB64;
    }
}