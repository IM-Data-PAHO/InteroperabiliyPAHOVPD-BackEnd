using Microsoft.EntityFrameworkCore.Migrations;

namespace Impodatos.Persistence.Database.Migrations
{
    public partial class _1606202201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserLogin",
                table: "history",
                newName: "userlogin");

            migrationBuilder.RenameColumn(
                name: "Uploads",
                table: "history",
                newName: "uploads");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "history",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "Programsid",
                table: "history",
                newName: "programsid");

            migrationBuilder.RenameColumn(
                name: "JsonSet",
                table: "history",
                newName: "jsonset");

            migrationBuilder.RenameColumn(
                name: "JsonResponse",
                table: "history",
                newName: "jsonresponse");

            migrationBuilder.RenameColumn(
                name: "File",
                table: "history",
                newName: "file");

            migrationBuilder.RenameColumn(
                name: "Fecha",
                table: "history",
                newName: "fecha");

            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "history",
                newName: "deleted");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "history",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_history_Uploads",
                table: "history",
                newName: "IX_history_uploads");

            migrationBuilder.RenameIndex(
                name: "IX_history_Id",
                table: "history",
                newName: "IX_history_id");

            migrationBuilder.RenameIndex(
                name: "IX_history_Deleted",
                table: "history",
                newName: "IX_history_deleted");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "userlogin",
                table: "history",
                newName: "UserLogin");

            migrationBuilder.RenameColumn(
                name: "uploads",
                table: "history",
                newName: "Uploads");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "history",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "programsid",
                table: "history",
                newName: "Programsid");

            migrationBuilder.RenameColumn(
                name: "jsonset",
                table: "history",
                newName: "JsonSet");

            migrationBuilder.RenameColumn(
                name: "jsonresponse",
                table: "history",
                newName: "JsonResponse");

            migrationBuilder.RenameColumn(
                name: "file",
                table: "history",
                newName: "File");

            migrationBuilder.RenameColumn(
                name: "fecha",
                table: "history",
                newName: "Fecha");

            migrationBuilder.RenameColumn(
                name: "deleted",
                table: "history",
                newName: "Deleted");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "history",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_history_uploads",
                table: "history",
                newName: "IX_history_Uploads");

            migrationBuilder.RenameIndex(
                name: "IX_history_id",
                table: "history",
                newName: "IX_history_Id");

            migrationBuilder.RenameIndex(
                name: "IX_history_deleted",
                table: "history",
                newName: "IX_history_Deleted");
        }
    }
}
