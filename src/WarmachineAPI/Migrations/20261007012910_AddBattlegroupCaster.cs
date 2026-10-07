using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmachineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBattlegroupCaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BattlegroupCasterEntryId",
                table: "ArmyEntries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BattlegroupCasterEntryId",
                table: "ArmyEntries");
        }
    }
}
