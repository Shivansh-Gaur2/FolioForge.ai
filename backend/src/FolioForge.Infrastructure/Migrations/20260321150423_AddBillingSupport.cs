using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FolioForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiParsesResetAt",
                table: "users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "AiParsesUsedThisMonth",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PriceMonthlyInCents = table.Column<int>(type: "int", nullable: false),
                    PriceYearlyInCents = table.Column<int>(type: "int", nullable: false),
                    MaxPortfolios = table.Column<int>(type: "int", nullable: false),
                    MaxAiParsesPerMonth = table.Column<int>(type: "int", nullable: false),
                    CustomDomain = table.Column<bool>(type: "bit", nullable: false),
                    RemoveWatermark = table.Column<bool>(type: "bit", nullable: false),
                    PasswordProtection = table.Column<bool>(type: "bit", nullable: false),
                    Analytics = table.Column<bool>(type: "bit", nullable: false),
                    StripePriceMonthlyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StripePriceYearlyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "plans",
                columns: new[] { "Id", "Analytics", "CreatedAt", "CustomDomain", "MaxAiParsesPerMonth", "MaxPortfolios", "Name", "PasswordProtection", "PriceMonthlyInCents", "PriceYearlyInCents", "RemoveWatermark", "Slug", "StripePriceMonthlyId", "StripePriceYearlyId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 1, 1, "Free", false, 0, 0, false, "free", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000011"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 100, "Pro", true, 999, 9990, true, "pro", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "tenants",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "Name", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "folioforge", true, "FolioForge", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_plans_Slug",
                table: "plans",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DeleteData(
                table: "tenants",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "AiParsesResetAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AiParsesUsedThisMonth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "users");
        }
    }
}
