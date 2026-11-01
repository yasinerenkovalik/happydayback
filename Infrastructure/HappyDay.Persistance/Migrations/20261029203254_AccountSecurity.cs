using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyDay.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AccountSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IdentityNo",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Eski int sütununda boş değer 0 olarak tutuluyordu
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""IdentityNo"" = NULL WHERE ""IdentityNo"" = '0';");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            // E-posta tekilliği: aynı e-postayla (büyük/küçük harf farkı dahil) iki aktif hesap olamaz.
            // Pasifleştirilmiş (silinmiş) hesaplar indekse girmez, aynı e-postayla yeniden kayıt açılabilir.
            // EF Core ifade indeksini modelde tanımlayamadığı için SQL ile oluşturuluyor; sorgular
            // lower("Email") ile aradığı için bu indeksi kullanır.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    dup_users text;
    dup_companies text;
BEGIN
    SELECT string_agg(e, ', ') INTO dup_users FROM (
        SELECT lower(btrim(""Email"")) AS e FROM ""Users""
        WHERE ""IsActivated"" GROUP BY 1 HAVING count(*) > 1) d;
    SELECT string_agg(e, ', ') INTO dup_companies FROM (
        SELECT lower(btrim(""Email"")) AS e FROM ""Companies""
        WHERE ""IsActivated"" GROUP BY 1 HAVING count(*) > 1) d;

    IF dup_users IS NOT NULL OR dup_companies IS NOT NULL THEN
        RAISE EXCEPTION 'Aynı e-postayı kullanan birden fazla aktif hesap var. Users: %, Companies: %. Fazla kayıtları pasifleştirip migration''ı yeniden çalıştırın.',
            coalesce(dup_users, '-'), coalesce(dup_companies, '-');
    END IF;
END $$;

UPDATE ""Users"" SET ""Email"" = lower(btrim(""Email"")) WHERE ""Email"" <> lower(btrim(""Email""));
UPDATE ""Companies"" SET ""Email"" = lower(btrim(""Email"")) WHERE ""Email"" <> lower(btrim(""Email""));

CREATE UNIQUE INDEX ""IX_Users_Email_Lower"" ON ""Users"" (lower(""Email"")) WHERE ""IsActivated"";
CREATE UNIQUE INDEX ""IX_Companies_Email_Lower"" ON ""Companies"" (lower(""Email"")) WHERE ""IsActivated"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_Users_Email_Lower"";
DROP INDEX IF EXISTS ""IX_Companies_Email_Lower"";");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Companies");

            // 11 haneli T.C. kimlik no int'e sığmaz; geri dönüşte bu değerler 0'a düşer.
            // text → integer dönüşümü USING ister, EF'in AlterColumn'u bunu üretmediği için SQL ile.
            migrationBuilder.Sql(@"
UPDATE ""Users"" SET ""IdentityNo"" = '0' WHERE ""IdentityNo"" IS NULL OR ""IdentityNo"" !~ '^[0-9]{1,9}$';
ALTER TABLE ""Users""
    ALTER COLUMN ""IdentityNo"" TYPE integer USING ""IdentityNo""::integer,
    ALTER COLUMN ""IdentityNo"" SET NOT NULL,
    ALTER COLUMN ""IdentityNo"" SET DEFAULT 0;");
        }
    }
}
