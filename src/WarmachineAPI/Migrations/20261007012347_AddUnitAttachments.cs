using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmachineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnitAttachmentId",
                table: "ArmyEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PointCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitAttachments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitAttachments");

            migrationBuilder.DropColumn(
                name: "UnitAttachmentId",
                table: "ArmyEntries");
        }
    }
}
