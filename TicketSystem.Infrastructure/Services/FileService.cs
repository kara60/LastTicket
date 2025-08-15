using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;

    public FileService(IConfiguration configuration, ILogger<FileService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string? subDirectory = null)
    {
        var uploadsPath = GetUploadsDirectory();
        if (!string.IsNullOrEmpty(subDirectory))
        {
            uploadsPath = Path.Combine(uploadsPath, subDirectory);
        }

        Directory.CreateDirectory(uploadsPath);

        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);

        _logger.LogInformation("File saved: {FilePath}", filePath);
        return filePath;
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
        }
        return false;
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        var stream = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        return (stream, contentType, fileName);
    }

    public bool IsValidFileType(string fileName)
    {
        var allowedExtensions = _configuration.GetSection("FileSettings:AllowedExtensions").Get<string[]>() ??
            new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", ".txt" };

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    public long GetMaxFileSize()
    {
        return _configuration.GetValue<long>("FileSettings:MaxFileSize", 10485760); // 10MB default
    }

    public string GetUploadsDirectory()
    {
        return _configuration.GetValue<string>("FileSettings:UploadPath", "wwwroot/uploads")!;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}