using System.ComponentModel.DataAnnotations;

public class CreateBookDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Copies must be a positive number.")]
    public int CopiesAvailable { get; set; }
}
