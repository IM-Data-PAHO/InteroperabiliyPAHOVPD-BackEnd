using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Impodatos.Persistence.Database.Migrations
{
    public partial class _04052022 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trackedhistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trackedEntityInstance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mxKJ869xJOd = table.Column<string>(type: "text", nullable: false),
                    kR6TpjXjMP7 = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<bool>(type: "boolean", nullable: false),
                    UserLogin = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trackedhistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trackedhistory_Id",
                table: "Trackedhistory",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trackedhistory");
        }
    }
}
