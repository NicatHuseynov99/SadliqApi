#!/usr/bin/env bash
# Uçdan-uca yoxlama: API həqiqətən işlədilir, cavablar yoxlanılır.
#
# İşlətmək:  bash tests/e2e/run.sh
# Şərt:      backend https://localhost:7277 ünvanında qalxmış olsun.
#
# Hər işləmə öz istifadəçilərini və sarayını yaradır — köhnə datadan asılı
# deyil və təkrar-təkrar işlədilə bilər.

cd "$(dirname "$0")"
. ./lib.sh

RUN="$(date +%H%M%S)"
PW="Sadliq123!"

CUSTOMER="e2e.musteri.$RUN@sadliq.az"
OWNER="e2e.sahib.$RUN@sadliq.az"
STAFF="e2e.admin.$RUN@sadliq.az"
OUTSIDER="e2e.kenar.$RUN@sadliq.az"

printf '\033[1mUçdan-uca yoxlama — %s\033[0m\n' "$RUN"

# ── Sistem ayaqdadırmı ─────────────────────────────────────────────────
req GET /Venues/GetTrust/1
if [ "$CODE" != "200" ]; then
  printf '\033[31mBackend cavab vermir (%s). Əvvəlcə onu qaldırın.\033[0m\n' "$CODE"
  exit 1
fi

# Paket onlarla sorğunu ardıcıl atır və iş rejimindəki limitə (yazma üçün
# dəqiqədə 10) dərhal dəyir. Limit qüsur deyil, düzgün işləyir — sadəcə
# yoxlama zamanı qaldırılmalıdır.
req POST /Auth/Login "" '{"email":"limit@yoxlama","password":"x"}'
if [ "$CODE" = "429" ]; then
  printf '\033[31mSürət limiti işə düşüb.\033[0m\n\n'
  printf 'Backend-i yoxlama üçün belə qaldırın:\n\n'
  printf '  RateLimiting__Auth=10000 RateLimiting__Write=10000 \\\n'
  printf '    dotnet run --project SadliqApi --launch-profile https\n\n'
  printf 'Və ya bir dəqiqə gözləyib təkrar cəhd edin.\n'
  exit 1
fi

# ── Qeydiyyat və giriş ─────────────────────────────────────────────────
section "Qeydiyyat və giriş"

req POST /Auth/Register "" "{\"firstName\":\"E2E\",\"lastName\":\"Musteri\",\"email\":\"$CUSTOMER\",\"phone\":\"0500000001\",\"password\":\"$PW\",\"role\":\"Customer\"}"
expect_code "müştəri qeydiyyatı" 200

req POST /Auth/Register "" "{\"firstName\":\"E2E\",\"lastName\":\"Sahib\",\"email\":\"$OWNER\",\"phone\":\"0500000002\",\"password\":\"$PW\",\"role\":\"Owner\"}"
expect_code "sahib qeydiyyatı" 200

req POST /Auth/Register "" "{\"firstName\":\"E2E\",\"lastName\":\"Kenar\",\"email\":\"$OUTSIDER\",\"phone\":\"0500000004\",\"password\":\"$PW\",\"role\":\"Owner\"}"
expect_code "kənar sahib qeydiyyatı" 200

req POST /Auth/Register "" "{\"firstName\":\"E2E\",\"lastName\":\"Tekrar\",\"email\":\"$CUSTOMER\",\"phone\":\"0500000001\",\"password\":\"$PW\",\"role\":\"Customer\"}"
expect_code "eyni e-poçtla təkrar qeydiyyat rədd olunur" 400

req POST /Auth/Login "" "{\"email\":\"$CUSTOMER\",\"password\":\"yanlis-parol\"}"
expect_code "yanlış parol rədd olunur" 400

TC="$(login "$CUSTOMER" "$PW")"
TO="$(login "$OWNER" "$PW")"
TX="$(login "$OUTSIDER" "$PW")"

[ -n "$TC" ] && pass "müştəri girişi" || fail "müştəri girişi" "token" "boş"
[ -n "$TO" ] && pass "sahib girişi" || fail "sahib girişi" "token" "boş"

req GET /Auth/Me "$TC"
expect_body "öz profilini oxuyur" "$CUSTOMER"

req GET /Auth/Me
expect_code "girişsiz profil bağlıdır" 401

# ── Saray, zal, menyu ──────────────────────────────────────────────────
section "Saray qurulması"

req POST /Venues/Create "$TO" "{\"name\":\"E2E Saray $RUN\",\"description\":\"Avtomatik yoxlama ucun yaradilmis saray.\",\"city\":\"Baki\",\"address\":\"Test 1\",\"phone\":\"+994120000000\",\"parkingCapacity\":40,\"hasValetParking\":false}"
expect_code "sahib saray yaradır" 200
VENUE="$(json id)"

