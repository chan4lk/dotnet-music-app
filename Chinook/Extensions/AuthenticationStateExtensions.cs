using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Chinook;

public static class AuthenticationStateExtensions
{
    public static async Task<string> GetUserId(this Task<AuthenticationState>? authenticationState )
    {
        if (authenticationState == null) return string.Empty;
        var user = (await authenticationState).User;
        var userId = user.FindFirst(u => u.Type.Contains(ClaimTypes.NameIdentifier))?.Value;
        return userId ?? string.Empty;

    }
}