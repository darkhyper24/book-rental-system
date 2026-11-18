public class PendingRentDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
}
