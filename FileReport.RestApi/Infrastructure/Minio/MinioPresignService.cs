using FileReport.RestApi.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace FileReport.RestApi.Infrastructure.Minio
{
    public class MinioPresignService : IMinioPresignService
    {
        private readonly IConfiguration _configuration;

        public MinioPresignService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GeneratePresignedDownloadUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("fileUrl cannot be empty", nameof(fileUrl));

            var minioSection = _configuration.GetSection("Minio");
            var endpoint = minioSection.GetValue<string>("Endpoint")
                ?? throw new InvalidOperationException("Minio:Endpoint is not configured");
            var accessKey = minioSection.GetValue<string>("AccessKey")
                ?? throw new InvalidOperationException("Minio:AccessKey is not configured");
            var secretKey = minioSection.GetValue<string>("SecretKey")
                ?? throw new InvalidOperationException("Minio:SecretKey is not configured");
            var bucket = minioSection.GetValue<string>("Bucket")
                ?? throw new InvalidOperationException("Minio:Bucket is not configured");
            var expirySeconds = minioSection.GetValue<int?>("PresignExpirySeconds") ?? 120;

            var endpointUri = new Uri(endpoint);
            var hostWithPort = endpointUri.IsDefaultPort
                ? endpointUri.Host
                : $"{endpointUri.Host}:{endpointUri.Port}";

            var clientBuilder = new MinioClient()
                .WithEndpoint(hostWithPort)
                .WithCredentials(accessKey, secretKey);

            if (endpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                clientBuilder = clientBuilder.WithSSL();
            }

            var minioClient = clientBuilder.Build();

            var objectName = ResolveObjectName(fileUrl, bucket);

            var args = new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            return await minioClient.PresignedGetObjectAsync(args);
        }

        private static string ResolveObjectName(string fileUrl, string bucket)
        {
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                var segments = path.Split('/', 2);

                if (segments.Length > 1 && segments[0].Equals(bucket, StringComparison.OrdinalIgnoreCase))
                    return segments[1];

                return path;
            }

            return fileUrl.TrimStart('/');
        }
    }
}
