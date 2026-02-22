using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloosCash.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftCashiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftUser",
                columns: table => new
                {
                    CashiersId = table.Column<int>(type: "int", nullable: false),
                    ShiftsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftUser", x => new { x.CashiersId, x.ShiftsId });
                    table.ForeignKey(
                        name: "FK_ShiftUser_Shifts_ShiftsId",
                        column: x => x.ShiftsId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftUser_Users_CashiersId",
                        column: x => x.CashiersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftUser_ShiftsId",
                table: "ShiftUser",
                column: "ShiftsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftUser");
        }
    }
}
