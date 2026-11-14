using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Title).IsRequired().HasMaxLength(100);
        builder.Property(book => book.Author).IsRequired().HasMaxLength(50);
        builder.Property(book => book.Isbn).IsRequired().HasMaxLength(20);
        builder.Property(book => book.CopiesAvailable).IsRequired();
        builder.HasIndex(book => book.Isbn).IsUnique();
        
    }
}