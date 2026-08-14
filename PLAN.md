# Şadlıq Sarayları — iş planı

Bu sənəd 2026-08-14-də mövcud kodun ölçülməsi ilə hazırlanıb. Rəqəmlər
təxmin deyil: fayl sətirləri `wc -l`, cədvəl sətirləri bazadan, endpoint
davranışı isə real sorğu ilə yoxlanılıb.

Ardıcıllıq təsadüfi deyil. Hər mərhələ özündən sonrakının üstündə dayandığı
üçün belə düzülüb: sınanmamış kodun üstünə yeni kod qoymaq, gizli qüsurları
daha dərinə basdırmaqdır.

---

## Vəziyyət

**Möhkəm olan yerlər — toxunmağa ehtiyac yoxdur**

| Sahə | Vəziyyət |
|---|---|
| DB indeksləri | 32 `HasIndex`, isti sütunlar örtülüb |
| Səlahiyyət məntiqi | bir yerdə — `VenueAccessService` |
| Audit izi | hər yazma əməliyyatında |
| Yumşaq silmə | qlobal filtrlər + kaskad `SaveChangesAsync`-də |
| E-poçt | SMTP qurulub |

**Zəif yerlər — ölçülmüş**

| # | Problem | Rəqəm |
|---|---|---|
| 1 | JWT imza açarı git-dədir | `appsettings.json` izlənir |
| 2 | Yazdıqlarımız real dataya dəyməyib | `Disputes` 0, `OfferMessages` 0 sətir idi |
| 3 | Avtomatik test yoxdur | 0 test faylı, hər iki repo |
| 4 | SEO praktiki olaraq yoxdur | `index.html`-də təsvir, OG etiketi, səhifə başlığı yox |
| 5 | Şəkil performansı | 31 `<img>`, cəmi 2-də `lazy`, 2-də `aspect-ratio` |
| 6 | Nəhəng fayllar | 7 fayl 1000+ sətir, ən böyüyü `Home.vue` 1813 |
| 7 | Real vaxt yoxdur | söhbət 15 san, bildiriş 60 san sorğu ilə |
| 8 | Ödəniş inteqrasiyası yoxdur | yalnız `DisputeCategory.Payment` enum-u |

---

## Mərhələ 0 — Təhlükəsizlik (dərhal)

**JWT imza açarını repodan çıxar.** İndi `appsettings.json` git-də izlənir və
içindəki `SecurityKey` oradadır. Bu açarı bilən adam istənilən istifadəçi
kimi, istənilən rolla — SuperAdmin daxil — token imzalaya bilər. Nə parol,
nə giriş lazımdır.

- Açarı `appsettings.json`-dan çıxar, `dotnet user-secrets` və mühit
  dəyişəninə keçir
- Yeni, təsadüfi açar yarat — köhnəsi artıq etibarsızdır
- `appsettings.json`-u izlənməkdən çıxar, nümunə fayl qoy
- SMTP parolu hazırda boşdur, amma eyni yolla qurulsun ki, sonra ora
  yazılmasın

Bu, digər hər şeydən əvvəl gəlir, çünki qalan işlərin heç birinin dəyəri
yoxdur, əgər hər kəs özünü admin elan edə bilirsə.

---

## Mərhələ 1 — Yazdıqlarımızı işlət və qüsurları çıxar

Son həftələrdə 23 commit getdi: şikayət sistemi, iki tərəfli danışıq, admin
yazışma görünüşü, ştat səlahiyyətləri, rəyə cavab. Bunların yoxlanışı yalnız
«build təmizdir, endpoint 401 qaytarır» səviyyəsində idi.

Bu yoxlanışın nəyi buraxdığı artıq göründü. İlk dəfə real şikayət açan kimi
çıxdı ki, `Dispute.OpenedBy` əlaqəsi qurulmayıb: EF ayrıca kölgə sütun
yaratmışdı, kod bir sütuna yazır, naviqasiya boş qalan digərindən oxuyurdu.
Dəstək növbəsində heç kimin adı görünməyəcəkdi. Build təmiz idi. Düzəldildi
(`d85c193`), amma dərs qalır: **işlədilməyən kod sınanmış sayılmır.**

Sıra ilə uçdan-uca keçiriləcək axınlar:

1. Şikayət: aç → yazış → dəstək təyin et → daxili qeyd → qərarla bağla
2. İki tərəfli danışıq: təklif → əks təklif → müştəri əks təklifi → qəbul
3. Status qoruması: qadağan olunmuş keçidlər həqiqətən rədd olunurmu
4. Ləğv səbəbi: təsdiqlənmiş rezerv ləğv olunanda səbəb məcburidirmi
5. Admin yazışma görünüşü: oxumaq oxunma vəziyyətinə toxunmurmu
6. Mesajın gizlədilməsi: tərəflər görmür, iz qalır
7. Ştat səlahiyyətləri: bayraq söndürüləndə əməliyyat həqiqətən bağlanırmı
8. Sarayın rədd edilməsi → düzəliş → yenidən təqdim
9. Rəyə cavab: yaz, dəyiş, sil

