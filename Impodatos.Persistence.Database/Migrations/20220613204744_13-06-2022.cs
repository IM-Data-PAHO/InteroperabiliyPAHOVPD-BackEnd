using Microsoft.EntityFrameworkCore.Migrations;

namespace Impodatos.Persistence.Database.Migrations
{
    public partial class _13062022 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Deleted",
                table: "history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Uploads",
                table: "history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_history_Deleted",
                table: "history",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_history_Uploads",
                table: "history",
                column: "Uploads");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_history_Deleted",
                table: "history");

            migrationBuilder.DropIndex(
                name: "IX_history_Uploads",
                table: "history");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "history");

            migrationBuilder.DropColumn(
                name: "Uploads",
                table: "history");
        }
    }
}
