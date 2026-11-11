using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmachineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitWeaponStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MeleeWeaponPow",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MeleeWeaponRange",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RangedWeaponAoe",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RangedWeaponBlastPow",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RangedWeaponIsSpray",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RangedWeaponPow",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RangedWeaponRange",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RangedWeaponRof",
                table: "UnitDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeleeWeaponPow",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "MeleeWeaponRange",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponAoe",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponBlastPow",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponIsSpray",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponPow",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponRange",
                table: "UnitDefinitions");

            migrationBuilder.DropColumn(
                name: "RangedWeaponRof",
                table: "UnitDefinitions");
        }
    }
}
