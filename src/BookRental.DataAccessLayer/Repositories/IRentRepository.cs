public interface IRentRepository
{
    Task<int> CountPendingRentsForBookAsync(Guid bookId);
    Task<bool> HasPendingRentAsync(Guid userId, Guid bookId);
    Task<bool> TryAddRentAsync(Rent rent);
    Task<Rent?> GetRentByIdAsync(Guid rentId);
    Task<List<Rent>> GetPendingRentsWithDetailsAsync();
    Task<List<Rent>> GetUserRentsWithBookAsync(Guid userId);
    Task<ApproveResult> ApproveRentAsync(Guid rentId, Guid bookId);
    Task<bool> RejectRentAsync(Guid rentId);
}