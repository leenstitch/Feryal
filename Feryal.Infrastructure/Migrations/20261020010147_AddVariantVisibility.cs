using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feryal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsConfigured",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsConfigured",
                table: "ProductVariants");
        }
    }
}
