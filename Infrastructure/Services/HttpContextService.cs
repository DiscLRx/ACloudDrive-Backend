using System.Security.Claims;

namespace Infrastructure.Services;

public class HttpContextService(IHttpContextAccessor httpContextAccessor)
{
    private readonly HttpContext _httpContext = httpContextAccessor.HttpContext!;

    public long Uid() =>
        Convert.ToInt64(_httpContext.User.Claims.Single(c => c.Type == "uid").Value);

    public string Role() => _httpContext.User.Claims.Single(c => c.Type == ClaimTypes.Role).Value;
}