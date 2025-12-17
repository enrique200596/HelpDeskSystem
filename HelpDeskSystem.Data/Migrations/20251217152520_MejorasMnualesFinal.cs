using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class MejorasMnualesFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Etiquetas",
                table: "Manuales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Manuales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Manuales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RolesVisibles",
                table: "Manuales",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ManualLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManualId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualLogs_Manuales_ManualId",
                        column: x => x.ManualId,
                        principalTable: "Manuales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualLogs_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Password",
                value: "$2a$11$2.LI0se.9Yu9ciOs7ep7AOALc1gwhiM.7Ph6co.tEOsk6z4Y5LYd2");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Password",
                value: "$2a$11$wnobxBfTwCvF3luvG9RB3O8w5NabDY503sR1oPgeUO9.QuQ5ht5BS");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Password",
                value: "$2a$11$1N9Vzeg4ofoWCe8XQdofk.RcLnHLsaFjLMIYC7aJlng/lAi1ozCIm");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "Password",
                value: "$2a$11$sAoShInM36jYw8qTy/JvMus7g3KI2uK5WzMjbvCM5d/JH7qu8yo2e");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Password",
                value: "$2a$11$z2ypKqowxpTlouXAfjEU0.Uy4hF2P6V4QcY73JDOjv9PSwNub.E6C");

            migrationBuilder.CreateIndex(
                name: "IX_ManualLogs_ManualId",
                table: "ManualLogs",
                column: "ManualId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualLogs_UsuarioId",
                table: "ManualLogs",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualLogs");

            migrationBuilder.DropColumn(
                name: "Etiquetas",
                table: "Manuales");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Manuales");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Manuales");

            migrationBuilder.DropColumn(
                name: "RolesVisibles",
                table: "Manuales");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Password",
                value: "$2a$11$YcIayIG7h.6aosFQ/ZchYuxpKzeLekasWsmvJlr2JEDZvNhjadQKS");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Password",
                value: "$2a$11$JMAEZo3ks/xqXSzaBYq4VubmnpIn4pA4aXdsDmLKc6.goL.2UNjQy");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Password",
                value: "$2a$11$r9zJKLFVn3kskR.YX5Lsje4agHr6dPd6VPpVe/0CsNySXg9LY4RgG");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "Password",
                value: "$2a$11$fj91JzhalMZd4.tOb2mYVelRIT455HW9U4sewihNm1fBPN0UhBgke");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Password",
                value: "$2a$11$x5HAx6Ta3HdXdHsfl/3m4ecef.qsgauouv2CBdEIeuzlOz8PvdKa2");
        }
    }
}
