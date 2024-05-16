using FileService.Services;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

public record CreateDirectoryArgs(string ParentId, string Name);

public record RenameItemArgs(string NewName);

public record MoveItemArgs(string TargetDirId);

[ApiController]
[Route("api/items")]
[Authorize(Roles = "USER")]
public class DirectoryItemController(DirectoryItemService directoryItemService) : ControllerBase
{
    private readonly DirectoryItemService _directoryItemService = directoryItemService;

    [HttpGet("root/id")]
    public async Task<AppResponse> GetRootId()
    {
        return await _directoryItemService.GetRootIdAsync();
    }

    [HttpGet("search")]
    public async Task<AppResponse> Search([FromQuery] string search = "")
    {
        return await _directoryItemService.SearchAsync(search);
    }

    [HttpGet("{itemId}/children")]
    public async Task<AppResponse> BrowseDirectoryAsync([FromRoute] string itemId)
    {
        return await _directoryItemService.BrowseDirectoryAsync(itemId);
    }

    [HttpGet("{itemId}/path-items")]
    public async Task<AppResponse> GetPathItemsAsync([FromRoute] string itemId)
    {
        return await _directoryItemService.GetPathItemsAsync(itemId);
    }

    [HttpPost]
    public async Task<AppResponse> CreateDirectoryAsync([FromBody] CreateDirectoryArgs createDirectoryArgs)
    {
        return await _directoryItemService.CreateDirectoryAsync(createDirectoryArgs);
    }

    [HttpPut("{itemId}/rename")]
    public async Task<AppResponse> RenameDirectoryItemAsync([FromRoute] string itemId, [FromBody] RenameItemArgs renameItemArgs)
    {
        return await _directoryItemService.RenameDirectoryItemAsync(renameItemArgs, itemId);
    }

    [HttpPut("{itemId}/move")]
    public async Task<AppResponse> MoveDirectoryItemAsync([FromRoute] string itemId, [FromBody] MoveItemArgs moveItemArgs)
    {
        return await _directoryItemService.MoveDirectoryItemAsync(itemId, moveItemArgs);
    }

    [HttpDelete("{itemId}")]
    public async Task<AppResponse> MoveToRecycleBinAsync(string itemId)
    {
        return await _directoryItemService.MoveToRecycleBinAsync(itemId);
    }

}