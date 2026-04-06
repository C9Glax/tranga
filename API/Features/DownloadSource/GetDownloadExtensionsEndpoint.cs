using API.Entities.DownloadExtensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Features.DownloadSource;

public abstract class GetDownloadExtensionsEndpoint
{
    public static Ok<DownloadExtensionsList> Handle() => TypedResults.Ok(new DownloadExtensionsList());
}