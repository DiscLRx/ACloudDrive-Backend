using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("api/files")]
[Authorize(Roles = "ADMIN")]
public class FileManageController(FileManageService fileManageService) : ControllerBase
{
    private readonly FileManageService _fileManageService = fileManageService;

    [HttpGet]
    public async Task<AppResponse> GetFilesAsync()
    {
        return await _fileManageService.GetFilesAsync();
    }

    [HttpPut("{id}/enable")]
    public async Task<AppResponse> SetFileEnableAsync([FromRoute] string id, [FromQuery] bool enable)
    {
        return await _fileManageService.SetFileEnableAsync(id, enable);
    }
}