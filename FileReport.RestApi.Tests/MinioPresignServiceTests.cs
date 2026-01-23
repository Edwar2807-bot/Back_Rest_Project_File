using System.Collections.Generic;
using FileReport.RestApi.Infrastructure.Minio;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FileReport.RestApi.Tests
{
    public class MinioPresignServiceTests
    {
        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Minio:Endpoint"] = "http://localhost:9000",
                    ["Minio:AccessKey"] = "minioadmin",
                    ["Minio:SecretKey"] = "minioadmin",
                    ["Minio:Bucket"] = "file-pipeline",
                    ["Minio:PresignExpirySeconds"] = "120"
                })
                .Build();

        [Fact]
        public async Task GeneratePresignedDownloadUrlAsync_ReturnsSignedUrl()
        {
            var configuration = BuildConfiguration();
            var service = new MinioPresignService(configuration);
            var fileUrl = "http://localhost:9000/file-pipeline/path/to/file.txt";

            var presignedUrl = await service.GeneratePresignedDownloadUrlAsync(fileUrl);

            Assert.False(string.IsNullOrWhiteSpace(presignedUrl));
            var uri = new Uri(presignedUrl);
            Assert.Equal("/file-pipeline/path/to/file.txt", uri.AbsolutePath);
            Assert.Contains("X-Amz-Signature", uri.Query);
        }
    }
}
