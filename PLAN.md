# Şadlıq Sarayları — rol əlaqələri üzrə iş planı

Platformada beş tərəf var: **müştəri**, **saray sahibi**, **saray
administratoru**, **dəstək** və **admin**. Kodun çətin yeri ayrı-ayrılıqda
bunların hər biri deyil — aralarındakı əlaqədir: kim kimi görür, kim kimə
yaza bilir, iş görüləndə kimin xəbəri olur və sonradan kimin etdiyi bilinir.

Bu sənəd 2026-08-14-də kodun oxunması ilə hazırlanıb. Aşağıdakı boşluqların
hamısı fayl və sətir göstərilərək yoxlanılıb, təxmin deyil.

---

## Hazırkı mənzərə

**Kanallar**

| Kanal | Kimlər | Vəziyyət |
|---|---|---|
| Təklif yazışması (`OfferMessage`) | müştəri ↔ saray | yalnız təklif üzərindən |
| Şikayət (`Dispute`) | tərəf ↔ dəstək | işləyir |
| Bildiriş (`Notification`) | platforma → istifadəçi | tək istiqamətli |
| Admin yazışma görünüşü | admin → yazışmalar | yalnız oxuma |

**Səlahiyyət**

`VenueAccessService` bütün qərarları bir yerdə verir — bu düzgün qurulub.
Sahib hər şeyi edir; administrator yalnız təyin olunduğu sarayda və yalnız
verilmiş bayraqlar çərçivəsində (`CanConfirmReservations`, `CanEditCalendar`,
`CanSendOffers`); dəstək və admin isə hər sarayda sahib sayılır.

Sonuncu cümlə problemdir və plan ondan başlayır.

---

## 1. Dəstəyin səlahiyyəti — qərar verildi, dəyişmir

`VenueAccessManager.HasPlatformWideAccessAsync` dəstəyi admin ilə eyni sayır,
ona görə dəstək işçisi hər sarayda sahib kimi davranır: sarayı dəyişə,
rezervi təsdiqləyə, təqvimi redaktə edə bilir.

Əvvəlcə bunu məhdudlaşdırmağı təklif etmişdim — dəstəyin işi vasitəçilikdir
deyə. **Təklif rədd edildi və səbəb yerindədir:** dəstək kənar tərəf deyil,
platformanın öz işçisidir və sahibkarın edə bildiyi hər şeyi edə bilməlidir.
Vasitəçi modeli burada uyğun gəlmir.

Deməli mövcud qurğu saxlanılır. Bunun əvəzini audit izi verir: dəstəyin hər
addımı `AuditActionFilter` ilə qeydə düşür və sahib öz panelində görür.

Bu bənd bağlıdır, koda toxunulmur.

---

## 2. Administrator işi görür, xəbəri isə sahibə gedir

**Boşluq.** `ReservationManager` saray tərəfinə gedən **bütün** bildirişləri
`venue.OwnerId`-ə göndərir (`NotifyOwnerAsync`, `NotifyOwnerOfCancellationAsync`
və digərləri). Administratora heç nə getmir.

Halbuki administrator elə rezervi təsdiqləmək üçün təyin olunur —
`CanConfirmReservations` bayrağını ona ötən həftə biz özümüz əlavə etdik.
Nəticə: səlahiyyəti olan adam yeni sorğudan xəbər tutmur, xəbər tutan sahib
isə işi ona həvalə edib.

**Təklif.** Bildiriş sahibə **və** həmin sarayda uyğun səlahiyyəti olan aktiv
administratorlara getsin. Səlahiyyət bayrağı süzgəc rolunu oynasın: təqvim
bildirişi `CanEditCalendar` olanlara, rezerv bildirişi
`CanConfirmReservations` olanlara. Bayraq söndürülübsə, o adam artıq həmin
axının içində deyil — nə edə bilir, nə də narahat olunur.

Sahib hər halda hamısını alır: sarayın cavabdehi odur.

---

## 3. Rezervdə «kim cavab verdi» yazılmır

**Boşluq.** `Reservation` entity-sində `CancelledByUserId` var (ötən həftə
əlavə etdik), amma **təsdiq və rədd üçün belə bir sahə yoxdur**. `SeenByUserId`
yalnız kimin baxdığını saxlayır.

Yəni sarayda üç administrator varsa, sahib rezervin kim tərəfindən
təsdiqləndiyini rezervin özündə görə bilmir. Audit jurnalında qalır, amma o,
ayrı ekrandır və hadisə ilə qaydını tapmaq lazımdır.

**Təklif.** `RespondedByUserId` sahəsi əlavə olunsun və `ApplyStatusAsync`
onu doldursun — ləğvdə etdiyimiz kimi. Sahibin panelində rezervin yanında
«Rəşad təsdiqlədi, 12:40» görünsün.

Müştəriyə bu ad göstərilmir: onun üçün qarşı tərəf saraydır, konkret işçi
deyil.

---

## 4. Birbaşa rezervin yazışma kanalı yoxdur

**Boşluq.** `OfferMessage` yalnız `OfferId`-ə bağlıdır. Yazışma sorğu →
təklif axını ilə gələn rezervlərdə açılır.

Müştəri sarayın səhifəsindən birbaşa rezerv edibsə, təklif yoxdur — deməli
yazışma da yoxdur. Rezerv təsdiqlənib, tarix tutulub, amma müştəri «zala
neçədə girə bilərik» sualını platformada verə bilmir. Söhbət telefona keçir,
mübahisə olanda isə adminin əlində heç nə qalmır — halbuki `OfferMessage`-in
öz şərhində yazılan məqsəd elə budur.

