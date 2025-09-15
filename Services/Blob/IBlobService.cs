public interface IBlobService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string? contentType = null);
    Task<bool> DeleteAsync(string fileName);
}
