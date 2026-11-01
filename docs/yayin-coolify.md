# Coolify ile yayına alma

Bu rehber Mutlu Günüm'ü (backend + frontend + PostgreSQL) tek bir sunucuda Coolify ile yayınlar. Sonunda yapı şöyle olur:

| Adres | Ne çalışır |
|---|---|
| `https://mutlugunum.com` (ve `www`) | Frontend (nginx, `happydayfinly` reposundaki `Dockerfile`) |
| `https://api.mutlugunum.com` | API (bu repodaki kök `Dockerfile`) |
| Yalnızca sunucu içi | PostgreSQL 17 (Coolify veritabanı kaynağı) |
| Kalıcı volume | Yüklenen fotoğraflar (`/app/wwwroot/uploads`) |
| `https://coolify.mutlugunum.com` | Coolify yönetim paneli |

HTTPS sertifikalarını Coolify Let's Encrypt'ten kendisi alır ve yeniler.

Coolify sürümüne göre menü adları biraz farklı olabilir; ayarların anlamı aynıdır.

## 0. Başlamadan önce

- **Alan adı Cloudflare'e bağlı olmalı.** Alan adının ad sunucuları (Atak Domain panelinde "İsim Sunucuları") Cloudflare'in verdiği iki ad olmalı (mutlugunum.com için `chan.ns.cloudflare.com` ve `kurt.ns.cloudflare.com`). Alan adı çözülmeden sertifika alınamaz.
- **Sunucu Ubuntu 24.04 LTS olmalı.** Coolify'ın otomatik kurulumu LTS olmayan Ubuntu sürümlerini desteklemez. Sunucu firmasının panelinden işletim sistemini Ubuntu 24.04 ile yeniden kurun ve yeni, güçlü bir root şifresi belirleyin.
- **En az 2 vCPU / 4 GB RAM / 50 GB disk.** Coolify'ın kendisi en az 2 çekirdek ve 2 GB ister; uygulama ve veritabanıyla birlikte 4 GB rahat yeter.
- **Kod GitHub'da olmalı.** İki repo gerekir: backend (`yasinerenkovalik/happydayback`) ve frontend (`yasinerenkovalik/happydayfinly`). Coolify kodu oradan çeker.

## 1. Coolify'ı kurun

Sunucuya bağlanın ve resmi kurulum komutunu çalıştırın:

```bash
ssh root@188.132.198.240
```

```bash
curl -fsSL https://cdn.coollabs.io/coolify/install.sh | bash
```

Kurulum bitince **hemen** `http://188.132.198.240:8000` adresini açıp yönetici hesabını oluşturun. Bu sayfayı ilk açan kişi Coolify'ın, dolayısıyla sunucunun yöneticisi olur. Sonra profil sayfasından iki adımlı doğrulamayı açın.

## 2. Cloudflare'de DNS kayıtları

Cloudflare → mutlugunum.com → DNS'te dört kayıt ekleyin:

| Tür | Ad | Değer | Proxy |
|---|---|---|---|
| A | `@` | `188.132.198.240` | DNS only (gri bulut) |
| A | `www` | `188.132.198.240` | DNS only |
| A | `api` | `188.132.198.240` | DNS only |
| A | `coolify` | `188.132.198.240` | DNS only |

Proxy'yi (turuncu bulut) şimdilik kapalı tutun; Coolify sertifikayı doğrudan alabilsin. Her şey çalıştıktan sonra açmak isterseniz Cloudflare'de SSL modunu **Full (strict)** yapın.

DNS yayıldıktan sonra Coolify → **Settings** → **Instance's Domain** alanına `https://coolify.mutlugunum.com` yazın. Panele artık HTTPS ile buradan girin.

## 3. GitHub bağlantısı

Coolify → **Sources** → **+ Add** → **GitHub App**. Adımları izleyip uygulamayı GitHub hesabınıza kurun ve iki repoya erişim verin. Böylece özel repolar çekilebilir ve `git push` sonrası otomatik yayın çalışır.

## 4. Proje ve veritabanı

1. **Projects** → **+ Add** → ad: `Mutlu Günüm` → `production` ortamını açın.
2. **+ New** → **Databases** → **PostgreSQL** (17).
3. Ayarlarda kullanıcı adı, şifre ve veritabanı adını not edin. **Make it publicly available** kapalı kalsın; veritabanına yalnızca sunucu içinden erişilir.
4. **Start**.
5. Coolify bağlantı bilgisini `postgres://kullanıcı:şifre@<iç-ad>:5432/veritabanı` biçiminde gösterir. .NET bu biçimi okumaz; parçaları şu hale getirin:

   ```
   Host=<iç-ad>;Port=5432;Database=<veritabanı>;Username=<kullanıcı>;Password=<şifre>
   ```

6. **Backups** sekmesinden günlük yedek açın. Yedeklerin sunucu dışına gitmesi için bir S3 hedefi ekleyin (Cloudflare R2, Backblaze B2 vb.); sunucu bozulursa yerel yedek de gider.

## 5. API

