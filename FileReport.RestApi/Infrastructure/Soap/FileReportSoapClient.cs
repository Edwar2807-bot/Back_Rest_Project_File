using FileReport.RestApi.Application.Interfaces;
using System.ServiceModel;
using System.Net.Http;
using System.Xml.Linq;
using System.Globalization;
using System.Linq;
using Polly;

namespace FileReport.RestApi.Infrastructure.Soap
{
    public class FileReportSoapClient : IFileReportSoapClient
    {
        private readonly FileReportPortClient _client;
        private readonly int _retryCount = 2;
        private readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(200);
        private readonly int _exceptionsAllowedBeforeBreaking = 2;
        private readonly TimeSpan _durationOfBreak = TimeSpan.FromSeconds(30);
        private readonly Polly.CircuitBreaker.AsyncCircuitBreakerPolicy? _breakerPolicy = null;
        private readonly Polly.Retry.AsyncRetryPolicy? _retryPolicy = null;
        private readonly IAsyncPolicy? _policy = null;

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

            // Build Polly policies
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(_retryCount, _ => _retryDelay);
            _breakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(_exceptionsAllowedBeforeBreaking, _durationOfBreak);
            _policy = Policy.WrapAsync(_retryPolicy, _breakerPolicy);
        }

        private Task<T> ExecutePolicyAsync<T>(Func<Task<T>> action)
        {
            if (_policy != null)
            {
                return _policy.ExecuteAsync((ct) => action(), CancellationToken.None);
            }

            return action();
        }

        public async Task<FileReportDto> GetFileByUuidAsync(string uuid)
        {
            var response = await ExecutePolicyAsync(() => _client.GetFileByUuidAsync(
                new GetFileByUuidRequest { uuid = uuid }));

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
            try
            {
                var response = await ExecutePolicyAsync(() => _client.ListFilesByUserAsync(
                    new ListFilesByUserRequest { userId = userId }));

                return response.ListFilesByUserResponse1?.ToList() ?? new();
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                // Circuit open: return empty list as a graceful fallback
                return new List<FileReportDto>();
            }
        }

        public async Task<List<FileReportDto>> SearchFilesByNameAsync(string fileName)
        {
            List<FileReportDto>? list = null;

            try
            {
                var response = await ExecutePolicyAsync(() => _client.SearchFilesByNameAsync(
                    new SearchFilesByNameRequest { fileName = fileName }));

                // Corrige el acceso a la propiedad según la definición de SearchFilesByNameResponse
                list = response.SearchFilesByNameResponse1?.ToList();

                if (list == null || list.Count == 0)
                {
                    try
                    {
                        return await FetchSearchFilesByNameViaHttp(fileName);
                    }
                    catch
                    {
                        return list ?? new();
                    }
                }
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                return new List<FileReportDto>();
            }

            return list ?? new();
        }

