#!/usr/bin/env bash
# Smoke test for every endpoint of the OLS Processor API (.NET 10 migration).
# Run it after the API is up locally. Requires bash and curl only.
#
#   bash scripts/smoke-test-api.sh                # targets http://localhost:5000
#   bash scripts/smoke-test-api.sh http://localhost:8080
#   OLS_API_URL=http://localhost:5000 bash scripts/smoke-test-api.sh
#
# Every response (headers + body) is saved under smoke-results-<timestamp>/ so the
# whole folder can be attached to the test report. Exit code is 1 if any check fails.

set -u

BASE_URL="${1:-${OLS_API_URL:-http://localhost:5000}}"
BASE_URL="${BASE_URL%/}"
STAMP="$(date +%Y%m%d%H%M%S)"
OUT_DIR="smoke-results-${STAMP}"
SAMPLES_DIR="${OUT_DIR}/samples"
SUMMARY="${OUT_DIR}/summary.txt"
PASS=0
FAIL=0
INFO=0

mkdir -p "${SAMPLES_DIR}"

if ! command -v curl >/dev/null 2>&1; then
    echo "curl is required but was not found in PATH" >&2
    exit 2
fi

log() {
    printf '%s\n' "$*" | tee -a "${SUMMARY}"
}

# request <name> <method> <path> [curl args...]
# Stores status in REQ_STATUS, header file in REQ_HEADERS, body file in REQ_BODY.
request() {
    local name="$1" method="$2" path="$3"
    shift 3
    REQ_HEADERS="${OUT_DIR}/${name}.headers.txt"
    REQ_BODY="${OUT_DIR}/${name}.body.txt"
    local method_args=(--request "${method}")
    if [ "${method}" = "HEAD" ]; then
        method_args=(--head)
    fi
    REQ_STATUS="$(curl --silent --show-error --max-time 60 \
        "${method_args[@]}" \
        --output "${REQ_BODY}" \
        --dump-header "${REQ_HEADERS}" \
        --write-out '%{http_code}' \
        "$@" \
        "${BASE_URL}${path}" 2>>"${OUT_DIR}/curl-errors.txt")" || true
    if [ -z "${REQ_STATUS}" ]; then
        REQ_STATUS="000"
    fi
}

header_value() {
    grep -i "^$1:" "${REQ_HEADERS}" 2>/dev/null | head -n 1 | cut -d: -f2- | tr -d '\r' | sed 's/^ *//'
}

body_contains() {
    grep -q -i -- "$1" "${REQ_BODY}" 2>/dev/null
}

body_excerpt() {
    head -c 400 "${REQ_BODY}" 2>/dev/null | tr '\r\n' '  '
}

# check <label> <expected status> [substring that must appear in the body]
check() {
    local label="$1" expected="$2" needle="${3:-}"
    local result="PASS" detail="status ${REQ_STATUS}"
    if [ "${REQ_STATUS}" != "${expected}" ]; then
        result="FAIL"
        detail="expected ${expected}, got ${REQ_STATUS}"
    elif [ -n "${needle}" ] && ! body_contains "${needle}"; then
        result="FAIL"
        detail="status ${REQ_STATUS} but body does not contain '${needle}'"
    fi
    if [ "${result}" = "PASS" ]; then
        PASS=$((PASS + 1))
    else
        FAIL=$((FAIL + 1))
        detail="${detail} | body: $(body_excerpt)"
    fi
    log "[${result}] ${label} -> ${detail}"
}

# info <label> <text>
info() {
    INFO=$((INFO + 1))
    log "[INFO] $1 -> $2"
}

check_header() {
    local label="$1" header="$2" expected="$3"
    local value
    value="$(header_value "${header}")"
    if [ -n "${value}" ] && printf '%s' "${value}" | grep -q -- "${expected}"; then
        PASS=$((PASS + 1))
        log "[PASS] ${label} -> ${header}: ${value}"
    else
        FAIL=$((FAIL + 1))
        log "[FAIL] ${label} -> ${header} missing or unexpected (value: '${value}')"
    fi
}

write_sample_files() {
    local today
    today="$(date +%m%d%Y)"

    AUTH_FILE="${SAMPLES_DIR}/authorizations_${STAMP}.txt"
    printf '%s\n' \
        "HEADER|STONEEAGLE|AUTHORIZED|${today}|${today}|${today}" \
        "5555930000003147|01122022 12:28:20|840|X|2100-00-00|0.10|990011|SE|||||1|546893" \
        "5555930000003237|01122022 00:00:00|840|X|2100-00-00|0.10|990012|MS|BOGUSMD|BOGUSMD|6010|US|2|532086" \
        "5555930000004152|01222022 01:12:22|840|X|2100-00-00|0.10|990013|SE|||||3|528972" \
        "5555930000009999|01222022 01:12:22|840|X|2100-00-00|0.10|990014|SE|||||4|123456" \
        "TRAILER|4" \
        > "${AUTH_FILE}"

    POSTED_FILE="${SAMPLES_DIR}/posted_transactions_${STAMP}.txt"
    printf '%s\n' \
        "HEADER|STONEEAGLE|POSTED|${today}|${today}|${today}|2" \
        "5555930000003147|01122022|2200-2S-0000|0.10|+|840|990011|01122022 18:56:30|SE|||||||||1|546893" \
        "5555930000003237|01122022|2200-2S-0000|0.10|-|840|990012|01122022 18:56:30|MS|BOGUSMD|BOGUSMD|6010|US|||||2|532086" \
        "5555930000004332|01122022|2200-2S-0000|0.20|-|840|990019|01122022 18:56:30|SE||||||||||123456" \
        "TRAILER|3" \
        > "${POSTED_FILE}"

    NONFIN_FILE="${SAMPLES_DIR}/non_financial_${STAMP}.txt"
    printf '%s\n' \
        "HEADER|STONEEAGLE|NON-FINANCIAL|${today}|${today}|${today}|3" \
        "5468930000001234|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10|||||||1|546893" \
        "5468930000004321|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|-||||||||0.10|||||||2|546893" \
        "5468930000004444|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10||||||||546893" \
        "TRAILER|3" \
        > "${NONFIN_FILE}"
}

