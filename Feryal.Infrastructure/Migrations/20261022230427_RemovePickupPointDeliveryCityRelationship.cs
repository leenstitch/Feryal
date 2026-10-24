using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feryal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePickupPointDeliveryCityRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupPoints_DeliveryCities_DeliveryCityId",
                table: "PickupPoints");

            migrationBuilder.DropIndex(
                name: "IX_PickupPoints_DeliveryCityId",
                table: "PickupPoints");

            migrationBuilder.DropColumn(
                name: "DeliveryCityId",
                table: "PickupPoints");

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_IsActive",
                table: "PickupPoints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_Name",
                table: "PickupPoints",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PickupPoints_IsActive",
                table: "PickupPoints");

            migrationBuilder.DropIndex(
                name: "IX_PickupPoints_Name",
                table: "PickupPoints");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCityId",
                table: "PickupPoints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_DeliveryCityId",
                table: "PickupPoints",
                column: "DeliveryCityId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupPoints_DeliveryCities_DeliveryCityId",
                table: "PickupPoints",
                column: "DeliveryCityId",
                principalTable: "DeliveryCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
