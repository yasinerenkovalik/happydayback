# API endpoint değişiklikleri

Endpoint adresleri REST kurallarına göre yeniden adlandırıldı. **Eski adresler kaldırıldı** (404 döner), bu yüzden backend ve frontend birlikte deploy edilmelidir.

## Genel kurallar

- Kaynak adları çoğul ve küçük harf: `api/organizations`, `api/contact-messages`.
- Okumalar `GET`, oluşturma `POST`, güncelleme `PUT`, silme `DELETE`.
- Tek bir kaydı hedefleyen uçlarda id artık **adreste**: `GET api/users/{id}`. Eskiden body, form veya query'de gönderilen `id` alanına gerek yok; gönderilirse adresteki id geçerli olur.
- Gönderilen body ve form alanları, aşağıda ayrıca belirtilenler dışında değişmedi.
- Yanıt formatı (`data`, `isSuccess`, `message`, `errors`) değişmedi.

## Davranış düzeltmeleri

Adreslerden bağımsız olarak şu yanıtlar düzeltildi; frontend bu alanlara bakıyorsa davranışı değişir:

- `DELETE api/companies/{id}` ve `DELETE api/organization-images/{id}` başarılı olunca artık `isSuccess: true` döner (eskiden başarılı silmede de `false` dönüyordu).
- `DELETE api/companies/{id}` ve `DELETE api/organizations/{id}` olmayan bir kayıtta 500 yerine `isSuccess: false` ve "bulunamadı" mesajı döner. `DELETE api/organizations/{id}` başarılı olunca artık bir mesaj da döner.
- `PUT api/organizations/{id}`: `Title`, `Description`, `CategoryId` ve `Duration` gönderilmezse mevcut değer korunur. Eskiden `CategoryId` 0'a düşüp istek 500 ile başarısız oluyordu. `ReservationNote`, `CancelPolicy` ve `VideoUrl` gönderilmezse eskisi gibi temizlenir; `Price`, `MaxGuestCount`, `IsOutdoor` ve `Services` her güncellemede gönderilmelidir.
- `GET api/users/validate-token` artık `userId` ve `role` alanlarını doldurur (eskiden hep `null` dönüyordu).
- `PUT api/companies/{id}` validasyon hatasında "Firma başarıyla güncellendi" yerine hatanın kendisini döner.
- `POST api/organizations`: `CoverPhoto` ve `Images` artık isteğe bağlı; yanıtta yeni mekanın `id`'si döner. Mekanla birlikte gönderilen galeri görselleri eskiden pasif kaydedilip hiç görünmüyordu, düzeltildi.
- `DELETE api/organizations/{id}`: firma kendi mekanını silebilir (eskiden yalnızca Admin). Başka firmanın mekanında "Bu işlem için yetkiniz yok." döner.
- `GET api/companies/{id}` yanıtına `isApproved` ve `isEmailConfirmed` eklendi.

## Güvenlik değişiklikleri

- **E-posta tekilliği**: aynı e-postayla (büyük/küçük harf farkı gözetmeden) iki aktif kullanıcı ya da iki aktif firma olamaz. Ön kontrol "Bu e-posta ile zaten bir hesap var." döner; aynı anda gelen iki kayıt isteğinde ikincisi `409 Conflict` ile aynı mesajı alır. Girişte e-posta harf büyüklüğü fark etmez.
- **T.C. kimlik no**: `POST api/users` isteğindeki ve `GET api/users`, `GET api/users/{id}` yanıtlarındaki `identityNo` artık sayı değil metin (`"10000000146"`) ve isteğe bağlı. Gönderilirse 11 haneli, kontrol haneleri doğru bir numara olmalı; değilse `400` ve `errors.IdentityNo` döner. Eski `int` alan 11 haneli numarayı tutamıyordu.
- **Hesap kilidi**: `POST api/users/login`, `POST api/companies/login` ve `POST api/auth/login` aynı hesaba art arda 5 hatalı şifrede hesabı 15 dakika kilitler. Kilitliyken doğru şifre de reddedilir, mesaj: "Çok fazla hatalı giriş denemesi yapıldı. N dakika sonra tekrar deneyin." Başarılı giriş sayacı sıfırlar; `POST api/companies/reset-password` kilidi de kaldırır. IP bazlı rate limit (dakikada 10) ayrıca devam ediyor.
- **Dosya yükleme**: dosya başına en fazla 10 MB; boş dosya ve içeriği uzantısıyla uyuşmayan dosya (ör. `.jpg` adlı HTML) `400` ve açıklayıcı bir mesajla reddedilir. Kapak değişince eski dosya diskten silinir; `DELETE api/organization-images/{id}` görsel dosyasını da siler. Yalnızca `/uploads/` altındaki dosyalar silinir, dış adresler (http) olduğu gibi kalır.
- **Spam koruması**: `POST api/contact-messages` ve `POST api/contacts` isteğe bağlı bir `Website` alanı alır. Bu alan formda kullanıcıya gösterilmez (bal küpü); dolu gelirse yanıt başarılı döner ama kayıt yapılmaz ve e-posta gönderilmez. Frontend bu alanı boş göndermelidir.
- **Güvenlik başlıkları**: tüm yanıtlara (yüklenen dosyalar dahil) `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy` ve kısıtlayıcı bir `Content-Security-Policy` eklenir (geliştirmedeki Swagger hariç). Geliştirme dışında `Strict-Transport-Security` (1 yıl) gönderilir.

