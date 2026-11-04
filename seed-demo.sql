-- ============================================================
--  HappyDay / Davetly — GELİŞTİRME İÇİN örnek veri
--  5 kurgusal firma, 13 mekan, görseller ve paketler.
--  Üretim veritabanında ÇALIŞTIRMAYIN.
--
--  Çalıştırma (migration'lar ve seed.sql'den sonra):
--    docker exec -i happyday-postgres psql -U postgres -d HappyDayDb -v ON_ERROR_STOP=1 < seed-demo.sql
--
--  Admin girişi: seed-admin@davetly.local / Davetly2026! (POST api/auth/login)
--  Firma paneli girişi: seed-<firma>@davetly.local / Davetly2026!
--    (bogazici, polonez, ege, baskent, bodrum). Şifre hash'i yerel user-secrets'taki
--    Security:Pepper ile üretildi; farklı pepper'la çalışan ortamda giriş yapılamaz.
--
--  Tekrar çalıştırılabilir: önce kendi kayıtlarını (seed-*@davetly.local) siler.
--  Görseller Stitch tasarımındaki fotoğraflardır (lh3.googleusercontent.com).
-- ============================================================

BEGIN;

-- Önceki seed ve önizleme kayıtlarını temizle
CREATE TEMP TABLE seed_companies ON COMMIT DROP AS
  SELECT "Id" FROM "Companies" WHERE "Email" LIKE 'seed-%@davetly.local' OR "Email" LIKE 'demo-%@davetly.local';
CREATE TEMP TABLE seed_orgs ON COMMIT DROP AS
  SELECT "Id" FROM "Organizations" WHERE "CompanyId" IN (SELECT "Id" FROM seed_companies);
DELETE FROM "ContactMessages" WHERE "OrganizationId" IN (SELECT "Id" FROM seed_orgs) OR "CompanyId" IN (SELECT "Id" FROM seed_companies);
DELETE FROM "Packages" WHERE "OrganizationId" IN (SELECT "Id" FROM seed_orgs);
DELETE FROM "OrganizationImages" WHERE "OrganizationId" IN (SELECT "Id" FROM seed_orgs);
DELETE FROM "CalendarEvents" WHERE "CompanyId" IN (SELECT "Id" FROM seed_companies);
DELETE FROM "Organizations" WHERE "Id" IN (SELECT "Id" FROM seed_orgs);
DELETE FROM "EmailVerificationTokens" WHERE "CompanyId" IN (SELECT "Id" FROM seed_companies);
DELETE FROM "Companies" WHERE "Id" IN (SELECT "Id" FROM seed_companies);
DELETE FROM "Users" WHERE "Email" LIKE 'seed-%@davetly.local';

-- Admin kullanıcısı (POST api/auth/login ile giriş yapar)
INSERT INTO "Users" ("Id", "Name", "SurName", "Email", "PasswordHash", "BirtDay", "IdentityNo", "Adress", "PhoneNumber", "Sex",
  "Role", "CompanyId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('15a9cffb-e616-5b72-8b84-d49bb94776a1', 'Davetly', 'Admin', 'seed-admin@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', '1990-01-01', 0, 'İstanbul', '5550000000', false,
  'Admin', NULL, now(), now(), '-infinity', true);

