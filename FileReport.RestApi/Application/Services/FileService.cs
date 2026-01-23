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

            var objectPath = encrypted
                ? $"encrypted/{file.uuid}.enc"
                : (string.IsNullOrWhiteSpace(file.urlFile)
                    ? $"original/{file.uuid}.bin"
                    : file.urlFile);

            var url = await _minioService.GeneratePresignedDownloadUrlAsync(objectPath);

            return new DownloadUrlResponseDto
            {
                DownloadUrl = url,
                ExpiresInSeconds = _configuration.GetValue<int>("Minio:PresignExpirySeconds")
            };
        }

        private static FileDto MapToDto(FileReportDto file) =>
            new()
            {
                // Identidad
                FileId = file.uuid,
                FileName = file.fileName,
                OriginalName = file.originalName,

                // Archivo
                ContentType = file.contentType,
                FileSize = file.fileSizeSpecified ? file.fileSize : null,
                MimeType = file.mimeType,
                OriginalSize = file.originalSizeSpecified ? file.originalSize : null,

                // Usuario
                UserId = file.userId,
                Username = file.username,
                UserEmail = file.userEmail,
                UserMetadataJson = file.userMetadataJson,

                // Pipeline
                DecryptValidationOk = file.decryptValidationOkSpecified
                    ? file.decryptValidationOk
                    : null,
                EncryptionAlgorithm = file.encryptionAlgorithm,
                Sha256Original = file.sha256Original,
                Sha256Decrypted = file.sha256Decrypted,
                ProcessedAt = file.processedAtSpecified ? file.processedAt : null,

                // Criptografía / storage
                IvBase64 = file.ivBase64,
                EncryptedAesKeyBase64 = file.encryptedAesKeyBase64,
                UrlFile = file.urlFile,

                // Control
                Error = file.error,
                ErrorMessage = file.errorMessage
            };

    }
}
