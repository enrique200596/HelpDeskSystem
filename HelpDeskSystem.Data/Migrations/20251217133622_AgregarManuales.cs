using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarManuales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContenidoHTML = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manuales_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Manuales_AutorId",
                table: "Manuales",
                column: "AutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Manuales");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Password",
                value: "$2a$11$NsGbd2xCvldF4FtasXrpc.gEaNIY2ZwPyfWIojppbsLG1Aq9jp8Qi");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Password",
                value: "$2a$11$WOwOhoJ0MA1xhy8jPtYZa.BG1PJ5uXpollFhUkJGk23ugobUdlGRa");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Password",
                value: "$2a$11$ieCvNSYJzgCLNlsoMcRnr.WtQgNq1otaEFVof8o9hkKJhfrqxwAO.");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "Password",
                value: "$2a$11$7/cq4p..gpQR.VQmiAPJROYtBr9.mC7BfsIN93Mla5GRghiMovI72");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Password",
                value: "$2a$11$pwAMCWT.UwnPgVTmzqnEJ.YFD/wrhKQTWv.Rt7H0xJEJW.N3EaOF6");
        }
    }
}
