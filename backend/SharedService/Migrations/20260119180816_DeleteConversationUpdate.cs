using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedService.Migrations
{
    /// <inheritdoc />
    public partial class DeleteConversationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageReadReceipts_AspNetUsers_UserId",
                table: "MessageReadReceipts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
