using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("api/upload/physical")]
[Authorize(Roles = "USER")]
public class FilePhysicalUploadController(FilePhysicalUploadService filePhysicalUploadService) : ControllerBase
{
    private readonly FilePhysicalUploadService _filePhysicalUploadService = filePhysicalUploadService;

    [HttpPost("chunk/{uploadKey}/{serial:int}")]
    public async Task<AppResponse> ChunkUploadAsync(
        [FromRoute] string uploadKey,
        [FromRoute] int serial)
    {
        return await _filePhysicalUploadService.UploadChunkAsync(uploadKey, serial);
    }

    [HttpPost("confirm/{uploadKey}")]
    public async Task<AppResponse> UploadConfirmAsync([FromRoute] string uploadKey)
    {
        return await _filePhysicalUploadService.UploadConfirmAsync(uploadKey);
    }
}