## Kayıt, e-posta doğrulama ve mekan konumu

- `POST api/companies` (firma kaydı):
  - `CoverPhotoPath` artık isteğe bağlı. Eskiden gönderilmezse 500 dönüyordu.
  - Doğrulama e-postası gönderilemezse firma yine kaydedilir ve `isSuccess: true` döner; mesaj e-postanın gidemediğini söyler. Eskiden firma kaydedildiği halde 500 dönüyordu.
  - Doğrulama kuralları: `Name`, `Email` (geçerli adres), `Adress` zorunlu; telefon 10-13 rakam (boşluk ve tire serbest); `CityId` ve `DistrictId` seçili olmalı.
  - Şifre en az 8 karakter olmalı, en az bir harf ve bir rakam içermeli. Aynı kural `POST api/users` ve `POST api/companies/reset-password` için de geçerli; kurala uymayan istek `400` ve `errors` alanıyla döner.
- **E-posta doğrulaması zorunlu**: `POST api/companies/login` ve `POST api/auth/login`, şifre doğru olsa da e-postası doğrulanmamış firmaya oturum açmaz. Yanıt `isSuccess: false`, `errors: ["EMAIL_NOT_CONFIRMED"]`; bu durumda `resend-confirmation` ile bağlantı yeniden istenebilir. Şifre yanlışsa yalnızca "E-posta veya şifre hatalı." döner, doğrulama durumu açığa çıkmaz. Admin onayı girişi engellemez.
- `POST api/contact-messages`: firmaya giden bildirim e-postası artık doğrulama ve sıfırlama e-postalarıyla aynı `Smtp:*` ayarını kullanır. Eski `MailSettings:*` ayarı kaldırıldı; ayar eksikken hiçbir şey göndermediği halde "işletmeye iletildi" diyordu.
- `POST api/companies/resend-confirmation`: e-posta gönderilemezse 500 yerine `isSuccess: false` ve "E-posta şu anda gönderilemiyor" mesajı döner.
- `POST api/companies/request-password-reset`: e-posta gönderilemese de aynı genel yanıtı döner; hata loglanır.
- E-postalardaki bağlantılar `Frontend:BaseUrl` ayarından üretilir: `{BaseUrl}/auth/confirm-email?cid=&token=` ve `{BaseUrl}/auth/reset-password?cid=&token=`. Geliştirmede `http://localhost:5173`; üretimde frontend adresi bu ayara yazılmalı.
- **Mekan bazlı konum**: mekanların artık kendi `CityId`, `DistrictId`, `Adress`, `Latitude` ve `Longitude` alanları var.
  - `POST api/organizations` ve `PUT api/organizations/{id}` bu alanları isteğe bağlı olarak alır. Oluştururken gönderilmezse firmanın konumu kopyalanır, güncellerken gönderilmezse mevcut değer korunur.
  - İlçe seçilen ile ait değilse "Seçilen ilçe bu ile ait değil." döner.
  - `GET api/organizations/filter`, `{id}/with-images` ve `{id}/manage` konum bilgisini mekandan verir. Yanıtlara `adress` eklendi (detay yanıtında `districtId` de); firmanın adresi `companyAddress`'te kalır. Şehir ve ilçe filtresi de mekanın konumuna göre çalışır.
  - `GET api/organizations/by-company/{companyId}` yanıtındaki `location` artık "İlçe, İl" olarak dolu (eskiden hep `null`dı).
  - `VenueLocation` migration'ı mevcut mekanlara firmalarının konumunu kopyalar.
