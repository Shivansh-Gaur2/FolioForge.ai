using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolioForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_portfolios_Slug",
                table: "portfolios");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "portfolios",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_TenantId",
                table: "portfolios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_TenantId_Slug",
                table: "portfolios",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Identifier",
                table: "tenants",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_portfolios_TenantId",
                table: "portfolios");

            migrationBuilder.DropIndex(
                name: "IX_portfolios_TenantId_Slug",
                table: "portfolios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "portfolios");

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_Slug",
                table: "portfolios",
                column: "Slug",
                unique: true);
        }
    }
}
