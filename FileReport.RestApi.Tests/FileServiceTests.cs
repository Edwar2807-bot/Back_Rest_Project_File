using System.Collections.Generic;
using FileReport.RestApi.Application.DTOs;
using FileReport.RestApi.Application.Interfaces;
using FileReport.RestApi.Application.Services;
using FileReport.RestApi.Infrastructure.Soap;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FileReport.RestApi.Tests
{
    public class FileServiceTests
    {
        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Minio:PresignExpirySeconds"] = "120"
                })
                .Build();

        private static FileReportDto BuildFile(string uuid, string urlFile = "http://localhost:9000/file-pipeline/original/test.bin") =>
            new FileReportDto
            {
                uuid = uuid,
                fileName = "test.bin",
                urlFile = urlFile,
                error = false
            };

        [Fact]
        public async Task GenerateDownloadUrlAsync_Original_UsesUrlFile()
        {
            var fileId = "uuid-123";
            var file = BuildFile(fileId);

            var soapMock = new Mock<IFileReportSoapClient>();
            soapMock.Setup(s => s.GetFileByUuidAsync(fileId))
                .ReturnsAsync(file);

            var presignMock = new Mock<IMinioPresignService>();
            presignMock.Setup(p => p.GeneratePresignedDownloadUrlAsync(file.urlFile))
                .ReturnsAsync("signed-original");

            var service = new FileService(soapMock.Object, presignMock.Object, BuildConfiguration());

            var result = await service.GenerateDownloadUrlAsync(fileId, encrypted: false);

            Assert.Equal("signed-original", result.DownloadUrl);
            presignMock.Verify(p => p.GeneratePresignedDownloadUrlAsync(file.urlFile), Times.Once);
        }

        [Fact]
        public async Task GenerateDownloadUrlAsync_Encrypted_UsesEncryptedPath()
        {
            var fileId = "uuid-456";
            var file = BuildFile(fileId, urlFile: "http://localhost:9000/file-pipeline/original/another.bin");

            var soapMock = new Mock<IFileReportSoapClient>();
            soapMock.Setup(s => s.GetFileByUuidAsync(fileId))
                .ReturnsAsync(file);

            var presignMock = new Mock<IMinioPresignService>();
            presignMock.Setup(p => p.GeneratePresignedDownloadUrlAsync($"encrypted/{fileId}.enc"))
                .ReturnsAsync("signed-encrypted");

            var service = new FileService(soapMock.Object, presignMock.Object, BuildConfiguration());

            var result = await service.GenerateDownloadUrlAsync(fileId, encrypted: true);

            Assert.Equal("signed-encrypted", result.DownloadUrl);
            presignMock.Verify(p => p.GeneratePresignedDownloadUrlAsync($"encrypted/{fileId}.enc"), Times.Once);
        }
    }
}