**Təklif.** Yazışma rezervə də bağlana bilsin: `ReservationId` sahəsi əlavə
olunsun, ikisindən biri dolu olsun. Mövcud təklif yazışmaları toxunulmadan
qalır.

---

## 5. Saray platformaya sual verə bilmir

**Boşluq.** Sahibin platforma ilə danışmaq üçün yeganə yolu `Dispute`-dur,
o isə şikayət kimi qurulub: kateqoriyalar «sarayın davranışı», «müştərinin
davranışı», qarşı tərəf anlayışı var.

Sahibin sualı isə çox vaxt şikayət deyil: «sarayım niyə rədd edildi»,
«administratoru necə əlavə edim», «bu müştəri şübhəlidir, baxın». Bunun üçün
kanal yoxdur — ya şikayət açır (yanlış görünür), ya da heç nə etmir.

**Təklif.** `DisputeCategory`-yə qarşı tərəfi olmayan **müraciət** növü
əlavə olunsun. Eyni iş axını, eyni dəstək növbəsi, eyni yazışma — sadəcə
qarşı tərəf yoxdur və dəstək onu şikayət kimi saymır. Yeni cədvəl və yeni
ekran lazım deyil.

---

## 6. Dəstək növbəsində «kimi gözləyirik» görünmür

**Boşluq.** `DisputeStatus` dörd haldan ibarətdir: `Open`, `InReview`,
`Resolved`, `Closed`. Bunlar işin dəstək tərəfindəki vəziyyətini göstərir.

Görünməyən isə budur: iş **kimi gözləyir**. Dəstək müştəridən sənəd
istəyibsə və üç gündür cavab yoxdursa, iş hələ də `InReview`-dur — növbədə
dəstəyin işi kimi görünür, halbuki top qarşı tərəfdədir.

**Təklif.** «Cavab gözlənilir» əlaməti əlavə olunsun: kimdən gözlənilir və
nə vaxtdan. Növbədə süzgəc olsun. Bu, statusu əvəz etmir, üstünə gəlir.

---

## 7. Adminin istifadəçi ilə birbaşa əlaqəsi yoxdur

**Boşluq.** Admin yazışmaları oxuyur (`AdminChatsController`, yalnız oxuma)
və bildiriş göndərə bilir (`NotificationCampaigns`) — amma bildiriş tək
istiqamətlidir, istifadəçi ona cavab yaza bilmir.

Yəni admin yazışmada problem görürsə, tərəflə danışmaq üçün ya şikayət
açılmasını gözləməli, ya da platformadan kənara çıxmalıdır.

**Təklif.** Admin mövcud yazışmadan birbaşa iş aça bilsin — düymə yazışma
ekranında olsun, iş həmin yazışmaya bağlansın. Yeni kanal qurmuruq: 5-ci
bənddəki müraciət növü burada da işləyir, sadəcə açan tərəf admindir.

---

## Ardıcıllıq

| Sıra | İş | Səbəb |
|---|---|---|
| ~~1~~ | ~~Dəstəyin səlahiyyətinin bölünməsi~~ | qərar: dəstək platformanın işçisidir, səlahiyyəti qalır |
| 2 | Bildirişlərin administratora yönlənməsi | səlahiyyət verdik, xəbəri vermədik — yarımçıq iş |
| 3 | `RespondedByUserId` | kiçik dəyişiklik, sahibə cavabdehlik görünüşü verir |
| 4 | Rezervə bağlı yazışma | müştərinin bu gün üzləşdiyi boşluq |
| 5 | Müraciət növü | sahibin platforma ilə əlaqəsini açır |
| 6 | «Cavab gözlənilir» | dəstək növbəsi böyüyəndə lazım olur |
| 7 | Yazışmadan iş açmaq | 5-ci bənd bitəndən sonra kiçik əlavədir |

Hər bənddən sonra ayrıca commit və push. Hər bənd real sorğu ilə işlədilir —
«build təmizdir» yoxlanış sayılmır.

---

## Bunu niyə vurğulayıram

Ötən həftə şikayət sistemini yazdıq, build təmiz idi, endpoint düzgün 401
qaytarırdı. Bu gün ilk dəfə real şikayət açan kimi çıxdı ki, `Dispute.OpenedBy`
əlaqəsi qurulmayıb: EF ayrıca kölgə sütun yaratmışdı, kod bir sütuna yazır,
naviqasiya isə həmişə boş qalan digərindən oxuyurdu. Dəstək növbəsində heç
kimin adı görünməyəcəkdi. Düzəldildi (`d85c193`).

Ona görə yuxarıdakı yeddi bəndin hər biri yazıldıqdan sonra əl ilə
işlədiləcək, nəticə isə commit mesajında göstəriləcək.

---

## Plandan kənar

- **Mobil (iOS/Android)** — razılaşdığımız kimi web bitəndən sonra
- **Real vaxt (SignalR)** — söhbət hazırda 15 saniyəlik sorğu ilə işləyir;
  istifadəçi sayı azdır, yük artanda qayıdırıq
- **Fayl bölünməsi** (`Home.vue` 1813 sətir və digər 6 fayl) — davranışı
  dəyişmir, yuxarıdakı işlər bitəndən sonra daha təhlükəsizdir
- **JWT imza açarı `appsettings.json`-dadır və git-də izlənir** — bu, rol
  əlaqələri mövzusundan kənardır, amma açarı bilən özünə admin token-i
  imzalaya bilər. Ayrıca qərar tələb edir.
