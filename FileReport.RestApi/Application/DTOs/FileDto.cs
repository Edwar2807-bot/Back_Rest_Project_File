namespace FileReport.RestApi.Application.DTOs
{
    public class FileDto
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
        public bool DecryptValidationOk { get; set; }
        public string EncryptionAlgorithm { get; set; } = string.Empty;
    }
}
