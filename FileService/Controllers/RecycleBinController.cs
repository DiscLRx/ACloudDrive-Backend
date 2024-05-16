using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("api/recycle-bin")]
[Authorize(Roles = "USER")]
public class RecycleBinController(RecycleBinService recycleBinService) : ControllerBase
{
    private readonly RecycleBinService _recycleBinService = recycleBinService;

    [HttpGet]
    public async Task<AppResponse> BrowseRecycleBinAsync()
    {
        return await _recycleBinService.BrowseRecycleBinAsync();
    }

    [HttpPut("{recycleBinId}")]
    public async Task<AppResponse> RestoreFromRecycleBinAsync(string recycleBinId)
    {
        return await _recycleBinService.RestoreFromRecycleBinAsync(recycleBinId);
    }

    [HttpDelete("{recycleBinId}")]
    public async Task<AppResponse> DeleteFromRecycleBinAsync(string recycleBinId)
    {
        return await _recycleBinService.DeleteForeverAsync(recycleBinId);
    }
}