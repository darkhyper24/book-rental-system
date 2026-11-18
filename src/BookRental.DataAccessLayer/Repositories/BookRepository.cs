using Microsoft.EntityFrameworkCore;

public class BookWithPendingCount
{
    public Book? Book { get; set; }
    public int PendingCount { get; set; }
}

public class BookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> BookIsbnExistsAsync(string isbn)
    {
        return await _context.Books.AnyAsync(book => book.Isbn == isbn);
    }

    public async Task AddBookAsync(Book book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
    }

    public async Task<Book?> GetBookByIdAsync(Guid bookId)
    {
        return await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(book => book.Id == bookId);
    }

    public async Task<List<BookWithPendingCount>> GetAllBooksWithPendingCountAsync()
    {
        return await _context.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .Select(book => new BookWithPendingCount
            {
                Book = book,
                PendingCount = _context.Rents.Count(rent => rent.BookId == book.Id && rent.Status == RentStatus.Pending)
            })
            .ToListAsync();
    }
}
