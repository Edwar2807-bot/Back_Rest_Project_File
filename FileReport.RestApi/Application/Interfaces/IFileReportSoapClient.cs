using FileReport.RestApi.Infrastructure.Soap;

namespace FileReport.RestApi.Application.Interfaces
{
    public interface IFileReportSoapClient
    {
        Task<FileReportDto> GetFileByUuidAsync(string uuid);
        Task<List<FileReportDto>> ListFilesByUserAsync(string userId);
        Task<List<FileReportDto>> SearchFilesByNameAsync(string fileName);
        Task<List<FileReportDto>> ListAllFilesPaginatedAsync(int page);
    }
}
