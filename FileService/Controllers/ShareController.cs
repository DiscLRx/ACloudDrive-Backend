using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

public record CreateShareArgs(string ItemId, long Ts, string Key);

public record SaveShareArgs(string Code, string Key, string DirId);

[ApiController]
[Route("api/share")]
[Authorize(Roles = "USER")]
public class ShareController(ShareService shareService) : ControllerBase
{
    private readonly ShareService _shareService = shareService;

    [HttpGet]
    public async Task<AppResponse> GetShareListAsync()
    {
        return await _shareService.GetShareListAsync();
    }

    [HttpGet("{code}")]
    public async Task<AppResponse> GetShareInformationAsync([FromRoute]string code, [FromQuery] string key)
    {
        return await _shareService.GetShareInformationAsync(code, key);
    }

    [HttpGet("{code}/detail")]
    public async Task<AppResponse> GetShareDetailAsync([FromRoute]string code)
    {
        return await _shareService.GetShareDetailAsync(code);
    }

    [HttpPost("create")]
    public async Task<AppResponse> CreateShareAsync([FromBody] CreateShareArgs createShareArgs)
    {
        return await _shareService.CreateShareAsync(createShareArgs);
    }

    [HttpPost("save")]
    public async Task<AppResponse> SaveShareAsync([FromBody] SaveShareArgs saveShareArgs)
    {
        return await _shareService.SaveShareAsync(saveShareArgs);
    }

    [HttpDelete("{code}")]
    public async Task<AppResponse> DeleteShareAsync([FromRoute]string code)
    {
        return await _shareService.DeleteShareAsync(code);
    }

    
}