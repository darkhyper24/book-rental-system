public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapPost("/books", AddBook);
        group.MapGet("/rents/pending", ListPendingRents);
        group.MapPost("/rents/{rentId:guid}/approve", ApproveRent);
        group.MapPost("/rents/{rentId:guid}/reject", RejectRent);
    }

    private static async Task<IResult> AddBook(CreateBookDto dto, IBookService bookService)
    {
        var errors = await bookService.AddBookAsync(dto);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Created();
    }

    private static async Task<IResult> ListPendingRents(IRentService rentService)
    {
        var rents = await rentService.GetPendingRentsAsync();
        return Results.Ok(rents);
    }

    private static async Task<IResult> ApproveRent(Guid rentId, IRentService rentService)
    {
        var errors = await rentService.ApproveRentAsync(rentId);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Ok();
    }

    private static async Task<IResult> RejectRent(Guid rentId, IRentService rentService)
    {
        var errors = await rentService.RejectRentAsync(rentId);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Ok();
    }
}