-- Boğaziçi Davet Evleri
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('aa6416b4-0398-542d-9681-e70a2a5c05c4', 'Boğaziçi Davet Evleri', 'seed-bogazici@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', 'Köybaşı Cad. No:12, Yeniköy, Sarıyer / İstanbul', '5550000000',
  'Boğaziçi Davet Evleri — örnek firma', 41.1186, 29.0606, 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Sarıyer'), true, true, now(), 'https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw',
  now(), now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('08dc7a4e-afbd-51ae-83c5-7c76d8113684', 'Yeniköy Yalı Balo Salonu', 7, 'Yeniköy sahilindeki tarihi yalının iki katlı balo salonu, yüksek tavanları ve denize bakan pencereleriyle büyük düğünlere ev sahipliği yapıyor. Salon 650 kişiye kadar oturmalı yemek düzenine uygun.

Kendi mutfağımızda hazırlanan menüler için düğün öncesi tadım yapılır. Salonun arkasındaki bahçe, yemek öncesi karşılama kokteyli için kullanılabilir.', 135000, 650, ARRAY['Yemekli Menü', 'Canlı Müzik İzni', 'Vale Hizmeti', 'Deniz Manzarası', 'Gelin Hazırlık Odası', 'Engelli Erişimi', 'Jeneratör', 'Ses Sistemi']::text[], '8 saat',
  false, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, true, 'aa6416b4-0398-542d-9681-e70a2a5c05c4', 'https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Sarıyer'), 'Yeniköy Mah. Yalı Sok. No:14, Sarıyer / İstanbul', 41.1176, 29.0648,
  now() - interval '1 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw', '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDzpHa3a2bs-Y-BwsWCCEzpyq-fRMWsIpXI2wAq4-0u4wNc5CDmTAkicoHZi377IrgRUd_67E34uVm51Gg5SeBXCpVf0MQ2onUA1kBs7YI6w895Cbm4bYMZFNfGwdXIomKuCVXslaRpCyBFArNG9k89bJaxgdcU1wrt23Lo34E8uhaKy5S_bQo_Leak0KPG_tHjwYl5UH2O6LctkypX3AxNTR3F3pJ1bH_0QTF6FRmyLlRfRAS-BRG92Q', '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('899f1859-5d21-5ca8-85d0-f1773dc0c163', 'Gümüş Paket', 'Samimi aile davetleri için başlangıç paketi
Karşılama kokteyli
3 aşamalı akşam yemeği
Temel ses ve ışık sistemi', 88000, NULL, NULL, '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('7202ea14-bc02-5811-bd84-5fb01b1f90f0', 'Altın Paket', 'En kapsamlı düğün paketi
4 aşamalı şef menüsü
Limitsiz meşrubat ve sıcak içecek
DJ ve ses-ışık prodüksiyonu
Çiçekli masa süslemesi', 135000, 450, NULL, '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('4e33d2c1-7ea6-594f-99ee-58970b141524', 'Platin Paket', 'Kişiye özel prodüksiyon
6 aşamalı ziyafet menüsü
Canlı orkestra
Fotoğraf ve drone çekimi
Gelin hazırlık süiti', 216000, NULL, NULL, '08dc7a4e-afbd-51ae-83c5-7c76d8113684', now() + interval '2 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('0573a938-694d-58c2-b234-197e11582e63', 'Yeniköy Rıhtım Bahçesi', 4, 'Denize sıfır rıhtım ve manolya ağaçlı bahçe, gün batımı törenleri için düzenlendi. Davetliler tekneyle doğrudan iskeleye yanaşabilir.

Akşam yemeği rıhtım boyunca kurulan uzun masalarda servis edilir; kötü hava ihtimaline karşı bahçeye çadır kurulabilir.', 95000, 400, ARRAY['Deniz Kenarı', 'Tekne Yanaşma', 'Açık Büfe', 'Işıklı Bahçe Dekoru', 'Vale Hizmeti', 'Yağmur Çadırı']::text[], '6 saat',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, true, 'aa6416b4-0398-542d-9681-e70a2a5c05c4', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCoNwXbXNQGJGDdxsWij4YTVjkJd6omRLqi4Rka7VXRknnYRw5j2AXqQFAj_kj5DS1W9X6-1dCvGtp91Ve4y7RGayd0mbTu-hE5SoTVfxxIqC8AGmFvOjyjQjN_kOR1kGp3AwfC5kmxk9CFQ6cqOQSIRzNOgmkdcGvPZweszdCafx5C3JKCFPOIpbtNMDjN5fXZ5oMv5N0Mx-mT57iTpgvlzlmS6ts14CoFehyzCQ6Q_WipD5bQEh5ipQ', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Sarıyer'), 'Yeniköy Mah. Köybaşı Cad. No:58, Sarıyer / İstanbul', 41.121, 29.069,
  now() - interval '2 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCoNwXbXNQGJGDdxsWij4YTVjkJd6omRLqi4Rka7VXRknnYRw5j2AXqQFAj_kj5DS1W9X6-1dCvGtp91Ve4y7RGayd0mbTu-hE5SoTVfxxIqC8AGmFvOjyjQjN_kOR1kGp3AwfC5kmxk9CFQ6cqOQSIRzNOgmkdcGvPZweszdCafx5C3JKCFPOIpbtNMDjN5fXZ5oMv5N0Mx-mT57iTpgvlzlmS6ts14CoFehyzCQ6Q_WipD5bQEh5ipQ', '0573a938-694d-58c2-b234-197e11582e63', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD7avi7lC62atL0fsLH_G8gQaj7lTEbH_tQj9uQiP-GNBEw_38AluY_S7YA_wWl1Dum19ci9ADa_eN4NFESyknCwrqbBipXqyIqeZDSZixqpzF6_j3qniPqapP13WEEw5rGiuc4YNlE1U3BddMwmn5taheVcyp3KAk_oWlVJqcFELD0IMuasjyHZzS9tshqEBYkZz9UVdPS5VNewwZ7u8yY8OjmcpYy6gxfN2pIlPrP65RukPiQ3QMqQQ', '0573a938-694d-58c2-b234-197e11582e63', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '0573a938-694d-58c2-b234-197e11582e63', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDzpHa3a2bs-Y-BwsWCCEzpyq-fRMWsIpXI2wAq4-0u4wNc5CDmTAkicoHZi377IrgRUd_67E34uVm51Gg5SeBXCpVf0MQ2onUA1kBs7YI6w895Cbm4bYMZFNfGwdXIomKuCVXslaRpCyBFArNG9k89bJaxgdcU1wrt23Lo34E8uhaKy5S_bQo_Leak0KPG_tHjwYl5UH2O6LctkypX3AxNTR3F3pJ1bH_0QTF6FRmyLlRfRAS-BRG92Q', '0573a938-694d-58c2-b234-197e11582e63', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('32d452bc-9293-57fa-a0c0-e8178d4853fa', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 66000, NULL, 150, '0573a938-694d-58c2-b234-197e11582e63', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('5b6ee7c4-8415-52c5-b043-60641f19d4cf', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 95000, 380, NULL, '0573a938-694d-58c2-b234-197e11582e63', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('37dd4b7f-fb29-5330-8f39-d15ba8de8201', 'Boğaz Tekne Davetleri', 5, 'Yeniköy iskelesinden kalkan 120 kişilik yat, köprüler arasında tur atarken nikah ve kokteyl için tasarlandı.

Tur rotası ve süresi çiftin isteğine göre belirlenir; gece turlarında köprü ışıkları eşliğinde servis yapılır.', 60000, 120, ARRAY['Tekne Üstü Nikah', 'Boğaz Turu', 'DJ', 'Soğuk Büfe', 'Fotoğrafçı']::text[], '4 saat',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, true, 'aa6416b4-0398-542d-9681-e70a2a5c05c4', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCe9rPuzDS3687RqusJOdx6OWEhh2A-pVM3XCMZyynaQ2RhwsqgVJ7Ku3SHLzHI1MTYyWMblYSBwC6z5846nfYTchIOr_kbISZfT-rEUzt2tV26XlVgOWiecNW7tgJZX6H1iQyfaImX2O-u23kCe9h4Gau0FEMtsaDt37NZUpxQ-KluKtHZItZ_JI3ECmXlj8JZoQuzWj3cT74dhTqzVJDvoRWZgWDRoQUXIc_ER6MmRg-0wH0RIuv44w', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Sarıyer'), 'Tarabya Mah. Kefeliköy Cad. No:12, Sarıyer / İstanbul', 41.137, 29.056,
  now() - interval '3 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCe9rPuzDS3687RqusJOdx6OWEhh2A-pVM3XCMZyynaQ2RhwsqgVJ7Ku3SHLzHI1MTYyWMblYSBwC6z5846nfYTchIOr_kbISZfT-rEUzt2tV26XlVgOWiecNW7tgJZX6H1iQyfaImX2O-u23kCe9h4Gau0FEMtsaDt37NZUpxQ-KluKtHZItZ_JI3ECmXlj8JZoQuzWj3cT74dhTqzVJDvoRWZgWDRoQUXIc_ER6MmRg-0wH0RIuv44w', '37dd4b7f-fb29-5330-8f39-d15ba8de8201', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDzpHa3a2bs-Y-BwsWCCEzpyq-fRMWsIpXI2wAq4-0u4wNc5CDmTAkicoHZi377IrgRUd_67E34uVm51Gg5SeBXCpVf0MQ2onUA1kBs7YI6w895Cbm4bYMZFNfGwdXIomKuCVXslaRpCyBFArNG9k89bJaxgdcU1wrt23Lo34E8uhaKy5S_bQo_Leak0KPG_tHjwYl5UH2O6LctkypX3AxNTR3F3pJ1bH_0QTF6FRmyLlRfRAS-BRG92Q', '37dd4b7f-fb29-5330-8f39-d15ba8de8201', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '37dd4b7f-fb29-5330-8f39-d15ba8de8201', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('d97d9d10-a881-597a-bea9-001aea922a38', 'Boğaz Turu', '3 saatlik Boğaz turu
Karşılama kokteyli
Tekne üstü nikah töreni', 60000, NULL, 60, '37dd4b7f-fb29-5330-8f39-d15ba8de8201', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('ad38ff85-3fe2-55ee-ac3b-c94c34bd7a51', 'Tekne Düğünü', '5 saatlik özel tekne
Soğuk ve sıcak servis
DJ ve ses sistemi
Fotoğrafçı', 108000, NULL, 120, '37dd4b7f-fb29-5330-8f39-d15ba8de8201', now() + interval '1 seconds', now(), '-infinity', true);

-- Polonez Kır Bahçeleri
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('eeb0b8f4-22c9-548f-9cb7-aae5de029025', 'Polonez Kır Bahçeleri', 'seed-polonez@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', 'Polonezköy Mah. Kır Sok. No:4, Beykoz / İstanbul', '5550000000',
  'Polonez Kır Bahçeleri — örnek firma', 41.1132, 29.2094, 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Beykoz'), true, true, now(), 'https://lh3.googleusercontent.com/aida-public/AB6AXuCZ8RBNjxZeSZD4phDl-40IkHAeSHNDl1WtF1ujpf3dxQNhPpanO1UsXC5zhucT89Tnjsyeas0GRR9fAHHDpxbvRuPhRl9cFSrDb7ncnfck0cVIUoeCl6Uwt81JIy0rg8qhHdFGessjhij8dNQQFhY_io2hS7ozJg5Xirdy2GiyaFbwoEp0GbH2AIanc_BDzxD7u6sKI5_z9Df5wECheMyufRTBvcqBBM29pFIrXADEzW7o62e0lfIc-Q',
  now(), now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('3bef5f03-a0b5-5451-9332-c2a393736ac2', 'Polonez Bahçe', 1, 'Polonezköy ormanının içindeki 12 dönümlük bahçe, havuz başında kurulan masalarla kalabalık kır düğünlerine uygun.

Geniş otopark ve çocuk oyun alanı aileler için rahat bir gün sağlar.', 65000, 800, ARRAY['Havuz Başı', 'Geniş Otopark', 'Açık Büfe', 'Çocuk Oyun Alanı', 'Işıklı Bahçe Dekoru', 'DJ']::text[], 'Tam gün',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, true, 'eeb0b8f4-22c9-548f-9cb7-aae5de029025', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCZ8RBNjxZeSZD4phDl-40IkHAeSHNDl1WtF1ujpf3dxQNhPpanO1UsXC5zhucT89Tnjsyeas0GRR9fAHHDpxbvRuPhRl9cFSrDb7ncnfck0cVIUoeCl6Uwt81JIy0rg8qhHdFGessjhij8dNQQFhY_io2hS7ozJg5Xirdy2GiyaFbwoEp0GbH2AIanc_BDzxD7u6sKI5_z9Df5wECheMyufRTBvcqBBM29pFIrXADEzW7o62e0lfIc-Q', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Beykoz'), 'Polonezköy Mah. Kır Sok. No:4, Beykoz / İstanbul', 41.1132, 29.2094,
  now() - interval '4 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCZ8RBNjxZeSZD4phDl-40IkHAeSHNDl1WtF1ujpf3dxQNhPpanO1UsXC5zhucT89Tnjsyeas0GRR9fAHHDpxbvRuPhRl9cFSrDb7ncnfck0cVIUoeCl6Uwt81JIy0rg8qhHdFGessjhij8dNQQFhY_io2hS7ozJg5Xirdy2GiyaFbwoEp0GbH2AIanc_BDzxD7u6sKI5_z9Df5wECheMyufRTBvcqBBM29pFIrXADEzW7o62e0lfIc-Q', '3bef5f03-a0b5-5451-9332-c2a393736ac2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '3bef5f03-a0b5-5451-9332-c2a393736ac2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '3bef5f03-a0b5-5451-9332-c2a393736ac2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '3bef5f03-a0b5-5451-9332-c2a393736ac2', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('b9876df9-8060-59ee-ae16-e30ea99e685a', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 46000, NULL, 150, '3bef5f03-a0b5-5451-9332-c2a393736ac2', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('40081621-6230-50f7-8cca-2f0d30eafc2d', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 65000, 260, NULL, '3bef5f03-a0b5-5451-9332-c2a393736ac2', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', 'Çamlık Kır Evi', 2, 'Çam ağaçlarının arasındaki ahşap ve cam köşk, kış aylarında şömine başında samimi davetler için kullanılıyor.

Menüde yöresel ev yemekleri öne çıkar; 300 kişiye kadar kapalı alanda servis yapılabilir.', 55000, 300, ARRAY['Şömineli Salon', 'Kış Düğünü', 'Akustik Müzik', 'Ev Yemekleri', 'Otopark']::text[], '6 saat',
  false, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, 'eeb0b8f4-22c9-548f-9cb7-aae5de029025', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwWruIWfYjbB3IJsvq42oGbVQ-5HBWeVyzj7C7NuJd0wC6Hv0ECB7iSN97cAqCGn-UjYAbMpeZqYxFMS6VckBZIMBERuFamJM9yVYNdGcgm6cMVx6xwCKTAlbLK218-xFIAfi1_WH5kFGqZZefyo2zW2sKWvJAQG7ZAZnf9bE9XB4C_AO7E2uk9MyU9b0b3g2W4jgvdD1B4ks0jx2QvAQHr9xniL7iAVg_MArtzBzMSv93sm-fZn2jrA', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Beykoz'), 'Polonezköy Mah. Çamlık Yolu No:11, Beykoz / İstanbul', 41.106, 29.202,
  now() - interval '5 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDwWruIWfYjbB3IJsvq42oGbVQ-5HBWeVyzj7C7NuJd0wC6Hv0ECB7iSN97cAqCGn-UjYAbMpeZqYxFMS6VckBZIMBERuFamJM9yVYNdGcgm6cMVx6xwCKTAlbLK218-xFIAfi1_WH5kFGqZZefyo2zW2sKWvJAQG7ZAZnf9bE9XB4C_AO7E2uk9MyU9b0b3g2W4jgvdD1B4ks0jx2QvAQHr9xniL7iAVg_MArtzBzMSv93sm-fZn2jrA', 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('67bcf4ad-df05-50c8-aafc-fa869def36e2', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 38000, NULL, 150, 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('843358a4-7b83-583e-abfd-b4d40e9a85b9', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 55000, 220, NULL, 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('a0acea33-341b-5ef6-923f-d1c17d700b88', 'Göl Kenarı Kokteyl Alanı', 12, 'Gölet kenarındaki lounge alanı, nişan ve söz törenleri ile kokteyl davetleri için düzenlendi.

Bar ekibi davete özel kokteyl menüsü hazırlar.', 48000, 250, ARRAY['Kokteyl Barı', 'Göl Manzarası', 'Lounge Oturma', 'Canlı Müzik İzni']::text[], '5 saat',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, 'eeb0b8f4-22c9-548f-9cb7-aae5de029025', 'https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', 34, (SELECT "Id" FROM "Districts" WHERE "CityId" = 34 AND "DistrictName" = 'Beykoz'), 'Polonezköy Mah. Göl Yolu No:2, Beykoz / İstanbul', 41.1185, 29.219,
  now() - interval '6 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', 'a0acea33-341b-5ef6-923f-d1c17d700b88', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCZ8RBNjxZeSZD4phDl-40IkHAeSHNDl1WtF1ujpf3dxQNhPpanO1UsXC5zhucT89Tnjsyeas0GRR9fAHHDpxbvRuPhRl9cFSrDb7ncnfck0cVIUoeCl6Uwt81JIy0rg8qhHdFGessjhij8dNQQFhY_io2hS7ozJg5Xirdy2GiyaFbwoEp0GbH2AIanc_BDzxD7u6sKI5_z9Df5wECheMyufRTBvcqBBM29pFIrXADEzW7o62e0lfIc-Q', 'a0acea33-341b-5ef6-923f-d1c17d700b88', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', 'a0acea33-341b-5ef6-923f-d1c17d700b88', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('dc365b1f-9c80-531a-91ac-b53f7bca6535', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 34000, NULL, 150, 'a0acea33-341b-5ef6-923f-d1c17d700b88', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('cfa2fe98-f8ce-5b38-b8fe-9eccb96f9f35', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 48000, 190, NULL, 'a0acea33-341b-5ef6-923f-d1c17d700b88', now() + interval '1 seconds', now(), '-infinity', true);

-- Ege Davet Organizasyon
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('97ffc3c2-c94d-53c6-b587-14bb1283328a', 'Ege Davet Organizasyon', 'seed-ege@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', 'Bağlararası Mah. Zeytin Yolu No:21, Urla / İzmir', '5550000000',
  'Ege Davet Organizasyon — örnek firma', 38.3226, 26.7647, 35, (SELECT "Id" FROM "Districts" WHERE "CityId" = 35 AND "DistrictName" = 'Urla'), true, true, now(), 'https://lh3.googleusercontent.com/aida-public/AB6AXuA1fzdpwK1jdbhHZJ4RFi-eunNpUOLAmAV8iRtumW14OYuS9N7FMrFwYS1EnzR9Dg8vcSH92pb3Lg9NQe2q3tzBDvlHn52ST31OyGA1kY225KOMA2QqzHwDjOmE4aYuV39wAZXoSKgowSY2eMr-5G39qfoeOZK43lh3xh9wO1ff3YvhBXz-RTZhO297TqEIszH_Ugnjvph_lU-ThIHnC_nNcp5KmLUWzwz6seKLz2iiMjfBjTEdSo3Lzw',
  now(), now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('984debfa-45b2-5491-b861-7353cf56cdd0', 'Urla Bağ Evi', 1, 'Urla bağları arasındaki taş ev ve bahçesi, Ege mutfağıyla hazırlanan düğün menüleriyle öne çıkıyor.

Aile ve yakın davetliler için bağ evinde konaklama imkanı var.', 72000, 350, ARRAY['Bağ Manzarası', 'Şarap Tadımı', 'Ege Mutfağı', 'Konaklama', 'Otopark']::text[], 'Tam gün',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, '97ffc3c2-c94d-53c6-b587-14bb1283328a', 'https://lh3.googleusercontent.com/aida-public/AB6AXuA1fzdpwK1jdbhHZJ4RFi-eunNpUOLAmAV8iRtumW14OYuS9N7FMrFwYS1EnzR9Dg8vcSH92pb3Lg9NQe2q3tzBDvlHn52ST31OyGA1kY225KOMA2QqzHwDjOmE4aYuV39wAZXoSKgowSY2eMr-5G39qfoeOZK43lh3xh9wO1ff3YvhBXz-RTZhO297TqEIszH_Ugnjvph_lU-ThIHnC_nNcp5KmLUWzwz6seKLz2iiMjfBjTEdSo3Lzw', 35, (SELECT "Id" FROM "Districts" WHERE "CityId" = 35 AND "DistrictName" = 'Urla'), 'Kuşçular Mah. Bağ Yolu No:7, Urla / İzmir', 38.2905, 26.765,
  now() - interval '7 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuA1fzdpwK1jdbhHZJ4RFi-eunNpUOLAmAV8iRtumW14OYuS9N7FMrFwYS1EnzR9Dg8vcSH92pb3Lg9NQe2q3tzBDvlHn52ST31OyGA1kY225KOMA2QqzHwDjOmE4aYuV39wAZXoSKgowSY2eMr-5G39qfoeOZK43lh3xh9wO1ff3YvhBXz-RTZhO297TqEIszH_Ugnjvph_lU-ThIHnC_nNcp5KmLUWzwz6seKLz2iiMjfBjTEdSo3Lzw', '984debfa-45b2-5491-b861-7353cf56cdd0', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '984debfa-45b2-5491-b861-7353cf56cdd0', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '984debfa-45b2-5491-b861-7353cf56cdd0', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '984debfa-45b2-5491-b861-7353cf56cdd0', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('2aac859e-9d55-53f7-95a5-dd5504fd9b59', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 50000, NULL, 150, '984debfa-45b2-5491-b861-7353cf56cdd0', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('02968a94-b919-5484-a189-0f70804d7427', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 72000, 290, NULL, '984debfa-45b2-5491-b861-7353cf56cdd0', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('7d20e0a9-915b-5a89-a781-d02a47c5ad0f', 'Urla Taş Avlu', 12, 'Restore edilmiş taş avlu, uzun masa düzeniyle 220 kişilik akşam yemeklerine ev sahipliği yapıyor.

Şefin hazırladığı tadım menüsü mevsime göre değişir.', 58000, 220, ARRAY['Taş Avlu', 'Şef Menüsü', 'Canlı Müzik İzni', 'Kokteyl Barı']::text[], '6 saat',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, '97ffc3c2-c94d-53c6-b587-14bb1283328a', 'https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', 35, (SELECT "Id" FROM "Districts" WHERE "CityId" = 35 AND "DistrictName" = 'Urla'), 'Yeni Mah. Taş Avlu Sok. No:4, Urla / İzmir', 38.324, 26.766,
  now() - interval '8 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '7d20e0a9-915b-5a89-a781-d02a47c5ad0f', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '7d20e0a9-915b-5a89-a781-d02a47c5ad0f', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuA1fzdpwK1jdbhHZJ4RFi-eunNpUOLAmAV8iRtumW14OYuS9N7FMrFwYS1EnzR9Dg8vcSH92pb3Lg9NQe2q3tzBDvlHn52ST31OyGA1kY225KOMA2QqzHwDjOmE4aYuV39wAZXoSKgowSY2eMr-5G39qfoeOZK43lh3xh9wO1ff3YvhBXz-RTZhO297TqEIszH_Ugnjvph_lU-ThIHnC_nNcp5KmLUWzwz6seKLz2iiMjfBjTEdSo3Lzw', '7d20e0a9-915b-5a89-a781-d02a47c5ad0f', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('8e4a5850-cab5-55e4-84f0-7d6228da196c', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 41000, NULL, 150, '7d20e0a9-915b-5a89-a781-d02a47c5ad0f', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('c61a0481-3a83-53c5-9326-90f1df6db15b', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 58000, 230, NULL, '7d20e0a9-915b-5a89-a781-d02a47c5ad0f', now() + interval '1 seconds', now(), '-infinity', true);

-- Başkent Balo Salonları
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('780143a1-10d3-5531-bf64-fcaefaeef42c', 'Başkent Balo Salonları', 'seed-baskent@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', 'Atatürk Bulvarı No:180, Çankaya / Ankara', '5550000000',
  'Başkent Balo Salonları — örnek firma', 39.9087, 32.8597, 6, (SELECT "Id" FROM "Districts" WHERE "CityId" = 6 AND "DistrictName" = 'Çankaya'), true, true, now(), 'https://lh3.googleusercontent.com/aida-public/AB6AXuDWR7YyD22XptihtDIvj813a_ZbXIk7IyZlNbqbZkGmZcCRh_GCQPdhiCii4fedXqHGPUY2nsiGE7L7FvEBJWDnWkyVWEFf_151KGDITzDvrVReyQeE9iphGldfS7HkYoYUJqv_ZY9OBYrIVmZ5Z1GD58GEyFrxHx8oYFDEwBh4kToIjuaBGwrYyhhsynJGkBTrg-Z_5RC7y1ZzXmq0M6h4a5RZa5ERU94LEVgZHHeXAsKXH-2QETWMkg',
  now(), now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('6e6cf4f9-c15f-5a71-b7bc-99598243a553', 'Kristal Balo Salonu', 6, 'Şehir merkezindeki 1200 kişilik balo salonu, kristal avizeleri ve geniş sahnesiyle büyük düğün ve galalar için tasarlandı.

Kapalı otopark ve vale hizmeti her pakete dahildir.', 150000, 1200, ARRAY['Kristal Avizeler', 'Sahne ve Podyum', 'Vale Hizmeti', 'Engelli Erişimi', 'Jeneratör', 'Ses Sistemi', 'Kapalı Otopark']::text[], '8 saat',
  false, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, '780143a1-10d3-5531-bf64-fcaefaeef42c', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDWR7YyD22XptihtDIvj813a_ZbXIk7IyZlNbqbZkGmZcCRh_GCQPdhiCii4fedXqHGPUY2nsiGE7L7FvEBJWDnWkyVWEFf_151KGDITzDvrVReyQeE9iphGldfS7HkYoYUJqv_ZY9OBYrIVmZ5Z1GD58GEyFrxHx8oYFDEwBh4kToIjuaBGwrYyhhsynJGkBTrg-Z_5RC7y1ZzXmq0M6h4a5RZa5ERU94LEVgZHHeXAsKXH-2QETWMkg', 6, (SELECT "Id" FROM "Districts" WHERE "CityId" = 6 AND "DistrictName" = 'Çankaya'), 'Oran Mah. Kristal Cad. No:15, Çankaya / Ankara', 39.851, 32.834,
  now() - interval '9 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDWR7YyD22XptihtDIvj813a_ZbXIk7IyZlNbqbZkGmZcCRh_GCQPdhiCii4fedXqHGPUY2nsiGE7L7FvEBJWDnWkyVWEFf_151KGDITzDvrVReyQeE9iphGldfS7HkYoYUJqv_ZY9OBYrIVmZ5Z1GD58GEyFrxHx8oYFDEwBh4kToIjuaBGwrYyhhsynJGkBTrg-Z_5RC7y1ZzXmq0M6h4a5RZa5ERU94LEVgZHHeXAsKXH-2QETWMkg', '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw', '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('ef7c1229-a222-587a-9321-3f5f169794ed', 'Gümüş Paket', 'Samimi aile davetleri için başlangıç paketi
Karşılama kokteyli
3 aşamalı akşam yemeği
Temel ses ve ışık sistemi', 98000, NULL, NULL, '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('0f8d6f63-50dd-5680-aa33-edfe279534d7', 'Altın Paket', 'En kapsamlı düğün paketi
4 aşamalı şef menüsü
Limitsiz meşrubat ve sıcak içecek
DJ ve ses-ışık prodüksiyonu
Çiçekli masa süslemesi', 150000, 500, NULL, '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('ae30b524-f370-5bab-a903-75da4e6ad4c4', 'Platin Paket', 'Kişiye özel prodüksiyon
6 aşamalı ziyafet menüsü
Canlı orkestra
Fotoğraf ve drone çekimi
Gelin hazırlık süiti', 240000, NULL, NULL, '6e6cf4f9-c15f-5a71-b7bc-99598243a553', now() + interval '2 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('fc22f025-3cc4-5d54-acf8-7177f1b95fc9', 'Çankaya Nikah Köşkü', 8, 'Bahçe içindeki köşk, resmi nikah törenleri ve sonrasındaki kısa kokteyller için kullanılıyor.

Nikah memuru ile randevu koordinasyonu mekan ekibi tarafından yapılır.', 35000, 200, ARRAY['Nikah Memuru Koordinasyonu', 'Seremoni Dekoru', 'Fotoğraf Köşesi', 'İkram Masası']::text[], '3 saat',
  false, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, '780143a1-10d3-5531-bf64-fcaefaeef42c', 'https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', 6, (SELECT "Id" FROM "Districts" WHERE "CityId" = 6 AND "DistrictName" = 'Çankaya'), 'Çankaya Mah. Köşk Sok. No:8, Çankaya / Ankara', 39.8885, 32.861,
  now() - interval '10 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD1keeGLdGU4mrJ2bEfjAu4z2CVj9s8mI118BUaNvC8OLfrSbkWVb7GAEpA0NfXpOvoroRqQhuBAtgobD2FEk0meH46qEi7K1eV9uzHCoelMJ3ELad7_j_EMrJFnfxGBwNZpjN3i7X5DQpj0E66-YZi0GaSZlwJNoAiofzx6wjbB_3-6g5YRp2MW9XXWd1R7ZxCgYuvG1DYOEBlnZcUNi-BUvL6O64akeKyszGecyFXHN0kQ7Th0sDXPg', 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDzpHa3a2bs-Y-BwsWCCEzpyq-fRMWsIpXI2wAq4-0u4wNc5CDmTAkicoHZi377IrgRUd_67E34uVm51Gg5SeBXCpVf0MQ2onUA1kBs7YI6w895Cbm4bYMZFNfGwdXIomKuCVXslaRpCyBFArNG9k89bJaxgdcU1wrt23Lo34E8uhaKy5S_bQo_Leak0KPG_tHjwYl5UH2O6LctkypX3AxNTR3F3pJ1bH_0QTF6FRmyLlRfRAS-BRG92Q', 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDWR7YyD22XptihtDIvj813a_ZbXIk7IyZlNbqbZkGmZcCRh_GCQPdhiCii4fedXqHGPUY2nsiGE7L7FvEBJWDnWkyVWEFf_151KGDITzDvrVReyQeE9iphGldfS7HkYoYUJqv_ZY9OBYrIVmZ5Z1GD58GEyFrxHx8oYFDEwBh4kToIjuaBGwrYyhhsynJGkBTrg-Z_5RC7y1ZzXmq0M6h4a5RZa5ERU94LEVgZHHeXAsKXH-2QETWMkg', 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('94de844f-c286-5471-8945-cced8cca5186', 'Nikah Töreni', 'Resmi nikah töreni
Seremoni dekorasyonu
İkram masası', 35000, NULL, 100, 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('982f4fe1-7e91-577d-af94-780dd05db002', 'Nikah ve Kokteyl', 'Tören sonrası 2 saatlik kokteyl
Pasta ve şampanya servisi
Fotoğraf köşesi', 60000, NULL, 200, 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', 'Başkent Kongre Salonu', 10, '900 kişilik amfi düzenli salon, kurumsal toplantı, lansman ve gala yemekleri için hazırlandı.

Simültane çeviri kabinleri ve teknik ekip desteği talep üzerine sağlanır.', 90000, 900, ARRAY['Projeksiyon', 'Simültane Çeviri Kabini', 'Fuaye Alanı', 'Kablosuz İnternet', 'Teknik Ekip']::text[], 'Tam gün',
  false, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, '780143a1-10d3-5531-bf64-fcaefaeef42c', 'https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw', 6, (SELECT "Id" FROM "Districts" WHERE "CityId" = 6 AND "DistrictName" = 'Çankaya'), 'Kızılay Mah. Atatürk Blv. No:120, Çankaya / Ankara', 39.9208, 32.8541,
  now() - interval '11 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD2-fPh0Glm1ACJLS025PezswmWASHxqmLH7T5LRTbRuI_yjKpIZ8s16AD0zWvb90qtRjxOsGxUoxTnqyD6CLVzRIyuk-s4XRXAYxutdeyVmWrd-5eEbjiduVxZozyhgtxXx0z3YltPR88gdeR8wen5Hlv26j3Y5-urML9hjHQVpq86CR_tkd3vbU_4OGmLf2d0oXbrogJ8FWvWYvR5wyOC0X1j8JJCO-EGINF3PFjkqYi05L7CAmecrw', '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDWR7YyD22XptihtDIvj813a_ZbXIk7IyZlNbqbZkGmZcCRh_GCQPdhiCii4fedXqHGPUY2nsiGE7L7FvEBJWDnWkyVWEFf_151KGDITzDvrVReyQeE9iphGldfS7HkYoYUJqv_ZY9OBYrIVmZ5Z1GD58GEyFrxHx8oYFDEwBh4kToIjuaBGwrYyhhsynJGkBTrg-Z_5RC7y1ZzXmq0M6h4a5RZa5ERU94LEVgZHHeXAsKXH-2QETWMkg', '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('8d2a15ee-fb88-5c51-91df-d80d94a7d42c', 'Yarım Gün Toplantı', '4 saatlik salon kullanımı
Projeksiyon ve mikrofon
Kahve molası', 45000, NULL, 300, '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('163f2951-f2ad-5576-952d-d2820719f958', 'Tam Gün Kongre', 'Tam gün salon ve fuaye
Öğle yemeği
İki kahve molası
Teknik ekip desteği', 90000, 150, 900, '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2', now() + interval '1 seconds', now(), '-infinity', true);

-- Bodrum Sahil Davetleri
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('ea5a0fce-9dc7-5980-8bd4-6084b468ab79', 'Bodrum Sahil Davetleri', 'seed-bodrum@davetly.local', '$2a$12$r/hbYmMuw4UY56vM3zM18ecOu0gFm6UVltj6x2EPjW8yofbnWlG72', 'Neyzen Tevfik Cad. No:66, Bodrum / Muğla', '5550000000',
  'Bodrum Sahil Davetleri — örnek firma', 37.0344, 27.4305, 48, (SELECT "Id" FROM "Districts" WHERE "CityId" = 48 AND "DistrictName" = 'Bodrum'), true, true, now(), 'https://lh3.googleusercontent.com/aida-public/AB6AXuD7avi7lC62atL0fsLH_G8gQaj7lTEbH_tQj9uQiP-GNBEw_38AluY_S7YA_wWl1Dum19ci9ADa_eN4NFESyknCwrqbBipXqyIqeZDSZixqpzF6_j3qniPqapP13WEEw5rGiuc4YNlE1U3BddMwmn5taheVcyp3KAk_oWlVJqcFELD0IMuasjyHZzS9tshqEBYkZz9UVdPS5VNewwZ7u8yY8OjmcpYy6gxfN2pIlPrP65RukPiQ3QMqQQ',
  now(), now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('7b88379f-9867-5496-8482-f40483b0371d', 'Gündoğan Koy Bahçesi', 4, 'Koya bakan bahçe ve iskele, gün batımında yapılan törenler ve deniz kenarında akşam yemeği için düzenlendi.

Yakındaki butik otellerle anlaşmalı konaklama paketleri sunulur.', 110000, 400, ARRAY['Denize Sıfır', 'Gün Batımı Töreni', 'Konaklama', 'Tekne Yanaşma', 'Açık Büfe', 'DJ']::text[], 'Tam gün',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, 'ea5a0fce-9dc7-5980-8bd4-6084b468ab79', 'https://lh3.googleusercontent.com/aida-public/AB6AXuD7avi7lC62atL0fsLH_G8gQaj7lTEbH_tQj9uQiP-GNBEw_38AluY_S7YA_wWl1Dum19ci9ADa_eN4NFESyknCwrqbBipXqyIqeZDSZixqpzF6_j3qniPqapP13WEEw5rGiuc4YNlE1U3BddMwmn5taheVcyp3KAk_oWlVJqcFELD0IMuasjyHZzS9tshqEBYkZz9UVdPS5VNewwZ7u8yY8OjmcpYy6gxfN2pIlPrP65RukPiQ3QMqQQ', 48, (SELECT "Id" FROM "Districts" WHERE "CityId" = 48 AND "DistrictName" = 'Bodrum'), 'Gündoğan Mah. Koy Sok. No:3, Bodrum / Muğla', 37.1305, 27.359,
  now() - interval '12 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuD7avi7lC62atL0fsLH_G8gQaj7lTEbH_tQj9uQiP-GNBEw_38AluY_S7YA_wWl1Dum19ci9ADa_eN4NFESyknCwrqbBipXqyIqeZDSZixqpzF6_j3qniPqapP13WEEw5rGiuc4YNlE1U3BddMwmn5taheVcyp3KAk_oWlVJqcFELD0IMuasjyHZzS9tshqEBYkZz9UVdPS5VNewwZ7u8yY8OjmcpYy6gxfN2pIlPrP65RukPiQ3QMqQQ', '7b88379f-9867-5496-8482-f40483b0371d', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCoNwXbXNQGJGDdxsWij4YTVjkJd6omRLqi4Rka7VXRknnYRw5j2AXqQFAj_kj5DS1W9X6-1dCvGtp91Ve4y7RGayd0mbTu-hE5SoTVfxxIqC8AGmFvOjyjQjN_kOR1kGp3AwfC5kmxk9CFQ6cqOQSIRzNOgmkdcGvPZweszdCafx5C3JKCFPOIpbtNMDjN5fXZ5oMv5N0Mx-mT57iTpgvlzlmS6ts14CoFehyzCQ6Q_WipD5bQEh5ipQ', '7b88379f-9867-5496-8482-f40483b0371d', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuDzpHa3a2bs-Y-BwsWCCEzpyq-fRMWsIpXI2wAq4-0u4wNc5CDmTAkicoHZi377IrgRUd_67E34uVm51Gg5SeBXCpVf0MQ2onUA1kBs7YI6w895Cbm4bYMZFNfGwdXIomKuCVXslaRpCyBFArNG9k89bJaxgdcU1wrt23Lo34E8uhaKy5S_bQo_Leak0KPG_tHjwYl5UH2O6LctkypX3AxNTR3F3pJ1bH_0QTF6FRmyLlRfRAS-BRG92Q', '7b88379f-9867-5496-8482-f40483b0371d', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '7b88379f-9867-5496-8482-f40483b0371d', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('c66a7794-2659-5516-9ae7-847f1a49b36d', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 77000, NULL, 150, '7b88379f-9867-5496-8482-f40483b0371d', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('1b5f1f3a-0fd9-5881-bd97-da934ad881de', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 110000, 440, NULL, '7b88379f-9867-5496-8482-f40483b0371d', now() + interval '1 seconds', now(), '-infinity', true);
INSERT INTO "Organizations" ("Id", "Title", "CategoryId", "Description", "Price", "MaxGuestCount", "Services", "Duration",
  "IsOutdoor", "ReservationNote", "CancelPolicy", "VideoUrl", "IsFeatured", "CompanyId", "CoverPhotoPath", "CityId", "DistrictId", "Adress", "Latitude", "Longitude", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('363ff92c-6d0f-52a9-9bdc-77cebf3469d2', 'Yalıkavak Rooftop', 11, 'Marinaya bakan çatı terası, gece boyu süren kokteyl ve after-party davetleri için tasarlandı.

Teras, rüzgara karşı cam panellerle korunur.', 85000, 250, ARRAY['Marina Manzarası', 'Kokteyl Barı', 'Lounge Oturma', 'DJ', 'Işık Şovu']::text[], '6 saat',
  true, 'Hafta sonu tarihleri için en az 3 ay önceden rezervasyon önerilir. Ön görüşme ve mekan turu ücretsizdir; tadım menüsü sözleşme sonrası planlanır.', 'Etkinlikten 90 gün öncesine kadar yapılan iptallerde kaporanın tamamı iade edilir. 90-30 gün arasında %50''si iade edilir, son 30 günde iade yapılmaz. Tarih değişikliği bir kez ücretsizdir.', NULL, false, 'ea5a0fce-9dc7-5980-8bd4-6084b468ab79', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCe9rPuzDS3687RqusJOdx6OWEhh2A-pVM3XCMZyynaQ2RhwsqgVJ7Ku3SHLzHI1MTYyWMblYSBwC6z5846nfYTchIOr_kbISZfT-rEUzt2tV26XlVgOWiecNW7tgJZX6H1iQyfaImX2O-u23kCe9h4Gau0FEMtsaDt37NZUpxQ-KluKtHZItZ_JI3ECmXlj8JZoQuzWj3cT74dhTqzVJDvoRWZgWDRoQUXIc_ER6MmRg-0wH0RIuv44w', 48, (SELECT "Id" FROM "Districts" WHERE "CityId" = 48 AND "DistrictName" = 'Bodrum'), 'Yalıkavak Mah. Marina Cad. No:21, Bodrum / Muğla', 37.105, 27.293,
  now() - interval '13 minutes', now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCe9rPuzDS3687RqusJOdx6OWEhh2A-pVM3XCMZyynaQ2RhwsqgVJ7Ku3SHLzHI1MTYyWMblYSBwC6z5846nfYTchIOr_kbISZfT-rEUzt2tV26XlVgOWiecNW7tgJZX6H1iQyfaImX2O-u23kCe9h4Gau0FEMtsaDt37NZUpxQ-KluKtHZItZ_JI3ECmXlj8JZoQuzWj3cT74dhTqzVJDvoRWZgWDRoQUXIc_ER6MmRg-0wH0RIuv44w', '363ff92c-6d0f-52a9-9bdc-77cebf3469d2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuAEANkSdr_J1UZo1PziTkb6bNBP-_XoOyyZxl6uxZWPiFjwSmgotLrFNdl63QYhw200vHwSbBfDJwaZHMIFAIlHjvs3sDHcJtAvWbJ8aLhShZYd9WDHvOayZJoNfIqCff1Vbhisjg55bbGZagE91LZclwN_6vuNqfyIUv5bsUhfebNnJw-y33FvJ5w9mE-vq9iM4LBAED7IGlKWugvZBVl02IVuxHaLfBw8r7o0eJjw6wdf2w3HyGO5Pw', '363ff92c-6d0f-52a9-9bdc-77cebf3469d2', now(), now(), '-infinity', true);
INSERT INTO "OrganizationImages" ("ImageUrl", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('https://lh3.googleusercontent.com/aida-public/AB6AXuCgbaIB-4tZOnw8Kh96Tm_a4znTDghPaSwYt3LptwioxWUs7EMuCPbqWKmOgZKGbL6CL3hxFClQ-N5PLrmWp4GDjm-miukV1JG8SKIb3vUcb7XIH2X2pSSYS8Pk7CAKDxW1ENb-tHAw4RuWrTq4B0_w0auPI5XWEBdA0cyNXHFfom7ZzdEhsuty8cHegeyXkjbo0SJYFgbvOw-v80nXaV0Vn1TG6xTcTZ8FlXDgYoTjKI181sziTARpZg', '363ff92c-6d0f-52a9-9bdc-77cebf3469d2', now(), now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('8783328d-d850-5146-9a00-ea903f33368d', 'Kır Kokteyli', 'Gün batımı kokteyli için ideal
Karşılama içecekleri
Kokteyl tabakları
Akustik müzik', 59000, NULL, 150, '363ff92c-6d0f-52a9-9bdc-77cebf3469d2', now() + interval '0 seconds', now(), '-infinity', true);
INSERT INTO "Packages" ("Id", "Name", "Description", "Price", "PricePerPerson", "MaxGuests", "OrganizationId", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated") VALUES ('6569c00c-c88a-524f-860b-cc6816499369', 'Bahçe Düğünü', 'Açık havada tam gün düğün
Açık büfe akşam yemeği
Işıklı bahçe dekorasyonu
DJ ve dans pisti
Yağmur planı için çadır', 85000, 340, NULL, '363ff92c-6d0f-52a9-9bdc-77cebf3469d2', now() + interval '1 seconds', now(), '-infinity', true);

-- Uygun olduğu etkinlikler (kimlikler: Core/HappyDay.Application/Common/EventTypes.cs)
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,3,4,5,11}' WHERE "Id" = '08dc7a4e-afbd-51ae-83c5-7c76d8113684'; -- Yeniköy Yalı Balo Salonu: düğün, nişan, kına, söz, nikah, kurumsal
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,4,5,7}' WHERE "Id" = '0573a938-694d-58c2-b234-197e11582e63'; -- Yeniköy Rıhtım Bahçesi: düğün, nişan, söz, nikah, doğum günü
UPDATE "Organizations" SET "EventTypeIds" = '{2,4,6,7,10,11}' WHERE "Id" = '37dd4b7f-fb29-5330-8f39-d15ba8de8201'; -- Boğaz Tekne Davetleri: nişan, söz, bekarlığa veda, doğum günü, mezuniyet, kurumsal
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,3,4,5}' WHERE "Id" = '3bef5f03-a0b5-5451-9332-c2a393736ac2'; -- Polonez Bahçe: düğün, nişan, kına, söz, nikah
UPDATE "Organizations" SET "EventTypeIds" = '{2,3,4,7,9}' WHERE "Id" = 'cb56bccc-72eb-5a1d-9f03-e76d6a3369ee'; -- Çamlık Kır Evi: nişan, kına, söz, doğum günü, baby shower
UPDATE "Organizations" SET "EventTypeIds" = '{2,6,7,10,11}' WHERE "Id" = 'a0acea33-341b-5ef6-923f-d1c17d700b88'; -- Göl Kenarı Kokteyl Alanı: nişan, bekarlığa veda, doğum günü, mezuniyet, kurumsal
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,4,5,7,9}' WHERE "Id" = '984debfa-45b2-5491-b861-7353cf56cdd0'; -- Urla Bağ Evi: düğün, nişan, söz, nikah, doğum günü, baby shower
UPDATE "Organizations" SET "EventTypeIds" = '{2,3,4,6,7,10}' WHERE "Id" = '7d20e0a9-915b-5a89-a781-d02a47c5ad0f'; -- Urla Taş Avlu: nişan, kına, söz, bekarlığa veda, doğum günü, mezuniyet
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,3,8,11}' WHERE "Id" = '6e6cf4f9-c15f-5a71-b7bc-99598243a553'; -- Kristal Balo Salonu: düğün, nişan, kına, sünnet, kurumsal
UPDATE "Organizations" SET "EventTypeIds" = '{4,5}' WHERE "Id" = 'fc22f025-3cc4-5d54-acf8-7177f1b95fc9'; -- Çankaya Nikah Köşkü: söz, nikah
UPDATE "Organizations" SET "EventTypeIds" = '{8,10,11}' WHERE "Id" = '68ba7298-bc9b-5fb4-a977-89ad42d8d9e2'; -- Başkent Kongre Salonu: sünnet, mezuniyet, kurumsal
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,5,6}' WHERE "Id" = '7b88379f-9867-5496-8482-f40483b0371d'; -- Gündoğan Koy Bahçesi: düğün, nişan, nikah, bekarlığa veda
UPDATE "Organizations" SET "EventTypeIds" = '{2,6,7,10,11}' WHERE "Id" = '363ff92c-6d0f-52a9-9bdc-77cebf3469d2'; -- Yalıkavak Rooftop: nişan, bekarlığa veda, doğum günü, mezuniyet, kurumsal

COMMIT;

SELECT (SELECT COUNT(*) FROM "Users" WHERE "Email" LIKE 'seed-%') AS admin,
       (SELECT COUNT(*) FROM "Companies" WHERE "Email" LIKE 'seed-%') AS firma,
       (SELECT COUNT(*) FROM "Organizations" o JOIN "Companies" c ON c."Id" = o."CompanyId" WHERE c."Email" LIKE 'seed-%') AS mekan;
