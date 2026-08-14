#!/usr/bin/env bash
# Testlərin ortaq hissəsi: sorğu göndərmək, nəticəni yoxlamaq, hesabat.
#
# Bilərəkdən sadə saxlanılıb — burada məqsəd test çərçivəsi yazmaq deyil,
# API-ni həqiqətən işlətmək. «Build təmizdir» yoxlanış sayılmır.

API="${API:-https://localhost:7277/api}"

PASS=0
FAIL=0
FAILURES=()

# Cavab burada saxlanılır ki, yoxlamalardan sonra da oxuna bilsin
BODY=""
CODE=""

# req METOD YOL [TOKEN] [GOVDE]
req() {
  local method="$1" path="$2" token="${3:-}" body="${4:-}"
  local args=(-sk -X "$method" "$API$path" -w '\n%{http_code}')

  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")

  if [ -n "$body" ]; then
    args+=(-H "Content-Type: application/json" -d "$body")
  fi

  local out
  out="$(curl "${args[@]}")"

  CODE="${out##*$'\n'}"
  BODY="${out%$'\n'*}"
}

pass() {
  PASS=$((PASS + 1))
  printf '  \033[32mOK\033[0m   %s\n' "$1"
}

fail() {
  FAIL=$((FAIL + 1))
  FAILURES+=("$1")
  printf '  \033[31mSƏHV\033[0m %s\n' "$1"
  printf '       gözlənilən: %s\n' "$2"
  printf '       alınan:     %s\n' "$3"
}

# Cavabın HTTP kodunu yoxlayır
expect_code() {
  local name="$1" want="$2"
  if [ "$CODE" = "$want" ]; then
    pass "$name"
  else
    fail "$name" "kod $want" "kod $CODE — ${BODY:0:200}"
  fi
}

# Cavabın içində mətn axtarır
expect_body() {
  local name="$1" needle="$2"
  case "$BODY" in
    *"$needle"*) pass "$name" ;;
    *) fail "$name" "cavabda «$needle»" "${BODY:0:200}" ;;
  esac
}

# Cavabda mətn OLMAMALIDIR — sızma yoxlamaları üçün
expect_not_body() {
  local name="$1" needle="$2"
  case "$BODY" in
    *"$needle"*) fail "$name" "«$needle» olmamalı" "${BODY:0:200}" ;;
    *) pass "$name" ;;
  esac
}

# JSON-dan sadə dəyər çıxarır: jq hər yerdə olmaya bilər
json() {
  local key="$1"
  printf '%s' "$BODY" | grep -o "\"$key\":[0-9]*" | head -1 | cut -d: -f2
}

json_str() {
  local key="$1"
  printf '%s' "$BODY" | sed -E "s/.*\"$key\":\"([^\"]*)\".*/\1/" | head -1
}

section() {
  printf '\n\033[1m%s\033[0m\n' "$1"
}

summary() {
  printf '\n────────────────────────────────\n'
  printf 'Uğurlu: %s   Səhv: %s\n' "$PASS" "$FAIL"

  if [ "$FAIL" -gt 0 ]; then
    printf '\nSəhvlər:\n'
    for f in "${FAILURES[@]}"; do printf '  · %s\n' "$f"; done
    return 1
  fi

  printf 'Hamısı keçdi.\n'
  return 0
}

# Girişi asanlaşdırır: uğurlu olsa token qaytarır
login() {
  req POST /Auth/Login "" "{\"email\":\"$1\",\"password\":\"$2\"}"
  printf '%s' "$BODY" | sed -E 's/.*"token":"([^"]+)".*/\1/'
}
