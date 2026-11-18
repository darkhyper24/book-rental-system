public static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/", ListBooks);
    }

    private static async Task<IResult> ListBooks(BookService bookService)
    {
        var books = await bookService.GetAllBooksAsync();
        return Results.Ok(books);
    }
}
