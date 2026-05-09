using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedService.Migrations
{
    /// <inheritdoc />
    public partial class AddCallTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallSessions",
                columns: table => new
                {
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsGroupCall = table.Column<bool>(type: "bit", nullable: false),
                    LiveKitSid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSessions", x => x.CallId);
                });

            migrationBuilder.CreateTable(
                name: "CallParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMicEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsVideoEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Joined = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Left = table.Column<bool>(type: "bit", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallParticipants_CallSessions_CallId",
                        column: x => x.CallId,
                        principalTable: "CallSessions",
                        principalColumn: "CallId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallParticipants_CallId",
                table: "CallParticipants",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_ConversationId",
                table: "CallSessions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_RoomName",
                table: "CallSessions",
                column: "RoomName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallParticipants");

            migrationBuilder.DropTable(
                name: "CallSessions");
        }
    }
}