1. **+ New** → **Private Repository (with GitHub App)** → `happydayback`, dal: `main`.
2. **Build Pack**: `Dockerfile`, **Dockerfile Location**: `/Dockerfile` (kökteki; `Presentation/HappyDay.Api/Dockerfile` IDE içindir).
3. **Ports Exposes**: `8080`.
4. **Domains**: `https://api.mutlugunum.com`.
5. **Environment Variables** → **Developer view**: `deploy/coolify-api.env.example` dosyasının içeriğini yapıştırın ve `<...>` yerlerini doldurun.
   - `JwtSettings__Secret` ve `Security__Pepper` için kendi bilgisayarınızda `openssl rand -base64 48` ve `openssl rand -base64 32` çalıştırıp çıkanları yazın.
   - **Pepper'ı bir kez belirleyin ve hiç değiştirmeyin.** Şifreler bu değerle saklanır; değişirse kimse giriş yapamaz. Bir yere (şifre yöneticisi) kaydedin.
6. **Persistent Storage** → **+ Add** → **Volume**, hedef yol: `/app/wwwroot/uploads`. Bu olmazsa her yeni yayında yüklenen fotoğraflar kaybolur.
7. **Healthcheck**: yol `/health`, port `8080`. İmajda da aynı kontrol tanımlı.
8. **Deploy**. Loglarda şunları görmelisiniz:
   - `Bekleyen migration'lar uygulanıyor…` (iller, ilçeler ve kategoriler bununla gelir)
   - `İlk yönetici oluşturuldu: ...`
9. `https://api.mutlugunum.com/health` adresi `Healthy` döndürmeli.
10. Yönetici oluştuktan sonra `Bootstrap__AdminEmail` ve `Bootstrap__AdminPassword` değişkenlerini silip **Restart** edin.

## 6. Frontend

1. **+ New** → **Private Repository (with GitHub App)** → `happydayfinly`, dal: `main`.
2. **Build Pack**: `Dockerfile`, **Ports Exposes**: `80`.
3. **Domains**: `https://mutlugunum.com,https://www.mutlugunum.com`. Ayarlardan `www` → çıplak alan adına yönlendirmeyi seçin.
4. **Environment Variables**: `VITE_API_URL=https://api.mutlugunum.com`. **Build Variable** kutusunu işaretleyin; bu adres derleme sırasında koda ve sayfanın güvenlik politikasına (CSP) yazılır. Sonradan değişirse yeniden derlemek gerekir.
5. **Deploy**.

## 7. Son kontrol

- [ ] `https://mutlugunum.com` açılıyor, mekanlar ve harita görünüyor (ilk kurulumda mekan yoktur)
- [ ] `https://www.mutlugunum.com` çıplak alan adına yönleniyor
- [ ] `/kayit` ile firma kaydı → doğrulama e-postası geliyor → bağlantı `https://mutlugunum.com/auth/confirm-email?...` adresine gidiyor
- [ ] Doğrulamadan sonra giriş, mekan ekleme ve fotoğraf yükleme çalışıyor
- [ ] Sayfa yenilenince oturum açık kalıyor (kalmıyorsa `Auth__SiteDomain` ve `Cors__AllowedOrigins__*` ayarlarına bakın)
- [ ] `https://mutlugunum.com/admin/giris` ile yönetici girişi, firma onayı
- [ ] Şifremi unuttum e-postası geliyor

## Güncelleme

Kodu GitHub'a gönderdiğinizde Coolify ilgili kaynağı otomatik yeniden derleyip yayınlar (GitHub App kuruluysa). Elle yayın için kaynağın sayfasında **Redeploy**. Yeni migration'lar API açılırken kendiliğinden uygulanır.

## Yedekler

- **Veritabanı:** Coolify'ın zamanlanmış yedekleri (4. adım).
- **Fotoğraflar:** Coolify volume'ları otomatik yedeklemez. Sunucu firmasının haftalık snapshot'ını açın; ayrıca `/var/lib/docker/volumes/` altındaki uploads volume'unu düzenli olarak sunucu dışına kopyalayın.

## E-posta

Gmail SMTP denemeye yeter ama yayında günlük gönderim sınırı vardır ve e-postalar kişisel Gmail adresinden gider. Alan adına bağlı bir adres (`bildirim@mutlugunum.com`: Yandex, Zoho, Google Workspace) ya da Brevo gibi bir gönderim servisi kullanın; yalnızca `Smtp__*` değişkenleri değişir.

## Sorun giderme

| Belirti | Bakılacak yer |
|---|---|
| Sertifika alınamıyor | Alan adı aktif mi, DNS kayıtları gri bulut mu, sunucuda 80/443 açık mı |
| API açılmıyor, logda `ConnectionStrings:DefaultConnection tanimli degil` | Bağlantı dizesi girilmemiş ya da `postgres://` biçiminde girilmiş |
| API açılmıyor, logda `JwtSettings:Secret` / `Security:Pepper` | Bu iki değişken eksik ya da Secret 32 karakterden kısa |
| Girişten sonra sayfa yenilenince çıkış yapılmış görünüyor | `Auth__SiteDomain=mutlugunum.com`, `Cors__AllowedOrigins__0=https://mutlugunum.com` |
| Tarayıcı konsolunda CSP hatası, API'ye istek gitmiyor | Frontend'de `VITE_API_URL` **Build Variable** olarak işaretli değil; işaretleyip yeniden derleyin |
| Fotoğraflar yeni yayından sonra kayboldu | API'de `/app/wwwroot/uploads` volume'u eklenmemiş |
| E-posta gitmiyor | API logunda `fail:` satırları; Gmail için uygulama şifresi ve `Smtp__*` değişkenleri |
