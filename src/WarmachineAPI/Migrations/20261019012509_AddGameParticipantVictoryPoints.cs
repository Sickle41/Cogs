using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmachineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGameParticipantVictoryPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VictoryPoints",
                table: "GameParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VictoryPoints",
                table: "GameParticipants");
        }
    }
}
