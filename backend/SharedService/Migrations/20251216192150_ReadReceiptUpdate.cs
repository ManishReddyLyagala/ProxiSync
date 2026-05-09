using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedService.Migrations
{
    /// <inheritdoc />
    public partial class ReadReceiptUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MessageReadReceipts_UserId",
                table: "MessageReadReceipts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts");

            migrationBuilder.DropIndex(
                name: "IX_MessageReadReceipts_UserId",
                table: "MessageReadReceipts");
        }
    }
}
