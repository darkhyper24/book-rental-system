public class MyRentDto
{
    public Guid Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