        private async Task<List<FileReportDto>> FetchSearchFilesByNameViaHttp(string fileName)
        {
            var endpoint = _client.Endpoint.Address.Uri.ToString();

            var soapEnvelope =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tns=\"http://project.example.com/file-report/soap\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<tns:SearchFilesByNameRequest>" +
                "<tns:fileName>" + System.Security.SecurityElement.Escape(fileName) + "</tns:fileName>" +
                "</tns:SearchFilesByNameRequest>" +
                "</soapenv:Body>" +
                "</soapenv:Envelope>";

            using var http = new HttpClient();
            // Wrap HTTP call with policy
            return await ExecutePolicyAsync(async () =>
            {
                var content = new StringContent(soapEnvelope);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
                // SOAPAction can be empty per WSDL
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
                req.Headers.Add("SOAPAction", "");

                var resp = await http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var xml = await resp.Content.ReadAsStringAsync();

                var doc = XDocument.Parse(xml);
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

                // Buscar todos los nodos cuyo nombre local es "results" en el documento
                var resultNodes = doc.Descendants().Where(n => n.Name.LocalName.Equals("results", StringComparison.OrdinalIgnoreCase));

                var results = new List<FileReportDto>();

                foreach (var node in resultNodes)
                {
                    var f = new FileReportDto();

                    string GetVal(string localName)
                    {
                        var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                        return el?.Value;
                    }

                    bool IsNil(string localName)
                    {
                        var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                        if (el == null) return true;
                        var nilAttr = el.Attributes().FirstOrDefault(a => a.Name.LocalName == "nil" && a.Name.Namespace == xsi.NamespaceName);
                        return nilAttr != null && (nilAttr.Value == "true" || nilAttr.Value == "1");
                    }

                    // Strings
                    f.uuid = GetVal("uuid") ?? string.Empty;
                    f.fileName = GetVal("fileName") ?? string.Empty;
                    f.originalName = GetVal("originalName");
                    f.contentType = GetVal("contentType");
                    f.userId = GetVal("userId");
                    f.errorMessage = GetVal("errorMessage");
                    f.mimeType = GetVal("mimeType");
                    f.urlFile = GetVal("urlFile");
                    f.sha256Original = GetVal("sha256Original");
                    f.sha256Decrypted = GetVal("sha256Decrypted");
                    f.encryptionAlgorithm = GetVal("encryptionAlgorithm");
                    f.encryptedAesKeyBase64 = GetVal("encryptedAesKeyBase64");
                    f.username = GetVal("username");
                    f.userEmail = GetVal("userEmail");
                    f.userMetadataJson = GetVal("userMetadataJson");
                    f.ivBase64 = GetVal("ivBase64");
                    f.originalName = GetVal("originalName");

                    // Booleans
                    var errorVal = GetVal("error");
                    f.error = !string.IsNullOrEmpty(errorVal) && bool.TryParse(errorVal, out var ev) && ev;

                    var decryptVal = GetVal("decryptValidationOk");
                    if (!IsNil("decryptValidationOk") && !string.IsNullOrEmpty(decryptVal) && bool.TryParse(decryptVal, out var dv))
                    {
                        f.decryptValidationOk = dv;
                        f.decryptValidationOkSpecified = true;
                    }
                    else
                    {
                        f.decryptValidationOkSpecified = false;
                    }

                    // Numbers
                    var fileSizeVal = GetVal("fileSize");
                    if (!IsNil("fileSize") && !string.IsNullOrEmpty(fileSizeVal) && long.TryParse(fileSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var fs))
                    {
                        f.fileSize = fs;
                        f.fileSizeSpecified = true;
                    }
                    else
                    {
                        f.fileSizeSpecified = false;
                    }

                    var originalSizeVal = GetVal("originalSize");
                    if (!IsNil("originalSize") && !string.IsNullOrEmpty(originalSizeVal) && long.TryParse(originalSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var os))
                    {
                        f.originalSize = os;
                        f.originalSizeSpecified = true;
                    }
                    else
                    {
                        f.originalSizeSpecified = false;
                    }

                    // processedAt (DateTime + Specified)
                    var processedVal = GetVal("processedAt");
                    if (!IsNil("processedAt") && !string.IsNullOrEmpty(processedVal) && DateTime.TryParse(processedVal, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var pd))
                    {
                        f.processedAt = pd;
                        f.processedAtSpecified = true;
                    }
                    else
                    {
                        f.processedAtSpecified = false;
                    }

                    results.Add(f);
                }

                return results;
            });
            var content = new StringContent(soapEnvelope);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            req.Headers.Add("SOAPAction", "");

            var resp = await http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var xml = await resp.Content.ReadAsStringAsync();

            var doc = XDocument.Parse(xml);
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var resultNodes = doc.Descendants().Where(n => n.Name.LocalName.Equals("results", StringComparison.OrdinalIgnoreCase));

            var results = new List<FileReportDto>();

            foreach (var node in resultNodes)
            {
                // Reuse parsing logic from FetchListAllFilesPaginatedViaHttp by inlining minimal mapping
                var f = new FileReportDto();
                string GetVal(string localName)
                {
                    var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                    return el?.Value;
                }
                bool IsNil(string localName)
                {
                    var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                    if (el == null) return true;
                    var nilAttr = el.Attributes().FirstOrDefault(a => a.Name.LocalName == "nil" && a.Name.Namespace == xsi.NamespaceName);
                    return nilAttr != null && (nilAttr.Value == "true" || nilAttr.Value == "1");
                }

                f.uuid = GetVal("uuid") ?? string.Empty;
                f.fileName = GetVal("fileName") ?? string.Empty;
                f.originalName = GetVal("originalName");
                f.contentType = GetVal("contentType");
                f.userId = GetVal("userId");
                f.errorMessage = GetVal("errorMessage");
                f.mimeType = GetVal("mimeType");
                f.urlFile = GetVal("urlFile");
                f.sha256Original = GetVal("sha256Original");
                f.sha256Decrypted = GetVal("sha256Decrypted");
                f.encryptionAlgorithm = GetVal("encryptionAlgorithm");
                f.encryptedAesKeyBase64 = GetVal("encryptedAesKeyBase64");
                f.username = GetVal("username");
                f.userEmail = GetVal("userEmail");
                f.userMetadataJson = GetVal("userMetadataJson");
                f.ivBase64 = GetVal("ivBase64");

                var errorVal = GetVal("error");
                f.error = !string.IsNullOrEmpty(errorVal) && bool.TryParse(errorVal, out var ev) && ev;

                var decryptVal = GetVal("decryptValidationOk");
                if (!IsNil("decryptValidationOk") && !string.IsNullOrEmpty(decryptVal) && bool.TryParse(decryptVal, out var dv))
                {
                    f.decryptValidationOk = dv;
                    f.decryptValidationOkSpecified = true;
                }
                else
                {
                    f.decryptValidationOkSpecified = false;
                }

                var fileSizeVal = GetVal("fileSize");
                if (!IsNil("fileSize") && !string.IsNullOrEmpty(fileSizeVal) && long.TryParse(fileSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var fs))
                {
                    f.fileSize = fs;
                    f.fileSizeSpecified = true;
                }
                else
                {
                    f.fileSizeSpecified = false;
                }

                var originalSizeVal = GetVal("originalSize");
                if (!IsNil("originalSize") && !string.IsNullOrEmpty(originalSizeVal) && long.TryParse(originalSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var os))
                {
                    f.originalSize = os;
                    f.originalSizeSpecified = true;
                }
                else
                {
                    f.originalSizeSpecified = false;
                }

                var processedVal = GetVal("processedAt");
                if (!IsNil("processedAt") && !string.IsNullOrEmpty(processedVal) && DateTime.TryParse(processedVal, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var pd))
                {
                    f.processedAt = pd;
                    f.processedAtSpecified = true;
                }
                else
                {
                    f.processedAtSpecified = false;
                }

                results.Add(f);
            }

            return results;
        }

