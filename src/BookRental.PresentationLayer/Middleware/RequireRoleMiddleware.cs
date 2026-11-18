using Microsoft.AspNetCore.Authentication;

public class RequireRoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _allowedRoles;

    public RequireRoleMiddleware(RequestDelegate next, string[] allowedRoles)
    {
        _next = next;
        _allowedRoles = allowedRoles;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync();
            return;
        }

        foreach (var role in _allowedRoles)
        {
            if (context.User.IsInRole(role))
            {
                await _next(context);
                return;
            }
        }

        await context.ForbidAsync();
    }
}

public static class RequireRoleMiddlewareExtensions
{
    public static IApplicationBuilder UseRequireRole(this IApplicationBuilder app, params string[] roles)
    {
        return app.UseMiddleware<RequireRoleMiddleware>(new object[] { roles });
    }
}
