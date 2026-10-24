using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feryal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ColorId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_ColorId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ScentId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_SizeId",
                table: "ProductVariants");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "ProductVariants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_ColorId_SizeId_ScentId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ColorId", "SizeId", "ScentId" },
                unique: true,
                filter: "[ColorId] IS NOT NULL AND [SizeId] IS NOT NULL AND [ScentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_IsActive_IsConfigured",
                table: "ProductVariants",
                columns: new[] { "ProductId", "IsActive", "IsConfigured" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_ColorId_SizeId_ScentId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_IsActive_IsConfigured",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "ProductVariants");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ColorId",
                table: "ProductVariants",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_ColorId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ColorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ScentId",
                table: "ProductVariants",
                column: "ScentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SizeId",
                table: "ProductVariants",
                column: "SizeId");
        }
    }
}
