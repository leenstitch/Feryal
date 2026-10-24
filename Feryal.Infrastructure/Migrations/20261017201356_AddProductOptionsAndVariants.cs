using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feryal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOptionsAndVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Colors_ColorId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Scents_ScentId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Sizes_SizeId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Sizes_Name",
                table: "Sizes");

            migrationBuilder.DropIndex(
                name: "IX_Scents_Name",
                table: "Scents");

            migrationBuilder.DropIndex(
                name: "IX_Colors_Name",
                table: "Colors");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Sizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Scents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ProductVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "ProductGroupId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Colors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sizes_ProductId_Id",
                table: "Sizes",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Scents_ProductId_Id",
                table: "Scents",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Colors_ProductId_Id",
                table: "Colors",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_ProductId",
                table: "Sizes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_ProductId_Id",
                table: "Sizes",
                columns: new[] { "ProductId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_ProductId_Name",
                table: "Sizes",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scents_ProductId",
                table: "Scents",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Scents_ProductId_Id",
                table: "Scents",
                columns: new[] { "ProductId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scents_ProductId_Name",
                table: "Scents",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_ColorId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ColorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_ScentId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ScentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_SizeId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SizeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Colors_ProductId",
                table: "Colors",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Colors_ProductId_Id",
                table: "Colors",
                columns: new[] { "ProductId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colors_ProductId_Name",
                table: "Colors",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_Products_ProductId",
                table: "Colors",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Colors_ProductId_ColorId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ColorId" },
                principalTable: "Colors",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Scents_ProductId_ScentId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "ScentId" },
                principalTable: "Scents",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Sizes_ProductId_SizeId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SizeId" },
                principalTable: "Sizes",
                principalColumns: new[] { "ProductId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scents_Products_ProductId",
                table: "Scents",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sizes_Products_ProductId",
                table: "Sizes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_Products_ProductId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Colors_ProductId_ColorId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Scents_ProductId_ScentId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Sizes_ProductId_SizeId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_Scents_Products_ProductId",
                table: "Scents");

            migrationBuilder.DropForeignKey(
                name: "FK_Sizes_Products_ProductId",
                table: "Sizes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sizes_ProductId_Id",
                table: "Sizes");

            migrationBuilder.DropIndex(
                name: "IX_Sizes_ProductId",
                table: "Sizes");

            migrationBuilder.DropIndex(
                name: "IX_Sizes_ProductId_Id",
                table: "Sizes");

            migrationBuilder.DropIndex(
                name: "IX_Sizes_ProductId_Name",
                table: "Sizes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Scents_ProductId_Id",
                table: "Scents");

            migrationBuilder.DropIndex(
                name: "IX_Scents_ProductId",
                table: "Scents");

            migrationBuilder.DropIndex(
                name: "IX_Scents_ProductId_Id",
                table: "Scents");

            migrationBuilder.DropIndex(
                name: "IX_Scents_ProductId_Name",
                table: "Scents");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_ColorId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_ScentId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_SizeId",
                table: "ProductVariants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Colors_ProductId_Id",
                table: "Colors");

            migrationBuilder.DropIndex(
                name: "IX_Colors_ProductId",
                table: "Colors");

            migrationBuilder.DropIndex(
                name: "IX_Colors_ProductId_Id",
                table: "Colors");

            migrationBuilder.DropIndex(
                name: "IX_Colors_ProductId_Name",
                table: "Colors");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Sizes");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Scents");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Colors");

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ProductVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductGroupId",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_Name",
                table: "Sizes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scents_Name",
                table: "Scents",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colors_Name",
                table: "Colors",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Colors_ColorId",
                table: "ProductVariants",
                column: "ColorId",
                principalTable: "Colors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Scents_ScentId",
                table: "ProductVariants",
                column: "ScentId",
                principalTable: "Scents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Sizes_SizeId",
                table: "ProductVariants",
                column: "SizeId",
                principalTable: "Sizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
