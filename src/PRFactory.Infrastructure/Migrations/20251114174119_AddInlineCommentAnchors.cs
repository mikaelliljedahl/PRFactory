using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInlineCommentAnchors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlanReviewId1",
                table: "ReviewChecklists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewChecklists_PlanReviewId1",
                table: "ReviewChecklists",
                column: "PlanReviewId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewChecklists_PlanReviews_PlanReviewId1",
                table: "ReviewChecklists",
                column: "PlanReviewId1",
                principalTable: "PlanReviews",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewChecklists_PlanReviews_PlanReviewId1",
                table: "ReviewChecklists");

            migrationBuilder.DropIndex(
                name: "IX_ReviewChecklists_PlanReviewId1",
                table: "ReviewChecklists");

            migrationBuilder.DropColumn(
                name: "PlanReviewId1",
                table: "ReviewChecklists");
        }
    }
}
