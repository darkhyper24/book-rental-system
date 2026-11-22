public interface IRentService
{
    Task<List<string>> RequestRentAsync(Guid userId, Guid bookId);
    Task<List<PendingRentDto>> GetPendingRentsAsync();
    Task<List<string>> ApproveRentAsync(Guid rentId);
    Task<List<string>> RejectRentAsync(Guid rentId);
    Task<List<MyRentDto>> GetUserRentsAsync(Guid userId);
    
}