log "OLS Processor API smoke test"
log "Base URL : ${BASE_URL}"
log "Started  : $(date)"
log "Results  : ${OUT_DIR}"
log ""

write_sample_files
log "Sample input files written to ${SAMPLES_DIR} (use them to verify the worker output afterwards)"
log ""

# ---------------------------------------------------------------- reachability
request "00-reachability" GET "/api/about"
if [ "${REQ_STATUS}" = "000" ]; then
    log "[FAIL] API not reachable at ${BASE_URL} (see ${OUT_DIR}/curl-errors.txt). Is it running?"
    FAIL=$((FAIL + 1))
    log ""
    log "Summary: ${PASS} passed, ${FAIL} failed, ${INFO} informational"
    exit 1
fi

# ---------------------------------------------------------------- about
request "01-about" GET "/api/about" --header "Accept: application/json"
check "GET /api/about" 200 "buildName"
check_header "GET /api/about content type" "Content-Type" "application/json"
check_header "GET /api/about reports API version" "api-supported-versions" "1.0"
info "GET /api/about body" "$(body_excerpt)"

request "02-about-head" HEAD "/api/about"
info "HEAD /api/about (documented by HttpHeadOperationFilter)" "status ${REQ_STATUS}"

# ---------------------------------------------------------------- health
request "03-health" GET "/health"
check "GET /health (needs SQL Server reachable)" 200
info "GET /health body" "$(body_excerpt)"

# ---------------------------------------------------------------- swagger
request "04-swagger-ui" GET "/swagger/index.html"
check "GET /swagger/index.html" 200 "swagger"

request "05-swagger-json" GET "/swagger/v1.0/swagger.json"
check "GET /swagger/v1.0/swagger.json" 200 "OLS Processor API"
for path in "/api/about" "/api/files/ingest-authorizations" "/api/files/ingest-posted-transactions" "/api/files/ingest-non-financial"; do
    if body_contains "\"${path}\""; then
        PASS=$((PASS + 1)); log "[PASS] swagger.json documents ${path}"
    else
        FAIL=$((FAIL + 1)); log "[FAIL] swagger.json does not document ${path}"
    fi
done
if body_contains "\"/health\""; then
    info "swagger.json documents /health (AddHealthCheckDocument)" "yes"
else
    info "swagger.json documents /health (AddHealthCheckDocument)" "no"
fi

# ---------------------------------------------------------------- file ingestion (happy path)
request "06-ingest-authorizations" POST "/api/files/ingest-authorizations" \
    --form "file=@${AUTH_FILE};filename=$(basename "${AUTH_FILE}")"
check "POST /api/files/ingest-authorizations" 202
check_header "POST ingest-authorizations reports API version" "api-supported-versions" "1.0"

request "07-ingest-posted-transactions" POST "/api/files/ingest-posted-transactions" \
    --form "file=@${POSTED_FILE};filename=$(basename "${POSTED_FILE}")"
check "POST /api/files/ingest-posted-transactions" 202

request "08-ingest-non-financial" POST "/api/files/ingest-non-financial" \
    --form "file=@${NONFIN_FILE};filename=$(basename "${NONFIN_FILE}")"
check "POST /api/files/ingest-non-financial" 202

# ---------------------------------------------------------------- explicit API version header
request "09-about-versioned" GET "/api/about" --header "api-version: 1.0"
check "GET /api/about with header api-version: 1.0" 200 "buildName"

# ---------------------------------------------------------------- error handling
request "10-ingest-missing-file" POST "/api/files/ingest-authorizations" \
    --form "notTheFile=x"
check "POST ingest-authorizations without 'file' field (model validation)" 400
info "Missing-file response content type" "$(header_value "Content-Type")"

request "11-ingest-wrong-content-type" POST "/api/files/ingest-authorizations" \
    --header "Content-Type: application/json" --data '{"file":"x"}'
check "POST ingest-authorizations with JSON body (inferred multipart Consumes)" 415
info "Wrong-content-type response content type" "$(header_value "Content-Type")"

request "12-not-found" GET "/api/does-not-exist"
check "GET /api/does-not-exist" 404
info "404 response content type (Hellang ProblemDetails)" "$(header_value "Content-Type") | body: $(body_excerpt)"

request "13-method-not-allowed" DELETE "/api/about"
check "DELETE /api/about" 405

# ---------------------------------------------------------------- summary
log ""
log "Finished : $(date)"
log "Summary  : ${PASS} passed, ${FAIL} failed, ${INFO} informational"
log ""
log "Next manual checks (worker side):"
log "  - the three sample files must appear in the API WorkingDirectory folders"
log "  - the worker must produce *_Optum_*_op_debit.TXT files in the OutputDirectory folders"
log "  - dbo.OlsFile must contain one row per ingested file"
log "  - the files must be copied under /SRVFS/filetransfer/optum_ols/yyyy/MM/dd"
log "Attach the whole ${OUT_DIR} folder to the report."

if [ "${FAIL}" -gt 0 ]; then
    exit 1
fi
exit 0
