-- ============================================================
--  Mutlu Günüm: YAYIN için örnek veri
--  3 kurgusal firma, 5 mekan (İstanbul, İzmir, Muğla), görseller ve paketler.
--  Site boş görünmesin diye; gerçek firmalar gelince deploy/ornek-veri-sil.sql ile kaldırın.
--
--  Coolify → PostgreSQL → Terminal'de:
--    wget -qO- https://raw.githubusercontent.com/yasinerenkovalik/happydayback/main/deploy/ornek-veri.sql | psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1
--
--  API'nin son sürümü yayında olmalı: dosya migration'larla gelen sütunları (EventTypeIds) kullanır.
--
--  Firmalar kurgusal ve giriş yapılamaz: şifre hash'i kimsenin bilmediği rastgele
--  bir şifreden üretildi, e-postalar teslim edilemeyen .invalid adresleri.
--  Tekrar çalıştırılabilir: önce kendi kayıtlarını (ornek-*@mutlugunum.invalid) siler.
--  Görseller lh3.googleusercontent.com'dan gelir (frontend CSP'si buna izin verir).
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

-- Boğaziçi Davet Evleri
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('aa6416b4-0398-542d-9681-e70a2a5c05c4', 'Boğaziçi Davet Evleri', 'ornek-bogazici@mutlugunum.invalid', '$2y$12$/B/d/OKBY48y1pMr6NJel.4EkWxSnJ75q47zCyf5GPjiV33zJs2XW', 'Köybaşı Cad. No:12, Yeniköy, Sarıyer / İstanbul', '5550000000',
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
-- Ege Davet Organizasyon
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('97ffc3c2-c94d-53c6-b587-14bb1283328a', 'Ege Davet Organizasyon', 'ornek-ege@mutlugunum.invalid', '$2y$12$/B/d/OKBY48y1pMr6NJel.4EkWxSnJ75q47zCyf5GPjiV33zJs2XW', 'Bağlararası Mah. Zeytin Yolu No:21, Urla / İzmir', '5550000000',
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

-- Bodrum Sahil Davetleri
INSERT INTO "Companies" ("Id", "Name", "Email", "PasswordHash", "Adress", "PhoneNumber", "Description", "Latitude", "Longitude",
  "CityId", "DistrictId", "IsApproved", "IsEmailConfirmed", "EmailConfirmedAt", "CoverPhotoPath", "CreateDate", "UpdateDate", "DeleteDate", "IsActivated")
VALUES ('ea5a0fce-9dc7-5980-8bd4-6084b468ab79', 'Bodrum Sahil Davetleri', 'ornek-bodrum@mutlugunum.invalid', '$2y$12$/B/d/OKBY48y1pMr6NJel.4EkWxSnJ75q47zCyf5GPjiV33zJs2XW', 'Neyzen Tevfik Cad. No:66, Bodrum / Muğla', '5550000000',
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
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,4,5,7,9}' WHERE "Id" = '984debfa-45b2-5491-b861-7353cf56cdd0'; -- Urla Bağ Evi: düğün, nişan, söz, nikah, doğum günü, baby shower
UPDATE "Organizations" SET "EventTypeIds" = '{2,3,4,6,7,10}' WHERE "Id" = '7d20e0a9-915b-5a89-a781-d02a47c5ad0f'; -- Urla Taş Avlu: nişan, kına, söz, bekarlığa veda, doğum günü, mezuniyet
UPDATE "Organizations" SET "EventTypeIds" = '{1,2,5,6}' WHERE "Id" = '7b88379f-9867-5496-8482-f40483b0371d'; -- Gündoğan Koy Bahçesi: düğün, nişan, nikah, bekarlığa veda
UPDATE "Organizations" SET "EventTypeIds" = '{2,6,7,10,11}' WHERE "Id" = '363ff92c-6d0f-52a9-9bdc-77cebf3469d2'; -- Yalıkavak Rooftop: nişan, bekarlığa veda, doğum günü, mezuniyet, kurumsal

COMMIT;

SELECT (SELECT COUNT(*) FROM "Companies" WHERE "Email" LIKE 'ornek-%@mutlugunum.invalid') AS ornek_firma,
       (SELECT COUNT(*) FROM "Organizations" o JOIN "Companies" c ON c."Id" = o."CompanyId"
         WHERE c."Email" LIKE 'ornek-%@mutlugunum.invalid') AS ornek_mekan;
