using FileReport.RestApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileReport.RestApi.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IFileService _service;

        public UsersController(IFileService service)
        {
            _service = service;
        }

        [HttpGet("{userId}/files")]
        public async Task<IActionResult> GetFilesByUser(string userId)
            => Ok(await _service.GetFilesByUserAsync(userId));
    }
}
