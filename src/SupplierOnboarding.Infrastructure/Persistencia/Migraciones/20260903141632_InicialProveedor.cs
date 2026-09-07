using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplierOnboarding.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdentificadorFiscal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentificadorFiscalNormalizado = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreContacto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorreoContacto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistradoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistradoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Pais_IdentificadorFiscalNormalizado",
                table: "Proveedores",
                columns: new[] { "Pais", "IdentificadorFiscalNormalizado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
