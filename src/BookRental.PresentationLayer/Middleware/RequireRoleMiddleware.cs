using Microsoft.AspNetCore.Authentication;

public class RequireRoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _role;

    public RequireRoleMiddleware(RequestDelegate next, string role)
    {
        _next = next;
        _role = role;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync();
            return;
        }

        if (!context.User.IsInRole(_role))
        {
            await context.ForbidAsync();
            return;
        }

        await _next(context);
    }
}

public static class RequireRoleMiddlewareExtensions
{
    public static IApplicationBuilder UseRequireRole(this IApplicationBuilder app, string role)
    {
        return app.UseMiddleware<RequireRoleMiddleware>(role);
    }
}
/* this middleware assumes that no branch is shared between different roles
   but if we have a feature for example super admin and admin can  both access the same branch(have a common privelage like adding or deleting books) 
   or have common endpoints then we will need to change the middleware to accept a list of roles and check on them instead of a single role.
*/
