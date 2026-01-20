namespace FileReport.RestApi.Application.DTOs
{
    public class DownloadUrlResponseDto
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
