using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyDay.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class VenueLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Cities_CityId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Districts_DistrictId",
                table: "Organizations");

            migrationBuilder.AddColumn<string>(
                name: "Adress",
                table: "Organizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Organizations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Organizations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Mevcut mekanlar firmanın konumunu alır. İl/ilçe daha önce doldurulmuşsa korunur.
            migrationBuilder.Sql(@"
UPDATE ""Organizations"" o
SET ""CityId""     = COALESCE(o.""CityId"", c.""CityId""),
    ""DistrictId"" = COALESCE(o.""DistrictId"", c.""DistrictId""),
    ""Adress""     = c.""Adress"",
    ""Latitude""   = c.""Latitude"",
    ""Longitude""  = c.""Longitude""
FROM ""Companies"" c
WHERE c.""Id"" = o.""CompanyId"";");

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Cities_CityId",
                table: "Organizations",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Districts_DistrictId",
                table: "Organizations",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Cities_CityId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Districts_DistrictId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Adress",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Organizations");

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "Organizations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Organizations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Cities_CityId",
                table: "Organizations",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Districts_DistrictId",
                table: "Organizations",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id");
        }
    }
}
