using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feryal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedQuantityToProductVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                newName: "UX_Orders_OrderNumber");

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneNumber",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReservationToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservationItems", x => x.Id);
                    table.CheckConstraint("CK_InventoryReservationItems_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_InventoryReservationItems_InventoryReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "InventoryReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReservationItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity_NonNegative",
                table: "ProductVariants",
                sql: "[ReservedQuantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity_NotAboveStock",
                table: "ProductVariants",
                sql: "[StockQuantity] IS NULL OR [ReservedQuantity] <= [StockQuantity]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_StockQuantity_NonNegative",
                table: "ProductVariants",
                sql: "[StockQuantity] IS NULL OR [StockQuantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_UserId_IdempotencyKey",
                table: "Orders",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems",
                sql: "[Quantity] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservationItems_ProductVariantId",
                table: "InventoryReservationItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservationItems_ReservationId_ProductVariantId",
                table: "InventoryReservationItems",
                columns: new[] { "ReservationId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ReservationToken",
                table: "InventoryReservations",
                column: "ReservationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Status_ExpiresAt",
                table: "InventoryReservations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_UserId_IdempotencyKey",
                table: "InventoryReservations",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_InventoryReservations_Active_UserId",
                table: "InventoryReservations",
                column: "UserId",
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryReservationItems");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity_NotAboveStock",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_StockQuantity_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "UX_Orders_UserId_IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Orders");

            migrationBuilder.RenameIndex(
                name: "UX_Orders_OrderNumber",
                table: "Orders",
                newName: "IX_Orders_OrderNumber");
        }
    }
}