**Maneə:** `Auth/Register` yalnız `Customer` və `Owner` rollarını açır.
1, 5, 6, 8 və 3-ün admin hissəsi SuperAdmin/Support tələb edir. Test hesabı
yaradıldı (`test.musteri@`, `test.sahib@`, parol `Sadliq123!`), amma admin
rolunu kənardan vermək olmur. Bu beş bənd üçün ya sənin hesabınla birlikdə
keçmək, ya da parolu almaq lazımdır.

---

## Mərhələ 2 — Qüsurların bağlanması

Mərhələ 1-də çıxanlar düzəldilir. Nə çıxacağını indidən bilmirik —
planlaşdırmaq da yanlış olardı. Amma bir şikayət axınında biri çıxdısa,
digər səkkizində də çıxacağını gözləmək məntiqlidir.

Hər düzəlişdən sonra ayrıca commit, mesajda kök səbəb yazılır.

---

## Mərhələ 3 — Gözlə görünən keyfiyyət

Burada istifadəçinin dərhal hiss etdiyi şeylər var, hər biri ucuzdur.

**Şəkil davranışı (#5).** 31 şəkildən 29-u `aspect-ratio` olmadan yüklənir —
səhifə açılanda məzmun sıçrayır. `loading="lazy"` isə demək olar yoxdur, ilk
açılışda hamısı birdən çəkilir. Hər ikisi kiçik dəyişiklikdir, təsiri isə
saray siyahısı kimi şəkilli səhifələrdə dərhal görünür.

**SEO (#4).** Bu, texniki yox, kommersiya boşluğudur. Saray axtaran adam
Google-a yazır. Hazırda `index.html`-də nə təsvir, nə OG etiketi var, hər
səhifənin başlığı eynidir — paylaşılan link boş görünür. Ən azı: səhifə üzrə
başlıq və təsvir, sarayın səhifəsi üçün OG şəkli, `sitemap.xml`.

Vue SPA olduğu üçün tam həll ya server tərəfdə render, ya da əvvəlcədən
generasiyadır — bu, ayrıca və böyük qərardır. Ona görə əvvəlcə ucuz hissəni
edirik, tam həlli mobil işindən sonraya saxlayırıq.

---

## Mərhələ 4 — Qorunma torunu

Test yoxdur (#3). Hər dəyişiklik əl ilə yoxlanır, yoxlanmayanda isə Mərhələ
1-dəki kimi qüsur bir ay gizli qalır.

Hər şeyi əhatə etmək lazım deyil və faydası da olmaz. Yalnız səhv olanda
bahalı olan yerlər:

- `ReservationStatusRules` — saf funksiyadır, sınamaq asandır, səhvi isə
  rezervin yanlış vəziyyətə düşməsi deməkdir
- `VenueAccessService` — kimin nəyi görüb dəyişə bildiyi
- `DisputeManager.CanAccessAsync` — daxili qeydin tərəflərə sızmaması

Üçü də təmiz məntiqdir, bazasız sınanır.

---

## Mərhələ 5 — Faylların bölünməsi

7 fayl 1000 sətirdən böyükdür (#6). Bu, indi ağrı vermir, amma hər yeni
dəyişikliyi bir az da yavaşladır.

Bunu **qəsdən sona qoyuram.** Böyük faylı bölmək davranışı dəyişmir, riski
isə real: sınanmamış kodu bölsək, sonra çıxan qüsurun bölünmədən, yoxsa
əvvəlki koddan gəldiyini ayırd edə bilməyəcəyik. Mərhələ 1 və 4 bitəndən
sonra bu iş həm təhlükəsiz, həm də sürətli olur.

Ardıcıllıq: `Home.vue` (1813) → `VenuesView` (1492) → `VenueDetail` (1267).

---

## Sonraya — qərar tələb edən böyük mövzular

Bunlar plana daxil deyil, çünki hər biri sənin qərarını gözləyir:

- **Beh/ödəniş (#8).** Ödəniş inteqrasiyası ümumiyyətlə yoxdur. Əvvəlki
  danışıqda bunu qəsdən kənara qoymuşduq. Qərar verilməyincə plana düşmür.
- **Real vaxt (#7).** Söhbət 15 saniyəlik sorğu ilə işləyir. İstifadəçi azdır
  deyə hələ problem deyil; SignalR-a keçmək artıq indi yox, yük artanda.
- **Mobil (iOS/Android).** Razılaşdığımız kimi web bitəndən sonra.

---

## Nə ilə başlayırıq

Mərhələ 0 → Mərhələ 1. Yəni: əvvəlcə JWT açarı repodan çıxır, sonra
yazdıqlarımızı bir-bir işlədib qüsurları çıxarırıq, hər düzəliş ayrıca
commit ilə.
