namespace FileReport.RestApi.Application.DTOs
{
    public class FileDto
    {
        // ===== Identidad =====
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;

        // ===== Archivo =====
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public long? OriginalSize { get; set; }

        // ===== Usuario =====
        public string UserId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? UserEmail { get; set; }
        public string? UserMetadataJson { get; set; }

        // ===== Procesamiento / Pipeline =====
        public bool? DecryptValidationOk { get; set; }
        public string? EncryptionAlgorithm { get; set; }
        public string? Sha256Original { get; set; }
        public string? Sha256Decrypted { get; set; }
        public DateTime? ProcessedAt { get; set; }

        // ===== Criptografía =====
        public string? IvBase64 { get; set; }
        public string? EncryptedAesKeyBase64 { get; set; }

        // ===== Almacenamiento =====
        public string? UrlFile { get; set; }

        // ===== Control =====
        public bool Error { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

