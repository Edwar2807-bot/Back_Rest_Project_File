using FileReport.RestApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileReport.RestApi.Controllers
{
    [ApiController]
    [Route("api/v1/fileInfo")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _service;

        public FilesController(IFileService service)
        {
            _service = service;
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetById(string fileId)
            => Ok(await _service.GetFileByIdAsync(fileId));

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string fileName)
            => Ok(await _service.SearchFilesByNameAsync(fileName));

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1)
            => Ok(await _service.GetAllFilesPaginatedAsync(page));

        [HttpPost("{fileId}/download/original")]
        public async Task<IActionResult> DownloadOriginal(string fileId)
            => Ok(await _service.GenerateDownloadUrlAsync(fileId, encrypted: false));

        [HttpPost("{fileId}/download/encrypted")]
        public async Task<IActionResult> DownloadEncrypted(string fileId)
            => Ok(await _service.GenerateDownloadUrlAsync(fileId, encrypted: true));
    }
}
