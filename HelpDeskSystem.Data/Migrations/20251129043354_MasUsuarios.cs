using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "FotoPerfilUrl", "IsActive", "Nombre", "Password", "Rol" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "ana@helpdesk.com", "", true, "Ana Asesora", "1234", 2 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "pedro@cliente.com", "", true, "Pedro Cliente", "1234", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