- **Mekan moderasyonu ve yayın kuralı**: "sitede görünür" kuralı tek yerde (`OrganizationVisibility.Published`): mekan silinmemiş, admin yayından kaldırmamış, firması aktif ve onaylı.
  - `GET api/organizations` (sayfalı liste) eskiden onaylanmamış firmaların mekanlarını da döndürüyordu; artık yalnızca yayındakileri döndürür.
  - `GET api/organizations/by-company/{companyId}` ziyaretçiye yalnızca yayındaki mekanları, firmanın kendisine ve admine hepsini döndürür. Yanıta `removedByAdminAt` ve `removalReason` eklendi; aynı alanlar `{id}/manage` yanıtında da var.
  - `POST api/contact-messages` yalnızca yayındaki mekan için kabul edilir.
- `GET api/cities` yanıtına il merkezinin `latitude` ve `longitude` değerleri eklendi.
- `GET api/contact-messages`: hangi firmanın mesajlarının döneceğine artık handler karar verir.
  - Firma yetkilisi her zaman kendi mesajlarını görür; gönderdiği `companyId` yok sayılır.
  - Admin `?companyId=` göndermezse `400` yerine `isSuccess: false` döner.
  - Firması olmayan kullanıcı `401` yerine `403` alır.
  - Mesaj yoksa `isSuccess: false` ve "bulunamadı" yerine `isSuccess: true` ve boş liste döner.
- `GET api/users/validate-token`: imzanın yanında oturumun açık olup olmadığına da bakar; çıkış yapılmış ya da şifresi değişmiş oturumun token'ı `401` alır.
- `POST api/admin/invitations`: davranış aynı. Controller token'dan admin Id'sini okuyordu ama servis bu değeri hiç kullanmıyordu; bu okuma kaldırıldı.
- `GET api/admin/dashboard`: `totalUsers` en fazla 1, `totalMessages` en fazla 500 sayılıyordu; artık gerçek toplamı verir.
- `POST api/admin/users/active`: yönetici kendi hesabını pasifleştiremez.

## Oturum ve token'lar

Tarayıcı oturumu artık httpOnly cookie'lerle taşınır; token `localStorage`'da tutulmaz.

- Giriş uçları (`POST api/companies/login`, `POST api/auth/login`, `POST api/users/login`) başarılı olunca üç cookie yazar:
  - `davetly.at`: access token, 15 dakika (`JwtSettings:ExpirationInMinutes`), httpOnly
  - `davetly.rt`: refresh token, 14 gün (`Auth:RefreshTokenDays`), httpOnly, yalnızca `/api/auth` altına gider
  - `davetly.session`: JavaScript'in okuyabildiği, sır içermeyen işaret (değeri `1`)
