using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RentConfiguration : IEntityTypeConfiguration<Rent>
{
    public void Configure(EntityTypeBuilder<Rent> builder)
    {
        builder.HasKey(rent => rent.Id);
        builder.Property(rent => rent.RequestDate).IsRequired();
        builder.Property(rent => rent.Status).IsRequired();
        builder.HasOne(rent => rent.Book)
            .WithMany()
            .HasForeignKey(rent => rent.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rent => rent.User)
            .WithMany()
            .HasForeignKey(rent => rent.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rent => rent.UserId);
        builder.HasIndex(rent => new { rent.UserId, rent.BookId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}