req POST /Venues/Create "$TC" "{\"name\":\"Musteri Sarayi\",\"description\":\"Bu yaradilmamalidir.\",\"city\":\"Baki\",\"address\":\"Test 2\",\"phone\":\"+994120000000\"}"
expect_code "müştəri saray yarada bilmir" 403

req POST /Halls/Create "$TO" "{\"venueId\":$VENUE,\"name\":\"E2E Zal\",\"capacity\":200,\"description\":\"Test zali\",\"minPrice\":40}"
expect_code "sahib zal yaradır" 200

req GET "/Halls/GetByVenue/$VENUE" "$TO"
HALL="$(json id)"

req POST /Menus/Create "$TO" "{\"venueId\":$VENUE,\"name\":\"E2E Menyu\",\"pricePerPerson\":40,\"description\":\"Test menyusu\"}"
expect_code "sahib menyu yaradır" 200

req GET "/Menus/GetByVenue/$VENUE" "$TO"
MENU="$(json id)"

req POST /Halls/Create "$TX" "{\"venueId\":$VENUE,\"name\":\"Ogurluq zal\",\"capacity\":10,\"description\":\"x\",\"minPrice\":1}"
expect_code "kənar sahib özgə saraya zal əlavə edə bilmir" 400

# ── Administrator ──────────────────────────────────────────────────────
section "Administrator"

req POST /VenueStaff/Create "$TO" "{\"venueId\":$VENUE,\"firstName\":\"E2E\",\"lastName\":\"Admin\",\"email\":\"$STAFF\",\"phone\":\"0500000003\",\"password\":\"$PW\"}"
expect_code "sahib administrator yaradır" 200
STAFF_ID="$(json id)"

TA="$(login "$STAFF" "$PW")"
[ -n "$TA" ] && pass "administrator girişi" || fail "administrator girişi" "token" "boş"

req GET /AdminChats/Search "$TA"
expect_code "administrator admin panelə girə bilmir" 403

req POST /VenueStaff/Create "$TX" "{\"venueId\":$VENUE,\"firstName\":\"X\",\"lastName\":\"Y\",\"email\":\"x.$RUN@sadliq.az\",\"phone\":\"0500000009\",\"password\":\"$PW\"}"
expect_code "kənar sahib özgə saraya işçi əlavə edə bilmir" 400

# ── Rezerv ─────────────────────────────────────────────────────────────
section "Rezerv axını"

req POST /Reservations/Create "$TC" "{\"venueId\":$VENUE,\"hallId\":$HALL,\"menuId\":$MENU,\"eventDate\":\"2028-04-10\",\"guestCount\":100,\"contactPhone\":\"0500000001\",\"note\":\"E2E\"}"
expect_code "müştəri rezerv göndərir" 200
RES="$(json id)"

req POST /Reservations/Create "$TC" "{\"venueId\":$VENUE,\"hallId\":$HALL,\"menuId\":$MENU,\"eventDate\":\"2020-01-01\",\"guestCount\":100,\"contactPhone\":\"0500000001\"}"
expect_code "keçmiş tarixə rezerv rədd olunur" 400

req POST /Reservations/Create "$TC" "{\"venueId\":$VENUE,\"hallId\":$HALL,\"menuId\":$MENU,\"eventDate\":\"2028-05-10\",\"guestCount\":9999,\"contactPhone\":\"0500000001\"}"
expect_code "zalın tutumundan çox qonaq rədd olunur" 400

req PUT "/Reservations/UpdateStatus/$RES" "$TX" '{"status":1}'
expect_code "kənar sahib özgə rezervi təsdiqləyə bilmir" 400

req PUT "/Reservations/UpdateStatus/$RES" "$TO" '{"status":1}'
expect_code "sahib rezervi təsdiqləyir" 200

req GET /Reservations/GetForOwner "$TO"
expect_body "sahib cavabı kimin verdiyini görür" '"respondedBy"'

req GET /Reservations/GetMine "$TC"
expect_not_body "müştəriyə işçinin adı sızmır" '"respondedBy":{'

req PUT "/Reservations/UpdateStatus/$RES" "$TO" '{"status":2}'
expect_code "təsdiqlənmiş rezerv gizlicə rədd edilə bilmir" 400

req PUT "/Reservations/UpdateStatus/$RES" "$TO" '{"status":3}'
expect_code "təsdiqlənmiş rezerv səbəbsiz ləğv edilə bilmir" 400

# ── Rezerv yazışması ───────────────────────────────────────────────────
section "Rezerv yazışması"

req POST "/ReservationChat/SendMessage/$RES" "$TC" '{"body":"Zala nece saat evvel gire bilerik?"}'
expect_code "müştəri mesaj yazır" 200

req POST "/ReservationChat/SendMessage/$RES" "$TO" '{"body":"Tedbirden 4 saat evvel."}'
expect_code "saray cavab yazır" 200

req POST "/ReservationChat/SendMessage/$RES" "$TC" '{"body":""}'
expect_code "boş mesaj rədd olunur" 400

