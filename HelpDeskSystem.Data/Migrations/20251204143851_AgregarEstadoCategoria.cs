using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Categorias");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Password",
                value: "$2a$11$ruXUOTWfMhLJKuNXXfa3Reas/rUD7b7KYWznolGfR0ukk6ZH6bXq2");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Password",
                value: "$2a$11$tasRir/EfnIzNdNRCMxpTuKEcJMAFE6d5uohxTVlGcMbyq83FirqS");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Password",
                value: "$2a$11$9pYtyhnjgKjXKULQ/3l6k.23aT9k0F08RVsa9G52HIz6Nqi0Kig9m");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "Password",
                value: "$2a$11$iU5J0yt6HNAcTS9B4E96yeWGsDrWcV4Ip8IsyyA6RRWrGMo5nk53y");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Password",
                value: "$2a$11$XnGQ8GGrBBlzbnWzvBcsT.d7CxruCj3ccR8gHoHfXKsBQ7zwslTj6");
        }
    }
}
