using DirectoryService.Protos.Services;
using FileService.Services.Data;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace FileService.Protos.Services;

public class GrpcRootDirectoryInitializerService(DirectoryItemDbService directoryItemDbService)
    : RootDirectoryInitializer.RootDirectoryInitializerBase
{
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;

    public override async Task<Empty> CreateRootDirectory(CreateRootArgs createRootArgs, ServerCallContext context)
    {
        await _directoryItemDbService.CreateRootDirectoryAsync(createRootArgs.Uid);
        return new Empty();
    }
}