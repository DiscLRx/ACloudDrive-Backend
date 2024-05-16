using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

public record HashUploadArgs(string DirId, string FileName, long FileSize ,string HeadHash, string EntiretyHash);

[ApiController]
[Route("api/upload/hash")]
[Authorize(Roles = "USER")]
public class FileHashUploadController(FileHashUploadService fileHashUploadService) : ControllerBase
{
    private readonly FileHashUploadService _fileHashUploadService = fileHashUploadService;

    [HttpPost]
    public async Task<AppResponse> HashUploadAsync([FromBody] HashUploadArgs hashUploadArgs)
    {
        return await _fileHashUploadService.HashUploadAsync(hashUploadArgs);
    }
}