        public async Task<List<FileReportDto>> ListAllFilesPaginatedAsync(int page)
        {
            try
            {
                var response = await ExecutePolicyAsync(() => _client.ListAllFilesPaginatedAsync(
                    new ListAllFilesPaginatedRequest { page = page }));

                // Corrige el acceso a la propiedad según la definición de ListAllFilesPaginatedResponse
                var list = response.ListAllFilesPaginatedResponse1?.ToList();

                // Si la deserialización estándar no devolvió resultados, intentar un fallback
                if (list == null || list.Count == 0)
                {
                    try
                    {
                        return await FetchListAllFilesPaginatedViaHttp(page);
                    }
                    catch
                    {
                        return list ?? new();
                    }
                }

                return list;
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                return new List<FileReportDto>();
            }
        }

        private async Task<List<FileReportDto>> FetchListAllFilesPaginatedViaHttp(int page)
        {
            var endpoint = _client.Endpoint.Address.Uri.ToString();

            var soapEnvelope =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tns=\"http://project.example.com/file-report/soap\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<tns:ListAllFilesPaginatedRequest>" +
                "<tns:page>" + page.ToString(CultureInfo.InvariantCulture) + "</tns:page>" +
                "</tns:ListAllFilesPaginatedRequest>" +
                "</soapenv:Body>" +
                "</soapenv:Envelope>";

            using var http = new HttpClient();
            var content = new StringContent(soapEnvelope);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
            // SOAPAction can be empty per WSDL
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            req.Headers.Add("SOAPAction", "");

            var resp = await http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var xml = await resp.Content.ReadAsStringAsync();

            var doc = XDocument.Parse(xml);
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            // Buscar todos los nodos cuyo nombre local es "results" en el documento
            var resultNodes = doc.Descendants().Where(n => n.Name.LocalName.Equals("results", StringComparison.OrdinalIgnoreCase));

            var results = new List<FileReportDto>();

            foreach (var node in resultNodes)
            {
                var f = new FileReportDto();

                string GetVal(string localName)
                {
                    var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                    return el?.Value;
                }

                bool IsNil(string localName)
                {
                    var el = node.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
                    if (el == null) return true;
                    var nilAttr = el.Attributes().FirstOrDefault(a => a.Name.LocalName == "nil" && a.Name.Namespace == xsi.NamespaceName);
                    return nilAttr != null && (nilAttr.Value == "true" || nilAttr.Value == "1");
                }

                // Strings
                f.uuid = GetVal("uuid") ?? string.Empty;
                f.fileName = GetVal("fileName") ?? string.Empty;
                f.originalName = GetVal("originalName");
                f.contentType = GetVal("contentType");
                f.userId = GetVal("userId");
                f.errorMessage = GetVal("errorMessage");
                f.mimeType = GetVal("mimeType");
                f.urlFile = GetVal("urlFile");
                f.sha256Original = GetVal("sha256Original");
                f.sha256Decrypted = GetVal("sha256Decrypted");
                f.encryptionAlgorithm = GetVal("encryptionAlgorithm");
                f.encryptedAesKeyBase64 = GetVal("encryptedAesKeyBase64");
                f.username = GetVal("username");
                f.userEmail = GetVal("userEmail");
                f.userMetadataJson = GetVal("userMetadataJson");
                f.ivBase64 = GetVal("ivBase64");
                f.originalName = GetVal("originalName");

                // Booleans
                var errorVal = GetVal("error");
                f.error = !string.IsNullOrEmpty(errorVal) && bool.TryParse(errorVal, out var ev) && ev;

                var decryptVal = GetVal("decryptValidationOk");
                if (!IsNil("decryptValidationOk") && !string.IsNullOrEmpty(decryptVal) && bool.TryParse(decryptVal, out var dv))
                {
                    f.decryptValidationOk = dv;
                    f.decryptValidationOkSpecified = true;
                }
                else
                {
                    f.decryptValidationOkSpecified = false;
                }

                // Numbers
                var fileSizeVal = GetVal("fileSize");
                if (!IsNil("fileSize") && !string.IsNullOrEmpty(fileSizeVal) && long.TryParse(fileSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var fs))
                {
                    f.fileSize = fs;
                    f.fileSizeSpecified = true;
                }
                else
                {
                    f.fileSizeSpecified = false;
                }

                var originalSizeVal = GetVal("originalSize");
                if (!IsNil("originalSize") && !string.IsNullOrEmpty(originalSizeVal) && long.TryParse(originalSizeVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var os))
                {
                    f.originalSize = os;
                    f.originalSizeSpecified = true;
                }
                else
                {
                    f.originalSizeSpecified = false;
                }

                // processedAt (DateTime + Specified)
                var processedVal = GetVal("processedAt");
                if (!IsNil("processedAt") && !string.IsNullOrEmpty(processedVal) && DateTime.TryParse(processedVal, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var pd))
                {
                    f.processedAt = pd;
                    f.processedAtSpecified = true;
                }
                else
                {
                    f.processedAtSpecified = false;
                }

                results.Add(f);
            }

            return results;
        }
    }
}