req GET "/ReservationChat/GetMessages/$RES" "$TC"
expect_body "lentdə göndərənin adı var" '"senderName":"E2E'

req GET "/ReservationChat/GetMessages/$RES" "$TX"
expect_code "kənar sahib yazışmanı oxuya bilmir" 400

req GET "/ReservationChat/GetMessages/$RES"
expect_code "girişsiz yazışma bağlıdır" 401

req GET "/ReservationChat/GetMessages/$RES" "$TA"
expect_code "administrator öz sarayının yazışmasını oxuyur" 200

# ── Ləğv və izi ────────────────────────────────────────────────────────
section "Ləğv"

req PUT "/Reservations/Cancel/$RES" "$TC" '{"reason":"Teqvim deyisdi."}'
expect_code "müştəri rezervi ləğv edir" 200

req POST "/ReservationChat/SendMessage/$RES" "$TC" '{"body":"Hele yazmaq olurmu?"}'
expect_code "bağlı rezervdə yazmaq olmur" 400

req GET "/ReservationChat/GetMessages/$RES" "$TC"
expect_code "bağlı rezervdə oxumaq açıq qalır" 200

req GET /Reservations/GetForOwner "$TO"
expect_body "ləğv səbəbi qeydə düşür" "Teqvim deyisdi"

# ── Sorğu və təklif ────────────────────────────────────────────────────
section "Sorğu və təklif"

req POST /VenueRequests/Create "$TC" '{"city":"Baki","guestCount":150,"isDateFlexible":true,"budgetPerPerson":50,"needsParking":true,"note":"E2E sorgusu"}'
expect_code "müştəri sorğu paylaşır" 200
REQ="$(json id)"

req POST /VenueRequests/Create "$TC" '{"city":"Baki","guestCount":100,"isDateFlexible":true}'
expect_code "ikinci açıq sorğu rədd olunur" 400

req GET /VenueRequests/GetMine "$TC"
expect_code "müştəri öz sorğularını görür" 200

req GET /VenueRequests/GetOpen "$TO"
expect_code "sahib açıq sorğuları görür" 200

# ── Rəy ────────────────────────────────────────────────────────────────
section "Rəy"

req GET "/Reviews/GetByVenue/$VENUE"
expect_code "rəylər girişsiz oxunur" 200

req POST /Reviews/Create "$TC" "{\"venueId\":$VENUE,\"rating\":5,\"comment\":\"Tedbir kecmeyib, bu red olunmalidir.\"}"
expect_code "tədbir keçirməyən rəy yaza bilmir" 400

req GET /Reviews/GetForOwner "$TO"
expect_code "sahib öz rəylərini görür" 200

req GET /Reviews/GetForOwner "$TC"
expect_code "müştəri sahibin rəy siyahısına girə bilmir" 403

# ── Şikayət ────────────────────────────────────────────────────────────
section "Şikayət"

req POST /Disputes/Create "$TC" "{\"category\":1,\"subject\":\"E2E sikayet\",\"description\":\"Bu, avtomatik yoxlama ucun acilmis sikayetdir ve uzunlugu kifayet edir.\",\"venueId\":$VENUE}"
expect_code "müştəri şikayət açır" 200
DISPUTE="$(json data)"

req POST /Disputes/Create "$TC" "{\"category\":1,\"subject\":\"Qisa\",\"description\":\"qisa\",\"venueId\":$VENUE}"
expect_code "qısa izahlı şikayət rədd olunur" 400

req GET /Disputes/GetMine "$TC"
expect_body "şikayətdə açanın adı görünür" '"openedByName":"E2E'

req GET /Disputes/Search "$TC"
expect_code "müştəri dəstək növbəsinə girə bilmir" 403

# ── Sevimlilər və bildirişlər ──────────────────────────────────────────
section "Sevimlilər və bildirişlər"

req POST "/Favorites/Toggle/$VENUE" "$TC"
expect_code "sarayı sevimlilərə əlavə edir" 200

req GET /Favorites/GetMyVenueIds "$TC"
expect_body "sevimlilər siyahısında görünür" "$VENUE"

req GET /Notifications/GetUnreadCount "$TO"
expect_code "sahib oxunmamış bildiriş sayını görür" 200

req GET /Notifications/GetMine "$TO"
expect_body "sahibə rezerv bildirişi gəlib" "rezerv"

# ── Admin sərhədləri ───────────────────────────────────────────────────
section "Admin sərhədləri"

for path in /Admin/GetUsers /AdminChats/Search /Audit/Search /Platform/GetSettings /Data/GetTables; do
  req GET "$path" "$TC"
  expect_code "müştəri $path ünvanına girə bilmir" 403
done

for path in /Admin/GetUsers /AdminChats/Search; do
  req GET "$path"
  expect_code "girişsiz $path bağlıdır" 401
done

summary
