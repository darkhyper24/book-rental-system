using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookRental.DataAccessLayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePendingRentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rents_UserId_BookId",
                table: "Rents",
                columns: new[] { "UserId", "BookId" },
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rents_UserId_BookId",
                table: "Rents");
        }
    }
}
