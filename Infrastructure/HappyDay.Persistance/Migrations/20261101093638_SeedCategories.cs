using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyDay.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mekan türleri. Daha önce seed.sql ile elle yükleniyordu; yeni sunucuda migration'la gelsin.
            // Var olan kategoriler (aynı adla) tekrar eklenmez, değiştirilmez.
            migrationBuilder.Sql(@"
INSERT INTO ""Categories"" (""Name"")
SELECT c.""Name""
FROM (VALUES
    ('Düğün Salonu'),
    ('Kır Bahçesi'),
    ('Kırık Ev'),
    ('Restaurant & Lounge'),
    ('Nikah Salonu'),
    ('Kongre & Toplantı Salonu'),
    ('Sosyal Tesis & Kulüp'),
    ('Rooftop & Teras'),
    ('Tekne & Yat'),
    ('Sanat Galerisi & Loft'),
    ('Otel & Balo Salonu'),
    ('Bahçe & Peyzaj')
) AS c(""Name"")
WHERE NOT EXISTS (SELECT 1 FROM ""Categories"" x WHERE x.""Name"" = c.""Name"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Kategorilere mekanlar bağlı; geri alırken silinmez

        }
    }
}