- Yanıt gövdesindeki `token` alanı duruyor; Swagger gibi cookie kullanmayan istemciler `Authorization: Bearer` ile devam edebilir. Başlık varsa cookie'ye bakılmaz.
- Her access token bir oturum kaydına (`AuthSessions`) bağlıdır ve her istekte o oturumun açık olup olmadığı kontrol edilir. Bu yüzden **bu sürümden önce alınmış token'lar geçersizdir**; kullanıcıların bir kez yeniden giriş yapması gerekir.
- Oturum şu durumlarda hemen kapanır (access token'ın süresi dolmasa da):
  - çıkış (`POST api/auth/logout`)
  - şifre değişikliği: diğer cihazlardaki oturumlar kapanır, değiştiren tarayıcı açık kalır
  - şifre sıfırlama: hesabın tüm oturumları kapanır
  - admin kullanıcıyı pasifleştirdiğinde ya da firma silindiğinde
  - daha önce kullanılmış bir refresh token tekrar gelirse (çalınmış olabilir)
- **CSRF**: kimliği cookie ile doğrulanan `POST`, `PUT` ve `DELETE` isteklerinde `X-Davetly-Client` başlığı zorunlu (değeri önemsiz, frontend `web` gönderir). Başlık yoksa istek giriş yapılmamış sayılır (`401`). Bearer başlığıyla gelen isteklerde bu başlık gerekmez.
- CORS artık `AllowCredentials` ile çalışır. `Cors:AllowedOrigins` içinde `*` varsa cookie'ler başka origin'den gönderilemez.
- Cookie'ler varsayılan olarak `SameSite=Strict`. Frontend ile API farklı sitelerdeyse (ör. `davetly.vercel.app` ve `api.davetly.com`) `Auth:CookieSameSite=None` verilmeli; o durumda cookie'ler yalnızca HTTPS'te gider.

## Yeni uçlar

| Uç | Yetki | Not |
|---|---|---|
| `GET api/organizations/{id}/manage` | Admin, Company | Firma panelinin düzenleme ekranı için mekan detayı ve görselleri. Firma onaylı olmasa da döner; firma yalnızca kendi mekanını açabilir |
| `GET api/admin/organizations` | Admin | Bütün mekanlar, firma ve yayın durumuyla. `?status=Published\|Removed\|Unapproved&search=&pageNumber=` |
| `POST api/admin/organizations/{id}/remove` | Admin | Body: `{ reason }` (10–500 karakter). Mekanı yayından kaldırır, silmez; firmaya sebebiyle e-posta gider |
| `POST api/admin/organizations/{id}/restore` | Admin | Yayından kaldırılan mekanı yeniden yayına alır; firmaya e-posta gider |
| `POST api/auth/refresh` | Refresh cookie | Yeni access token yazar; refresh token da değişir. Yanıt: `{ userId, role, companyId }`. `X-Davetly-Client` başlığı zorunlu. Oturum yoksa `401` ve cookie'ler silinir |
| `GET api/auth/session` | Giriş yapmış | Oturumdaki hesap: `{ userId, role, companyId }`. Frontend httpOnly token'ı okuyamadığı için kullanır |
| `POST api/auth/logout` | Herkes | Oturumu sunucuda kapatır ve cookie'leri siler. `X-Davetly-Client` başlığı zorunlu |
| `POST api/auth/change-password` | Giriş yapmış | Body: `{ currentPassword, newPassword }`. Yanlış mevcut şifre giriş kilidine sayılır. Diğer oturumları kapatır |
| `GET /sitemap.xml` | Herkes | Ana sayfa ve yayındaki bütün mekanların frontend adresleri (`Frontend:BaseUrl`). Frontend `mutlugunum.com/sitemap.xml` adresinde bunu sunar |
| `GET /robots.txt` | Herkes | API'nin kendisi aranmaz; `/uploads/` ve `/sitemap.xml` açık |
| `GET /seo/mekan/{id}` | Herkes | WhatsApp, Instagram gibi bağlantı önizlemesi yapan uygulamalar için başlık, açıklama ve kapak görselli küçük HTML. Frontend'in nginx'i bu botları mekan sayfasından buraya yönlendirir. Yayında olmayan mekan `404` |

## Proxy arkasında çalışma

API `X-Forwarded-For` ve `X-Forwarded-Proto` başlıklarını okur (Coolify'da Traefik ekler). Hız sınırı artık her ziyaretçinin kendi IP'sine göre işler; önceden bütün istekler Traefik'in adresinden geldiği için sitedeki herkes aynı sınırı paylaşıyordu (örneğin bütün site için dakikada 10 giriş). Bu başlıklara her kaynaktan güvenildiği için API'nin portu dışarı açılmamalı, yalnızca proxy üzerinden erişilmeli.

## Eski → yeni

### Kategori, şehir, ilçe

| Eski | Yeni | Not |
|---|---|---|
| `GET api/Category/CategoryGetAll` | `GET api/categories` | |
| `GET api/City/CityGetAll` | `GET api/cities` | |
| `POST api/District/GetAllDisctrictByCity` | `GET api/districts/by-city/{cityId}` | Body (`cityId`) yok |

### Kullanıcılar

| Eski | Yeni | Not |
|---|---|---|
| `POST api/User/login` | `POST api/users/login` | |
| `POST api/User/create` | `POST api/users` | |
| `GET api/User/getall` | `GET api/users` | |
| `POST api/User/getbyid` | `GET api/users/{id}` | Body (`id`) yok |
| `GET api/User/validate-token?token=` | `GET api/users/validate-token?token=` | |

### Firmalar

| Eski | Yeni | Not |
|---|---|---|
| `POST api/Company/add` | `POST api/companies` | |
| `POST api/Company/login` | `POST api/companies/login` | |
| `POST api/Company/confirm-email` | `POST api/companies/confirm-email` | |
| `POST api/Company/resend-confirmation` | `POST api/companies/resend-confirmation` | |
| `POST api/Company/request-password-reset` | `POST api/companies/request-password-reset` | |
| `POST api/Company/reset-password` | `POST api/companies/reset-password` | |
| `POST api/company/register-by-invite` | `POST api/companies/register-by-invite` | |
| `GET api/Company/CompanyGetAll` | `GET api/companies` | |
| `GET api/Company/getbyid?Id=` | `GET api/companies/{id}` | |
| `PUT api/Company/update` | `PUT api/companies/{id}` | Formdaki `Id` alanı gerekmez |
| `DELETE api/Company/delete` | `DELETE api/companies/{id}` | Form yok |

### Organizasyonlar

| Eski | Yeni | Not |
|---|---|---|
| `POST api/Organization/AddOrganization` | `POST api/organizations` | |
| `GET api/Organization/OrganizationGetAll?pageNumber=&pageSize=` | `GET api/organizations?pageNumber=&pageSize=` | |
| `GET api/Organization/OrganizationGetById?id=` | `GET api/organizations/{id}` | |
| `GET api/Organization/GetOrganizationWithImages?Id=` | `GET api/organizations/{id}/with-images` | |
| `GET api/Organization/GetOrganizationWithICompany?Id=` | `GET api/organizations/by-company/{companyId}` | |
| `GET api/Organization/Filter?...` | `GET api/organizations/filter?...` | Query parametreleri aynı |
| `POST api/Organization/GetFeatured` | `GET api/organizations/featured?categoryId=` | Body'deki `id` artık `categoryId` query parametresi |
| `PUT api/Organization/OrganizationUpdate` | `PUT api/organizations/{id}` | Formdaki `Id` alanı gerekmez |
| `DELETE api/Organization/DeleteOrganization` | `DELETE api/organizations/{id}` | Form yok |

### Organizasyon görselleri

| Eski | Yeni | Not |
|---|---|---|
| `POST api/OrganizationImages/AddOrganizationImages` | `POST api/organization-images` | Form aynı (`OrganizationId`, `OrganizationImage`) |
| `DELETE api/OrganizationImages/DeleteOrganizationImages/{id}` | `DELETE api/organization-images/{id}` | |

### Paketler

| Eski | Yeni | Not |
|---|---|---|
| `POST api/Package/create` | `POST api/packages` | |
| `POST api/Package/get-by-organization` | `GET api/packages/by-organization/{organizationId}` | Body (`id`) yok. Artık giriş gerektirmez; mekan detay sayfasında ziyaretçiye gösterilir |
| `PUT api/Package/update` | `PUT api/packages/{id}` | Body'deki `id` gerekmez |
| `DELETE api/Package/delete/{id}` | `DELETE api/packages/{id}` | |

### İletişim formu ve firma mesajları

| Eski | Yeni | Not |
|---|---|---|
| `POST api/Concat/add` | `POST api/contacts` | |
| `GET api/Concat/ContactGetAll` | `GET api/contacts` | |
| `POST api/Concat/getbyid` | `GET api/contacts/{id}` | Body (`id`) yok |
| `POST api/ContactMessage/add` | `POST api/contact-messages` | |
| `POST api/ContactMessage/CompanyContactMessage` | `GET api/contact-messages` | Form yok. Firma yetkilisi parametre göndermez; admin `?companyId=` gönderir |

### Takvim ve davetiye

| Eski | Yeni | Not |
|---|---|---|
| `POST api/CalendarEvents` | `POST api/calendar-events` | |
| `POST api/admin/invitations/create` | `POST api/admin/invitations` | |

### Kaldırılan

| Eski | Not |
|---|---|
| `GET api/Test/GetOrganizationWithICompany` | Sabit bir adrese test maili atan hata ayıklama ucuydu, silindi |

## Değişmeyenler

- `POST api/auth/login`
- `GET api/location/detect-city`
- `GET api/admin/dashboard`, `GET api/admin/companies`, `POST api/admin/companies/approval`, `GET api/admin/users`, `POST api/admin/users/active`, `GET api/admin/messages`
