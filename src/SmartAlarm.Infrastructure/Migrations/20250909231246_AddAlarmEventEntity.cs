using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAlarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmEventEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHolidayPreferences_Holidays",
                table: "UserHolidayPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHolidayPreferences_Users",
                table: "UserHolidayPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserHolidayPreferences_UserId_HolidayId",
                table: "UserHolidayPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserHolidayPreferences_UserId_IsEnabled",
                table: "UserHolidayPreferences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserHolidayPreferences_DelayInMinutes",
                table: "UserHolidayPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Roles_IsActive",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_Recurring",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_ExceptionPeriods_UserActive",
                table: "ExceptionPeriods");

            migrationBuilder.DropIndex(
                name: "IX_ExceptionPeriods_UserDate",
                table: "ExceptionPeriods");

            migrationBuilder.DropIndex(
                name: "IX_ExceptionPeriods_UserId",
                table: "ExceptionPeriods");

            migrationBuilder.DropIndex(
                name: "IX_ExceptionPeriods_UserType",
                table: "ExceptionPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExceptionPeriods_DateRange",
                table: "ExceptionPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExceptionPeriods_DescriptionLength",
                table: "ExceptionPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExceptionPeriods_NameLength",
                table: "ExceptionPeriods");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserRoles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoles",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "UserHolidayPreferences",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DisableAlarms",
                table: "UserHolidayPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "Integrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Integrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Integrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Integrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "Integrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Integrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Holidays",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Holidays",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Holidays",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Holidays",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Holidays",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "ExceptionPeriods",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ExceptionPeriods",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "ExceptionPeriods",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ExceptionPeriods",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "AlarmEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlarmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    Time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    SnoozeMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DeviceInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmEvents_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_IsActive",
                table: "UserRoles",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_AlarmId",
                table: "AlarmEvents",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_AlarmId_EventType_Timestamp",
                table: "AlarmEvents",
                columns: new[] { "AlarmId", "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_EventType",
                table: "AlarmEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_Timestamp",
                table: "AlarmEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_UserId",
                table: "AlarmEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_UserId_Timestamp",
                table: "AlarmEvents",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserHolidayPreferences_Holidays_HolidayId",
                table: "UserHolidayPreferences",
                column: "HolidayId",
                principalTable: "Holidays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHolidayPreferences_Users_UserId",
                table: "UserHolidayPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHolidayPreferences_Holidays_HolidayId",
                table: "UserHolidayPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHolidayPreferences_Users_UserId",
                table: "UserHolidayPreferences");

            migrationBuilder.DropTable(
                name: "AlarmEvents");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_IsActive",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DisableAlarms",
                table: "UserHolidayPreferences");

            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Holidays");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "UserRoles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "UserHolidayPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "ExceptionPeriods",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ExceptionPeriods",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "ExceptionPeriods",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ExceptionPeriods",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_UserId_HolidayId",
                table: "UserHolidayPreferences",
                columns: new[] { "UserId", "HolidayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserHolidayPreferences_UserId_IsEnabled",
                table: "UserHolidayPreferences",
                columns: new[] { "UserId", "IsEnabled" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserHolidayPreferences_DelayInMinutes",
                table: "UserHolidayPreferences",
                sql: "(Action != 2) OR (Action = 2 AND DelayInMinutes IS NOT NULL AND DelayInMinutes > 0 AND DelayInMinutes <= 1440)");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive",
                table: "Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Recurring",
                table: "Holidays",
                column: "Date",
                filter: "date(Date) LIKE '0001-%'");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExceptionPeriods_DateRange",
                table: "ExceptionPeriods",
                sql: "\"StartDate\" < \"EndDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExceptionPeriods_DescriptionLength",
                table: "ExceptionPeriods",
                sql: "\"Description\" IS NULL OR LENGTH(\"Description\") <= 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExceptionPeriods_NameLength",
                table: "ExceptionPeriods",
                sql: "LENGTH(\"Name\") > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_UserHolidayPreferences_Holidays",
                table: "UserHolidayPreferences",
                column: "HolidayId",
                principalTable: "Holidays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHolidayPreferences_Users",
                table: "UserHolidayPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
