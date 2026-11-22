public interface IBookRepository
{
    Task<Book?> GetBookByIdAsync(Guid bookId);
    Task<List<BookWithPendingCount>> GetAllBooksWithPendingCountAsync();
    Task<bool> BookIsbnExistsAsync(string isbn);
    Task AddBookAsync(Book book);
}