using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionPeriodEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionPeriods", x => x.Id);
                    table.CheckConstraint("CK_ExceptionPeriods_DateRange", "\"StartDate\" < \"EndDate\"");
                    table.CheckConstraint("CK_ExceptionPeriods_DescriptionLength", "\"Description\" IS NULL OR LENGTH(\"Description\") <= 500");
                    table.CheckConstraint("CK_ExceptionPeriods_NameLength", "LENGTH(\"Name\") > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionPeriods_UserActive",
                table: "ExceptionPeriods",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionPeriods_UserDate",
                table: "ExceptionPeriods",
                columns: new[] { "UserId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionPeriods_UserId",
                table: "ExceptionPeriods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionPeriods_UserType",
                table: "ExceptionPeriods",
                columns: new[] { "UserId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExceptionPeriods");
        }
    }
}
