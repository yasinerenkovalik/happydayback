using Microsoft.AspNetCore.Http;

namespace HappyDay.Application.Interface.Services;

public interface IFileService
{
    Task<List<string>> SaveFilesAsync(List<IFormFile> files, string targetFolder);
    Task<string> SaveFileAsync(IFormFile file, string folder);

    /// <summary>SaveFileAsync'in döndürdüğü "/uploads/..." yolundaki dosyayı siler. Başka yolları yok sayar.</summary>
    void DeleteFile(string? webPath);
}
