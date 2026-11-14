public enum RentStatus
{
    Pending,
    Approved,
    Rejected
}
public class Rent
{
    public Rent(Guid bookId, Guid userId)
    {
        BookId = bookId;
        UserId = userId;
    }
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public Book? Book { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public DateTime RequestDate { get; set; }
    public RentStatus Status { get; set; }

}