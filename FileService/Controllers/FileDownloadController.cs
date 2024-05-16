using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("api/download")]
public class FileDownloadController(FileDownloadService fileDownloadService) : ControllerBase
{
    private readonly FileDownloadService _fileDownloadService = fileDownloadService;

    [HttpGet("items/{id}")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> GetPresignedFileUrlByDirItemIdAsync([FromRoute] string id)
    {
        return await _fileDownloadService.GetPresignedFileUrlByDirItemIdAsync(id);
    }

    [HttpGet("files/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<AppResponse> GetPresignedFileUrlByFileIdAsync([FromRoute] string id)
    {
        return await _fileDownloadService.GetPresignedFileUrlByFileIdAsync(id);
    }
}