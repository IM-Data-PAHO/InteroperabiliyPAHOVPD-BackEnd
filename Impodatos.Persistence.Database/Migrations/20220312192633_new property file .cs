using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Impodatos.Persistence.Database.Migrations
{
    public partial class newpropertyfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "File",
                table: "history",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "File",
                table: "history");
        }
    }
}
