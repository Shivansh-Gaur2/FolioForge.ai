using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolioForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetRazorpayPlanId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "StripePriceMonthlyId",
                value: "plan_SUATD5lUCBKQnG");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"),
                column: "StripePriceMonthlyId",
                value: null);
        }
    }
}
