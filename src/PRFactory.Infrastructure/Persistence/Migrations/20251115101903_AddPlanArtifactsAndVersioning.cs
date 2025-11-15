using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanArtifactsAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestCases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementationSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    UserStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestCases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementationSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    RevisionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_TicketId",
                table: "Plans",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PlanId",
                table: "PlanVersions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "UQ_PlanVersions_PlanId_Version",
                table: "PlanVersions",
                columns: new[] { "PlanId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanVersions");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
