public class RentService : IRentService
{
    private readonly IRentRepository _rentRepository;
    private readonly IBookRepository _bookRepository;

    public RentService(IRentRepository rentRepository, IBookRepository bookRepository)
    {
        _rentRepository = rentRepository;
        _bookRepository = bookRepository;
    }

    public async Task<List<string>> RequestRentAsync(Guid userId, Guid bookId)
    {
        var book = await _bookRepository.GetBookByIdAsync(bookId);
        if (book == null)
        {
            return new List<string> { "Book not found." };
        }

        var alreadyPending = await _rentRepository.HasPendingRentAsync(userId, bookId);
        if (alreadyPending)
        {
            return new List<string> { "You already have a pending request for this book." };
        }

        var pendingCount = await _rentRepository.CountPendingRentsForBookAsync(bookId);
        if (pendingCount >= book.CopiesAvailable)
        {
            return new List<string> { "No copies available to request." };
        }

        var rent = new Rent(bookId, userId)
        {
            RequestDate = DateTime.UtcNow,
            Status = RentStatus.Pending
        };

        var added = await _rentRepository.TryAddRentAsync(rent);
        if (!added)
        {
            return new List<string> { "You already have a pending request for this book." };
        }

        return new List<string>();
    }

    public async Task<List<PendingRentDto>> GetPendingRentsAsync()
    {
        var rents = await _rentRepository.GetPendingRentsWithDetailsAsync();

        var result = new List<PendingRentDto>();
        foreach (var rent in rents)
        {
            result.Add(new PendingRentDto
            {
                Id = rent.Id,
                UserName = rent.User!.Name,
                BookTitle = rent.Book!.Title,
                RequestDate = rent.RequestDate
            });
        }
        return result;
    }

    public async Task<List<string>> ApproveRentAsync(Guid rentId)
    {
        var rent = await _rentRepository.GetRentByIdAsync(rentId);
        if (rent == null)
        {
            return new List<string> { "Rental request not found." };
        }

        var result = await _rentRepository.ApproveRentAsync(rent.Id, rent.BookId);
        if (result == ApproveResult.AlreadyHandled)
        {
            return new List<string> { "This request was already handled." };
        }
        if (result == ApproveResult.NoCopiesLeft)
        {
            return new List<string> { "No copies left to approve this request." };
        }

        return new List<string>();
    }

    public async Task<List<string>> RejectRentAsync(Guid rentId)
    {
        var rent = await _rentRepository.GetRentByIdAsync(rentId);
        if (rent == null)
        {
            return new List<string> { "Rental request not found." };
        }

        var rejected = await _rentRepository.RejectRentAsync(rent.Id);
        if (!rejected)
        {
            return new List<string> { "This request was already handled." };
        }

        return new List<string>();
    }

    public async Task<List<MyRentDto>> GetUserRentsAsync(Guid userId)
    {
        var rents = await _rentRepository.GetUserRentsWithBookAsync(userId);

        var result = new List<MyRentDto>();
        foreach (var rent in rents)
        {
            result.Add(new MyRentDto
            {
                Id = rent.Id,
                BookTitle = rent.Book!.Title,
                RequestDate = rent.RequestDate,
                Status = rent.Status.ToString()
            });
        }
        return result;
    }
}
