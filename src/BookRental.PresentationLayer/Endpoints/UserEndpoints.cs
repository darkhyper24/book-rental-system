using System.Security.Claims;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/user");

        group.MapPost("/books/{bookId:guid}/rent", RequestRent);
        group.MapGet("/rents", ListMyRents);
    }

    private static async Task<IResult> RequestRent(Guid bookId, ClaimsPrincipal user, RentService rentService)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")!);

        var errors = await rentService.RequestRentAsync(userId, bookId);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Created();
    }

    private static async Task<IResult> ListMyRents(ClaimsPrincipal user, RentService rentService)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")!);

        var rents = await rentService.GetUserRentsAsync(userId);
        return Results.Ok(rents);
    }
}
