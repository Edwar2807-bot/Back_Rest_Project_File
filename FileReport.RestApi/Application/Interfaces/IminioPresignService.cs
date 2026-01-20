namespace FileReport.RestApi.Application.Interfaces
{
    public interface IMinioPresignService
    {
        Task<string> GeneratePresignedDownloadUrlAsync(string fileUrl);
    }
}
