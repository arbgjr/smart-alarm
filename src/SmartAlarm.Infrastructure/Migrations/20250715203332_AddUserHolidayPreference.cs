using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHolidayPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHolidayPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HolidayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayInMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHolidayPreferences", x => x.Id);
                    table.CheckConstraint("CK_UserHolidayPreferences_DelayInMinutes", "(\"Action\" <> 2) OR (\"Action\" = 2 AND \"DelayInMinutes\" IS NOT NULL AND \"DelayInMinutes\" > 0 AND \"DelayInMinutes\" <= 1440)");
                    table.ForeignKey(
                        name: "FK_UserHolidayPreferences_Holidays",
                        column: x => x.HolidayId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserHolidayPreferences_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_HolidayId",
                table: "UserHolidayPreferences",
                column: "HolidayId");

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_UserId",
                table: "UserHolidayPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_UserId_HolidayId",
                table: "UserHolidayPreferences",
                columns: new[] { "UserId", "HolidayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_UserId_IsEnabled",
                table: "UserHolidayPreferences",
                columns: new[] { "UserId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHolidayPreferences");
        }
    }
}
