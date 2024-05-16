using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemService.Services;

namespace SystemService.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "ADMIN")]
public class LogController(LogService logService) : ControllerBase
{
    private readonly LogService _logService = logService;

    [HttpGet]
    public async Task<AppResponse> BrowseLogsAsync(
        [FromQuery(Name = "begin")] long beginTs,
        [FromQuery(Name = "end")] long endTs,
        [FromQuery] string source)
    {
        return await _logService.BrowseLogsAsync(beginTs, endTs, source);
    }
}