using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemService.Services;

namespace SystemService.Controllers;

[ApiController]
[Route("api/sysconfig")]
[Authorize(Roles = "ADMIN")]
public class SystemConfigController(SystemConfigService systemConfigService) : ControllerBase
{
    private readonly SystemConfigService _systemConfigService = systemConfigService;

    [HttpGet]
    public AppResponse GetSystemConfig()
    {
        return _systemConfigService.GetSystemConfig();
    }

    [HttpPut("{key}")]
    public AppResponse UpdateSystemConfig([FromRoute] string key, [FromQuery] string value)
    {
        return _systemConfigService.UpdateSystemConfig(key, value);
    }
}