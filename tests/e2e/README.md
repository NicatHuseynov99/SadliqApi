# Uçdan-uca yoxlama

API-ni həqiqətən işlədən yoxlama paketi. Kod oxumur — sorğu göndərir,
cavabı yoxlayır. «Build təmizdir» yoxlanış sayılmır: bu layihədə build-dən
keçən, endpoint-i düzgün 401 qaytaran, amma real işlədiləndə sınan qüsurlar
olub (məsələn `Dispute.OpenedBy` kölgə sütunu).

## İşlətmək

Backend yoxlama üçün limitləri qaldırılmış halda qaldırılır:

```
RateLimiting__Auth=10000 RateLimiting__Write=10000 \
  dotnet run --project SadliqApi --launch-profile https
```

Sonra:

```
bash tests/e2e/run.sh
```

Limit qaldırılmasa paket öz kvotasını yeyir və 429 alır — bu, qüsur deyil,
məhdudiyyətin düzgün işləməsidir. Skript bunu aşkarlayıb izah yazır.

## Nələr yoxlanılır

Hər işləmə öz istifadəçilərini və sarayını sıfırdan yaradır, ona görə köhnə
datadan asılı deyil və təkrar-təkrar işlədilə bilər.

- Qeydiyyat, giriş, təkrar e-poçt, yanlış parol
- Saray, zal, menyu yaradılması və özgə saraya müdaxilənin bağlanması
- Administrator hesabının yaradılması və səlahiyyət sərhədləri
- Rezerv: yaradılma, validasiya, təsdiq, qadağan olunmuş status keçidləri
- Rezerv yazışması: iki istiqamət, kənar şəxsin bağlanması, bağlı rezervdə
  oxumaq açıq / yazmaq bağlı
- Ləğv və ləğv səbəbinin qeydə düşməsi
- Sorğu/təklif, rəy, şikayət axınları
- Sevimlilər, bildirişlər
- Admin sərhədləri: müştəri 403, girişsiz 401

## Yoxlanmayan

**SuperAdmin və Dəstək tələb edən ~63 endpoint.** `Auth/Register` yalnız
`Customer` və `Owner` rollarını açır, administrator isə sahib tərəfindən
yaradılır. Platforma rolunu kənardan vermək mümkün deyil — bu hissə üçün
həmin hesabın parolu lazımdır.

**Ekranın özü.** Bu, API səviyyəsində yoxlamadır: düymənin görünüb-görünmədiyini,
dialoqun düzgün açıldığını tutmur.
