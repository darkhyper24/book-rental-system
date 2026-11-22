using Microsoft.EntityFrameworkCore;
using Npgsql;

public enum ApproveResult
{
    Approved,
    AlreadyHandled,
    NoCopiesLeft
}

public class RentRepository : IRentRepository 
{
    private readonly AppDbContext _context;

    public RentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountPendingRentsForBookAsync(Guid bookId)
    {
        return await _context.Rents
            .CountAsync(rent => rent.BookId == bookId && rent.Status == RentStatus.Pending);
    }

    public async Task<bool> HasPendingRentAsync(Guid userId, Guid bookId)
    {
        return await _context.Rents
            .AnyAsync(rent => rent.UserId == userId && rent.BookId == bookId && rent.Status == RentStatus.Pending);
    }

    public async Task<bool> TryAddRentAsync(Rent rent)
    {
        _context.Rents.Add(rent);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                  && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _context.Rents.Remove(rent);
            return false;
        }
    }

    public async Task<Rent?> GetRentByIdAsync(Guid rentId)
    {
        return await _context.Rents
            .AsNoTracking()
            .FirstOrDefaultAsync(rent => rent.Id == rentId);
    }

    public async Task<List<Rent>> GetPendingRentsWithDetailsAsync()
    {
        return await _context.Rents
            .AsNoTracking()
            .Include(rent => rent.User)
            .Include(rent => rent.Book)
            .Where(rent => rent.Status == RentStatus.Pending)
            .OrderBy(rent => rent.RequestDate)
            .ToListAsync();
    }

    public async Task<List<Rent>> GetUserRentsWithBookAsync(Guid userId)
    {
        return await _context.Rents
            .AsNoTracking()
            .Include(rent => rent.Book)
            .Where(rent => rent.UserId == userId)
            .OrderByDescending(rent => rent.RequestDate)
            .ToListAsync();
    }

    public async Task<ApproveResult> ApproveRentAsync(Guid rentId, Guid bookId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var rentsChanged = await _context.Rents
            .Where(rent => rent.Id == rentId && rent.Status == RentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rent => rent.Status, RentStatus.Approved));

        if (rentsChanged == 0)
        {
            await transaction.RollbackAsync();
            return ApproveResult.AlreadyHandled;
        }

        var booksChanged = await _context.Books
            .Where(book => book.Id == bookId && book.CopiesAvailable > 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(book => book.CopiesAvailable, book => book.CopiesAvailable - 1));

        if (booksChanged == 0)
        {
            await transaction.RollbackAsync();
            return ApproveResult.NoCopiesLeft;
        }

        await transaction.CommitAsync();
        return ApproveResult.Approved;
    }

    public async Task<bool> RejectRentAsync(Guid rentId)
    {
        var rentsChanged = await _context.Rents
            .Where(rent => rent.Id == rentId && rent.Status == RentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rent => rent.Status, RentStatus.Rejected));

        return rentsChanged > 0;
    }
}
