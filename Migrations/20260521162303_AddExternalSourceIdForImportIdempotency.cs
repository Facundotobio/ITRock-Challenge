using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITRockChallenge.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalSourceIdForImportIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalSourceId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_ExternalSourceId",
                table: "Tasks",
                columns: new[] { "UserId", "ExternalSourceId" },
                unique: true,
                filter: "\"ExternalSourceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_ExternalSourceId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ExternalSourceId",
                table: "Tasks");
        }
    }
}
