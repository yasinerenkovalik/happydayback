-- ============================================================
--  Mutlu Günüm: örnek veriyi kaldırır (deploy/ornek-veri.sql'in eklediği her şey)
--  Yalnızca ornek-*@mutlugunum.invalid firmalarına ve onların kayıtlarına dokunur.
--
--  Coolify → PostgreSQL → Terminal'de:
--    wget -qO- https://raw.githubusercontent.com/yasinerenkovalik/happydayback/main/deploy/ornek-veri-sil.sql | psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1
-- ============================================================

BEGIN;

CREATE TEMP TABLE ornek_companies ON COMMIT DROP AS
  SELECT "Id" FROM "Companies" WHERE "Email" LIKE 'ornek-%@mutlugunum.invalid';
CREATE TEMP TABLE ornek_orgs ON COMMIT DROP AS
  SELECT "Id" FROM "Organizations" WHERE "CompanyId" IN (SELECT "Id" FROM ornek_companies);
DELETE FROM "ContactMessages" WHERE "OrganizationId" IN (SELECT "Id" FROM ornek_orgs) OR "CompanyId" IN (SELECT "Id" FROM ornek_companies);
DELETE FROM "Packages" WHERE "OrganizationId" IN (SELECT "Id" FROM ornek_orgs);
DELETE FROM "OrganizationImages" WHERE "OrganizationId" IN (SELECT "Id" FROM ornek_orgs);
DELETE FROM "CalendarEvents" WHERE "CompanyId" IN (SELECT "Id" FROM ornek_companies);
DELETE FROM "Organizations" WHERE "Id" IN (SELECT "Id" FROM ornek_orgs);
DELETE FROM "EmailVerificationTokens" WHERE "CompanyId" IN (SELECT "Id" FROM ornek_companies);
DELETE FROM "Companies" WHERE "Id" IN (SELECT "Id" FROM ornek_companies);

COMMIT;

SELECT (SELECT COUNT(*) FROM "Companies" WHERE "Email" LIKE 'ornek-%@mutlugunum.invalid') AS ornek_firma,
       (SELECT COUNT(*) FROM "Organizations" o JOIN "Companies" c ON c."Id" = o."CompanyId"
         WHERE c."Email" LIKE 'ornek-%@mutlugunum.invalid') AS ornek_mekan;
