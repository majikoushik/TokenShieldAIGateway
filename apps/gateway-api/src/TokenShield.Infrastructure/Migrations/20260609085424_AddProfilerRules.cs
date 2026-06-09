using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenShield.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilerRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfilerRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetTaskType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhrasesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RegexPatternsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilerRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilerRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfilerRules_TenantId",
                table: "ProfilerRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfilerRules_TenantId_IsDeleted",
                table: "ProfilerRules",
                columns: new[] { "TenantId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfilerRules");
        }
    }
}
