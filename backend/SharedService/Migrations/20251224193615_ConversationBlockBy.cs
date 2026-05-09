using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedService.Migrations
{
    /// <inheritdoc />
    public partial class ConversationBlockBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockedByUserId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedByUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Conversations");
        }
    }
}
