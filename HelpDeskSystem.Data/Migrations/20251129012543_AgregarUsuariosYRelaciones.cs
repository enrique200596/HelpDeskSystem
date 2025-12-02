using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuariosYRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FotoPerfilUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "FotoPerfilUrl", "IsActive", "Nombre", "Rol" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "admin@helpdesk.com", "", true, "Administrador Jefe", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "juan@helpdesk.com", "", true, "Juan Asesor", 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "carla@cliente.com", "", true, "Carla Usuario", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AsesorId",
                table: "Tickets",
                column: "AsesorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UsuarioId",
                table: "Tickets",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Usuarios_AsesorId",
                table: "Tickets",
                column: "AsesorId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Usuarios_UsuarioId",
                table: "Tickets",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Usuarios_AsesorId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Usuarios_UsuarioId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AsesorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_UsuarioId",
                table: "Tickets");
        }
    }
}
