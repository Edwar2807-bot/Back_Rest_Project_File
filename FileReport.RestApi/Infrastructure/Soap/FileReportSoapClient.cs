using FileReport.RestApi.Application.Interfaces;

namespace FileReport.RestApi.Infrastructure.Soap
{
    public class FileReportSoapClient : IFileReportSoapClient
    {
        private readonly FileReportPortClient _client;

        public FileReportSoapClient(IConfiguration configuration)
        {
            _client = new FileReportPortClient(
                FileReportPortClient.EndpointConfiguration.FileReportPortSoap11,
                configuration["Soap:Endpoint"]);
        }

        public async Task<FileReportDto> GetFileByUuidAsync(string uuid)
        {
            var response = await _client.GetFileByUuidAsync(
                new GetFileByUuidRequest { uuid = uuid });

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
