using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "Users",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                table: "Users",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProvider",
                table: "Users",
                columns: new[] { "ExternalProvider", "ExternalProviderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
