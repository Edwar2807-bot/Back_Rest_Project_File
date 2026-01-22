using FileReport.RestApi.Application.Interfaces;
using System.ServiceModel;

namespace FileReport.RestApi.Infrastructure.Soap
{
    public class FileReportSoapClient : IFileReportSoapClient
    {
        private readonly FileReportPortClient _client;

        public FileReportSoapClient(IConfiguration configuration)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                SendTimeout = TimeSpan.FromMinutes(3),
                ReceiveTimeout = TimeSpan.FromMinutes(3),
                OpenTimeout = TimeSpan.FromSeconds(30),
                CloseTimeout = TimeSpan.FromSeconds(30),
                MaxReceivedMessageSize = 10 * 1024 * 1024 // 10 MB
            };

            var endpoint = new EndpointAddress(configuration["Soap:Endpoint"]);

            _client = new FileReportPortClient(binding, endpoint);
        }

        public async Task<FileReportDto> GetFileByUuidAsync(string uuid)
        {
            var response = await _client.GetFileByUuidAsync(
                new GetFileByUuidRequest { uuid = uuid });

            var f = response.GetFileByUuidResponse.file;

            Console.WriteLine($"fileSize: {f.fileSize}");
            Console.WriteLine($"fileSizeSpecified: {f.fileSizeSpecified}");
            Console.WriteLine($"decryptValidationOk: {f.decryptValidationOk}");
            Console.WriteLine($"decryptValidationOkSpecified: {f.decryptValidationOkSpecified}");
            Console.WriteLine($"processedAt: {f.processedAt}");
            Console.WriteLine($"processedAtSpecified: {f.processedAtSpecified}");

            return response.GetFileByUuidResponse.file;
        }

        public async Task<List<FileReportDto>> ListFilesByUserAsync(string userId)
        {
            var response = await _client.ListFilesByUserAsync(
                new ListFilesByUserRequest { userId = userId });

            return response.ListFilesByUserResponse1?.ToList() ?? new();
        }

        public async Task<List<FileReportDto>> SearchFilesByNameAsync(string fileName)
        {
            var response = await _client.SearchFilesByNameAsync(
                new SearchFilesByNameRequest { fileName = fileName });

            // Corrige el acceso a la propiedad según la definición de SearchFilesByNameResponse
            return response.SearchFilesByNameResponse1?.ToList() ?? new();
        }

        public async Task<List<FileReportDto>> ListAllFilesPaginatedAsync(int page)
        {
            var response = await _client.ListAllFilesPaginatedAsync(
                new ListAllFilesPaginatedRequest { page = page });

            // Corrige el acceso a la propiedad según la definición de ListAllFilesPaginatedResponse
            return response.ListAllFilesPaginatedResponse1?.ToList() ?? new();
        }
    }
}
