using FileReport.RestApi.Application.DTOs;

namespace FileReport.RestApi.Application.Interfaces
{
    public interface IFileService
    {
        Task<FileDto> GetFileByIdAsync(string fileId);
        Task<IEnumerable<FileDto>> GetFilesByUserAsync(string userId);
        Task<IEnumerable<FileDto>> SearchFilesByNameAsync(string fileName);
        Task<IEnumerable<FileDto>> GetAllFilesPaginatedAsync(int page);
        Task<DownloadUrlResponseDto> GenerateDownloadUrlAsync(string fileId, bool encrypted);
    }
}
