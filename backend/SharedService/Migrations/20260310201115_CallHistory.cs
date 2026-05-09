using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedService.Migrations
{
    /// <inheritdoc />
    public partial class CallHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CallParticipants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CallParticipants_UserId",
                table: "CallParticipants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_AspNetUsers_UserId",
                table: "CallParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallSessions_Conversations_ConversationId",
                table: "CallSessions",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "ConversationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallParticipants_AspNetUsers_UserId",
                table: "CallParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CallSessions_Conversations_ConversationId",
                table: "CallSessions");

            migrationBuilder.DropIndex(
                name: "IX_CallParticipants_UserId",
                table: "CallParticipants");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CallParticipants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
