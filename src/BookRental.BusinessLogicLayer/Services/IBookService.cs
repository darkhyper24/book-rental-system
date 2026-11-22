public interface IBookService
{
    Task<List<string>> AddBookAsync(CreateBookDto dto);
    Task<List<BookDto>> GetAllBooksAsync();
    
}