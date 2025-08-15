namespace TicketSystem.Application.Common.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string? subDirectory = null);
    Task<bool> DeleteFileAsync(string filePath);
    Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(string filePath);
    bool IsValidFileType(string fileName);
    long GetMaxFileSize();
    string GetUploadsDirectory();
}