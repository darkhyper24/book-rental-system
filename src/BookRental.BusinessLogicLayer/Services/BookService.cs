public class BookService : IBookService 
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<List<string>> AddBookAsync(CreateBookDto dto)
    {
        var isbn = dto.Isbn.Trim();

        var isbnExists = await _bookRepository.BookIsbnExistsAsync(isbn);
        if (isbnExists)
        {
            return new List<string> { "A book with this ISBN already exists." };
        }

        var book = new Book(dto.Title.Trim(), dto.Author.Trim(), isbn, dto.CopiesAvailable);
        await _bookRepository.AddBookAsync(book);

        return new List<string>();
    }

    public async Task<List<BookDto>> GetAllBooksAsync()
    {
        var books = await _bookRepository.GetAllBooksWithPendingCountAsync();

        var result = new List<BookDto>();
        foreach (var item in books)
        {
            var book = item.Book;
            if (book == null)
            {
                continue;
            }

            result.Add(new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn,
                CopiesAvailable = book.CopiesAvailable,
                IsAvailable = book.CopiesAvailable - item.PendingCount > 0
            });
        }
        return result;
    }
}
