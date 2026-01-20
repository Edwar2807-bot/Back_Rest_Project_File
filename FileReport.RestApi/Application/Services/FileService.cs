using FileReport.RestApi.Application.DTOs;
using FileReport.RestApi.Application.Interfaces;
using FileReport.RestApi.Infrastructure.Soap;

namespace FileReport.RestApi.Application.Services
{
    public class FileService : IFileService
    {
        private readonly IFileReportSoapClient _soapClient;
        private readonly IMinioPresignService _minioService;
        private readonly IConfiguration _configuration;

        public FileService(
            IFileReportSoapClient soapClient,
            IMinioPresignService minioService,
            IConfiguration configuration)
        {
            _soapClient = soapClient;
            _minioService = minioService;
            _configuration = configuration;
        }

        public async Task<FileDto> GetFileByIdAsync(string fileId)
        {
            var file = await _soapClient.GetFileByUuidAsync(fileId);

            if (file.error)
                throw new KeyNotFoundException(file.errorMessage);

            return MapToDto(file);
        }

        public async Task<IEnumerable<FileDto>> GetFilesByUserAsync(string userId)
        {
            var files = await _soapClient.ListFilesByUserAsync(userId);
            return files.Select(MapToDto);
        }

        public async Task<IEnumerable<FileDto>> SearchFilesByNameAsync(string fileName)
        {
            var files = await _soapClient.SearchFilesByNameAsync(fileName);
            return files.Select(MapToDto);
        }

        public async Task<IEnumerable<FileDto>> GetAllFilesPaginatedAsync(int page)
        {
            var files = await _soapClient.ListAllFilesPaginatedAsync(page);
            return files.Select(MapToDto);
        }

        public async Task<DownloadUrlResponseDto> GenerateDownloadUrlAsync(string fileId, bool encrypted)
        {
            var file = await _soapClient.GetFileByUuidAsync(fileId);

            if (file.error)
                throw new KeyNotFoundException(file.errorMessage);

            var url = await _minioService.GeneratePresignedDownloadUrlAsync(file.urlFile);

            return new DownloadUrlResponseDto
            {
                DownloadUrl = url,
                ExpiresInSeconds = _configuration.GetValue<int>("Minio:PresignExpirySeconds")
            };
        }

        private static FileDto MapToDto(FileReportDto file) =>
            new()
            {
                FileId = file.uuid,
                FileName = file.fileName,
                ContentType = file.contentType,
                FileSize = file.fileSize,
                UserId = file.userId,
                ProcessedAt = file.processedAt,
                DecryptValidationOk = file.decryptValidationOk,
                EncryptionAlgorithm = file.encryptionAlgorithm
            };
    }
}
