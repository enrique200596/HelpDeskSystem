using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImplementarCategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioCategoria",
                columns: table => new
                {
                    AsesoresId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriasId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioCategoria", x => new { x.AsesoresId, x.CategoriasId });
                    table.ForeignKey(
                        name: "FK_UsuarioCategoria_Categorias_CategoriasId",
                        column: x => x.CategoriasId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioCategoria_Usuarios_AsesoresId",
                        column: x => x.AsesoresId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Sistema Financiero" },
                    { 2, "Sistema Genesis" },
                    { 3, "Reportes" }
                });

            migrationBuilder.InsertData(
                table: "UsuarioCategoria",
                columns: new[] { "AsesoresId", "CategoriasId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 2 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategoriaId",
                table: "Tickets",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioCategoria_CategoriasId",
                table: "UsuarioCategoria",
                column: "CategoriasId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Categorias_CategoriaId",
                table: "Tickets",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Categorias_CategoriaId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "UsuarioCategoria");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategoriaId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Tickets");
        }
    }
}
