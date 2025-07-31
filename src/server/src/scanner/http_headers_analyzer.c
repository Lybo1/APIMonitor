#include "../../include/scanner/http_headers_analyzer.h"
#include <cjson/cJSON.h>
#include <ctype.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strings.h>
#include <curl/curl.h>
#include <regex.h>
#include <time.h>
#include <unistd.h>

#define MAX_REQUESTS 10
#define REQUEST_INTERVAL_MS 100
#define MAX_PAYLOADS 3
#define COOKIE_MAX_ATTRS 10

typedef struct {
    char* data;
    size_t size;
} MemoryStruct;

typedef struct {
    char key[64];
    char value[256];
} CookieAttribute;

typedef struct {
    char* name;
    CookieAttribute attrs[COOKIE_MAX_ATTRS];
    int attr_count;
} Cookie;

static size_t WriteCallback(void* contents, size_t size, size_t nmemb, void* userp) {
    size_t total_size = size * nmemb;
    MemoryStruct* mem = userp;

    char* ptr = realloc(mem->data, mem->size + total_size + 1);
    if (!ptr) {
        fprintf(stderr, "Failed to allocate memory for response body\n");
        return 0;
    }

    mem->data = ptr;
    memcpy(&(mem->data[mem->size]), contents, total_size);
    mem->size += total_size;
    mem->data[mem->size] = 0;

    return total_size;
}

static size_t HeaderCallback(char* buffer, size_t size, size_t nitems, void* userdata) {
    size_t total_size = nitems * size;
    cJSON* headers_json = (cJSON*)userdata;

    char* colon_pos = memchr(buffer, ':', total_size);
    if (!colon_pos) return total_size;

    size_t name_len = colon_pos - buffer;
    while (name_len > 0 && (buffer[name_len - 1] == ' ' || buffer[name_len - 1] == '\t')) name_len--;

    char* name = malloc(name_len + 1);
    if (!name) return 0;
    memcpy(name, buffer, name_len);
    name[name_len] = 0;

    char* value_start = colon_pos + 1;
    size_t value_len = total_size - (value_start - buffer);
    while (value_len > 0 && (value_start[value_len - 1] == '\r' || value_start[value_len - 1] == '\n' || value_start[value_len - 1] == ' ' || value_start[value_len - 1] == '\t')) value_len--;
    while (value_len > 0 && (*value_start == ' ' || *value_start == '\t')) { value_start++; value_len--; }

    char* value = malloc(value_len + 1);
    if (!value) { free(name); return 0; }
    memcpy(value, value_start, value_len);
    value[value_len] = 0;

    cJSON* header_obj = cJSON_CreateObject();
    cJSON_AddStringToObject(header_obj, "name", name);
    cJSON_AddStringToObject(header_obj, "value", value);
    cJSON_AddItemToArray(headers_json, header_obj);

    free(name);
    free(value);
    return total_size;
}

int http_fetch_url(const char* url, cJSON** out_headers, char** out_html) {
    if (!url || !out_headers || !out_html) return -1;

    CURL* curl = curl_easy_init();
    if (!curl) { fprintf(stderr, "Failed to init curl\n"); return -1; }

    MemoryStruct chunk = {0};
    cJSON* headers_json = cJSON_CreateArray();
    if (!headers_json) { fprintf(stderr, "Failed to allocate JSON array\n"); curl_easy_cleanup(curl); return -1; }

    curl_easy_setopt(curl, CURLOPT_URL, url);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &chunk);
    curl_easy_setopt(curl, CURLOPT_HEADERFUNCTION, HeaderCallback);
    curl_easy_setopt(curl, CURLOPT_HEADERDATA, headers_json);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 1L);
    curl_easy_setopt(curl, CURLOPT_USERAGENT, "Mozilla/5.0 (compatible; APIMonitor/1.0)");
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, 30L);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1L);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 2L);

    CURLcode res = curl_easy_perform(curl);
    if (res != CURLE_OK) {
        fprintf(stderr, "curl_easy_perform() failed: %s\n", curl_easy_strerror(res));
        cJSON_Delete(headers_json);
        free(chunk.data);
        curl_easy_cleanup(curl);
        return -1;
    }

    long http_code = 0;
    curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &http_code);
    if (http_code < 200 || http_code >= 300) {
        fprintf(stderr, "HTTP request failed with code %ld\n", http_code);
        cJSON_Delete(headers_json);
        free(chunk.data);
        curl_easy_cleanup(curl);
        return -1;
    }

    curl_easy_cleanup(curl);
    *out_headers = headers_json;
    *out_html = chunk.data;
    return 0;
}

void normalize_name(char* dst, const char* src) {
    int i = 0;
    while (src[i] && i < MAX_HEADER_NAME - 1) {
        dst[i] = tolower((unsigned char)src[i]);
        i++;
    }
    dst[i] = 0;
}

void trim_whitespace(char** str_ptr) {
    char* str = *str_ptr;
    if (!str) return;
    while (*str && isspace((unsigned char)*str)) str++;
    if (*str == 0) { *str_ptr = str; return; }
    char* end = str + strlen(str) - 1;
    while (end > str && isspace((unsigned char)*end)) end--;
    *(end + 1) = 0;
    *str_ptr = str;
}

int find_header(HeaderCollection* hc, const char* name) {
    for (int i = 0; i < hc->count; i++) {
        if (strcmp(hc->headers[i].name, name) == 0) return i;
    }
    return -1;
}

void add_header(HeaderCollection* hc, const char* name_raw, const char* value_raw) {
    if (hc->count >= MAX_HEADERS) { fprintf(stderr, "Header limit reached\n"); return; }

    char name[MAX_HEADER_NAME];
    strncpy(name, name_raw, sizeof(name) - 1);
    name[sizeof(name) - 1] = 0;
    normalize_name(name, name);

    char value[MAX_HEADER_VALUE];
    strncpy(value, value_raw, sizeof(value) - 1);
    value[sizeof(value) - 1] = 0;
    char* value_ptr = value;
    trim_whitespace(&value_ptr);

    int idx = find_header(hc, name);
    if (idx >= 0) {
        hc->headers[idx].duplicates++;
    } else {
        strncpy(hc->headers[hc->count].name, name, MAX_HEADER_NAME - 1);
        hc->headers[hc->count].name[MAX_HEADER_NAME - 1] = 0;
        strncpy(hc->headers[hc->count].value, value_ptr, MAX_HEADER_VALUE - 1);
        hc->headers[hc->count].value[MAX_HEADER_VALUE - 1] = 0;
        hc->headers[hc->count].duplicates = 0;
        hc->count++;
    }
}

void parse_raw_headers(const char* raw_headers, HeaderCollection* hc) {
    if (!raw_headers || !hc) return;
    hc->count = 0;
    char* copy = strdup(raw_headers);
    if (!copy) { fprintf(stderr, "Memory allocation failed\n"); return; }

    char* line_start = copy;
    while (*line_start) {
        char* line_end = strstr(line_start, "\r\n");
        if (!line_end) line_end = strchr(line_start, '\n');
        if (line_end) *line_end = 0;

        char* colon = strchr(line_start, ':');
        if (colon) {
            *colon = 0;
            add_header(hc, line_start, colon + 1);
        }
        if (!line_end) break;
        line_start = line_end + ((line_end[0] == '\r' && line_end[1] == '\n') ? 2 : 1);
    }
    free(copy);
}

// New function: Detect file type based on magic bytes
void detect_file_type(const char* data, size_t size, char* file_type, size_t file_type_len) {
    if (!data || size < 4) {
        strncpy(file_type, "unknown", file_type_len);
        return;
    }
    if (size >= 4 && memcmp(data, "\x89PNG", 4) == 0) {
        strncpy(file_type, "image/png", file_type_len);
    } else if (size >= 2 && memcmp(data, "\xFF\xD8", 2) == 0) {
        strncpy(file_type, "image/jpeg", file_type_len);
    } else if (size >= 4 && memcmp(data, "%PDF", 4) == 0) {
        strncpy(file_type, "application/pdf", file_type_len);
    } else if (size >= 4 && memcmp(data, "\x7FELF", 4) == 0) {
        strncpy(file_type, "application/x-executable", file_type_len);
    } else {
        strncpy(file_type, "text/html", file_type_len); // Default assumption
    }
}

void analyze_content_type(const char* value, const char* body, size_t body_size, ReportList* rl) {
    if (!value) {
        report_add(rl, SEV_WARNING, "Content-Type header missing.");
        return;
    }
    char file_type[64];
    detect_file_type(body, body_size, file_type, sizeof(file_type));
    if (strcasestr_exists(value, "charset=utf-8")) {
        report_add(rl, SEV_INFO, "Content-Type charset set to UTF-8.");
    } else {
        report_add(rl, SEV_WARNING, "Non-UTF-8 charset detected or charset missing in Content-Type.");
    }
    if (!strcasestr_exists(value, file_type)) {
        report_add(rl, SEV_WARNING, "Content-Type '%s' does not match detected file type '%s'.", value, file_type);
    } else {
        report_add(rl, SEV_INFO, "Content-Type '%s' matches detected file type.", value);
    }
    if (strcasestr_exists(file_type, "application/x-executable")) {
        report_add(rl, SEV_CRITICAL, "Executable file type detected in response body.");
    }
}

// New function: Test rate limiting by sending multiple requests
void analyze_rate_limiting(const char* url, ReportList* rl) {
    CURL* curl = curl_easy_init();
    if (!curl) { report_add(rl, SEV_WARNING, "Failed to init curl for rate limiting test."); return; }

    MemoryStruct chunk = {0};
    cJSON* headers_json = cJSON_CreateArray();
    if (!headers_json) { curl_easy_cleanup(curl); return; }

    curl_easy_setopt(curl, CURLOPT_URL, url);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &chunk);
    curl_easy_setopt(curl, CURLOPT_HEADERFUNCTION, HeaderCallback);
    curl_easy_setopt(curl, CURLOPT_HEADERDATA, headers_json);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 1L);
    curl_easy_setopt(curl, CURLOPT_USERAGENT, "Mozilla/5.0 (compatible; APIMonitor/1.0)");
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, 10L);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1L);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 2L);

    int rate_limit_detected = 0;
    for (int i = 0; i < MAX_REQUESTS; i++) {
        chunk.data = NULL;
        chunk.size = 0;
        cJSON_Delete(headers_json);
        headers_json = cJSON_CreateArray();

        CURLcode res = curl_easy_perform(curl);
        long http_code = 0;
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &http_code);

        if (res != CURLE_OK) {
            report_add(rl, SEV_WARNING, "Rate limiting test failed: %s.", curl_easy_strerror(res));
            break;
        }
        if (http_code == 429) {
            report_add(rl, SEV_INFO, "Rate limiting detected: HTTP 429 Too Many Requests.");
            rate_limit_detected = 1;
            break;
        }
        for (int j = 0; j < cJSON_GetArraySize(headers_json); j++) {
            cJSON* item = cJSON_GetArrayItem(headers_json, j);
            cJSON* name = cJSON_GetObjectItem(item, "name");
            cJSON* value = cJSON_GetObjectItem(item, "value");
            if (!cJSON_IsString(name) || !cJSON_IsString(value)) continue;
            if (strcasecmp(name->valuestring, "x-rate-limit") == 0 ||
                strcasecmp(name->valuestring, "retry-after") == 0) {
                report_add(rl, SEV_INFO, "Rate limiting header '%s: %s' detected.", name->valuestring, value->valuestring);
                rate_limit_detected = 1;
                break;
            }
        }
        free(chunk.data);
        usleep(REQUEST_INTERVAL_MS * 1000); // Controlled delay
    }
    if (!rate_limit_detected) {
        report_add(rl, SEV_WARNING, "No rate limiting detected after %d requests.", MAX_REQUESTS);
    }
    cJSON_Delete(headers_json);
    curl_easy_cleanup(curl);
}

// New function: Test for XSS and SQL injection vulnerabilities
void analyze_xss_sql_injection(const char* url, ReportList* rl) {
    const char* payloads[MAX_PAYLOADS] = {
        "<script>alert('xss')</script>", // XSS
        "1' OR '1'='1",                // SQL Injection
        "%3Cscript%3Ealert(1)%3C/script%3E" // URL-encoded XSS
    };
    const char* patterns[MAX_PAYLOADS] = {
        "<script>alert\\('xss'\\)</script>",
        "(error|exception|sql|syntax|database)",
        "<script>alert\\(1\\)</script>"
    };

    CURL* curl = curl_easy_init();
    if (!curl) { report_add(rl, SEV_WARNING, "Failed to init curl for injection test."); return; }

    MemoryStruct chunk = {0};
    cJSON* headers_json = cJSON_CreateArray();
    if (!headers_json) { curl_easy_cleanup(curl); return; }

    char* escaped_url = curl_easy_escape(curl, url, 0);
    char test_url[1024];
    regex_t regex[MAX_PAYLOADS];
    for (int i = 0; i < MAX_PAYLOADS; i++) {
        if (regcomp(&regex[i], patterns[i], REG_ICASE | REG_EXTENDED) != 0) {
            report_add(rl, SEV_WARNING, "Failed to compile regex for payload %d.", i);
            continue;
        }
    }

    for (int i = 0; i < MAX_PAYLOADS; i++) {
        snprintf(test_url, sizeof(test_url), "%s?test=%s", escaped_url, curl_easy_escape(curl, payloads[i], 0));
        chunk.data = NULL;
        chunk.size = 0;
        cJSON_Delete(headers_json);
        headers_json = cJSON_CreateArray();

        curl_easy_setopt(curl, CURLOPT_URL, test_url);
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &chunk);
        curl_easy_setopt(curl, CURLOPT_HEADERFUNCTION, HeaderCallback);
        curl_easy_setopt(curl, CURLOPT_HEADERDATA, headers_json);
        curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 1L);
        curl_easy_setopt(curl, CURLOPT_USERAGENT, "Mozilla/5.0 (compatible; APIMonitor/1.0)");
        curl_easy_setopt(curl, CURLOPT_TIMEOUT, 10L);
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1L);
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 2L);

        CURLcode res = curl_easy_perform(curl);
        if (res != CURLE_OK) {
            report_add(rl, SEV_WARNING, "Injection test failed for payload %d: %s.", i, curl_easy_strerror(res));
            continue;
        }
        if (chunk.data && regexec(&regex[i], chunk.data, 0, NULL, 0) == 0) {
            report_add(rl, SEV_CRITICAL, "Potential %s vulnerability detected with payload: %s.",
                       i == 1 ? "SQL Injection" : "XSS", payloads[i]);
        }
        free(chunk.data);
    }
    for (int i = 0; i < MAX_PAYLOADS; i++) regfree(&regex[i]);
    curl_free(escaped_url);
    cJSON_Delete(headers_json);
    curl_easy_cleanup(curl);
}

// New function: Parse and analyze Set-Cookie headers
void analyze_cookies(const HeaderCollection* hc, ReportList* rl) {
    Cookie cookies[10];
    int cookie_count = 0;

    for (int i = 0; i < hc->count; i++) {
        if (strcasecmp(hc->headers[i].name, "set-cookie") != 0) continue;
        if (cookie_count >= 10) break;

        cookies[cookie_count].name = NULL;
        cookies[cookie_count].attr_count = 0;
        char* value = strdup(hc->headers[i].value);
        if (!value) continue;

        char* token = strtok(value, ";");
        if (token) {
            char* eq = strchr(token, '=');
            if (eq) {
                *eq = 0;
                cookies[cookie_count].name = strdup(token);
                trim_whitespace(&cookies[cookie_count].name);
            }
        }
        while ((token = strtok(NULL, ";"))) {
            trim_whitespace(&token);
            char* eq = strchr(token, '=');
            if (cookies[cookie_count].attr_count >= COOKIE_MAX_ATTRS) break;
            if (eq) {
                *eq = 0;
                strncpy(cookies[cookie_count].attrs[cookies[cookie_count].attr_count].key, token, sizeof(cookies[cookie_count].attrs[0].key) - 1);
                strncpy(cookies[cookie_count].attrs[cookies[cookie_count].attr_count].value, eq + 1, sizeof(cookies[cookie_count].attrs[0].value) - 1);
            } else {
                strncpy(cookies[cookie_count].attrs[cookies[cookie_count].attr_count].key, token, sizeof(cookies[cookie_count].attrs[0].key) - 1);
                cookies[cookie_count].attrs[cookies[cookie_count].attr_count].value[0] = 0;
            }
            cookies[cookie_count].attr_count++;
        }
        cookie_count++;
        free(value);
    }

    for (int i = 0; i < cookie_count; i++) {
        int secure = 0, httponly = 0, samesite_strict = 0;
        for (int j = 0; j < cookies[i].attr_count; j++) {
            if (strcasecmp(cookies[i].attrs[j].key, "Secure") == 0) secure = 1;
            if (strcasecmp(cookies[i].attrs[j].key, "HttpOnly") == 0) httponly = 1;
            if (strcasecmp(cookies[i].attrs[j].key, "SameSite") == 0 &&
                strcasecmp(cookies[i].attrs[j].value, "Strict") == 0) samesite_strict = 1;
        }
        if (!secure) {
            report_add(rl, SEV_WARNING, "Cookie '%s' missing Secure attribute.", cookies[i].name ? cookies[i].name : "unknown");
        }
        if (!httponly) {
            report_add(rl, SEV_WARNING, "Cookie '%s' missing HttpOnly attribute.", cookies[i].name ? cookies[i].name : "unknown");
        }
        if (!samesite_strict) {
            report_add(rl, SEV_WARNING, "Cookie '%s' missing SameSite=Strict attribute.", cookies[i].name ? cookies[i].name : "unknown");
        }
        if (secure && httponly && samesite_strict) {
            report_add(rl, SEV_INFO, "Cookie '%s' has secure attributes (Secure, HttpOnly, SameSite=Strict).", cookies[i].name ? cookies[i].name : "unknown");
        }
    }
    for (int i = 0; i < cookie_count; i++) {
        if (cookies[i].name) free(cookies[i].name);
    }
}

grading_result grading_analyze(cJSON* headers_json, const char* url, char* body, size_t body_size) {
    grading_result res = { .score = 100, .missing = cJSON_CreateArray(), .notes = cJSON_CreateArray() };
    HeaderCollection hc = {0};
    int headers_count = cJSON_GetArraySize(headers_json);

    for (int i = 0; i < headers_count; i++) {
        cJSON* item = cJSON_GetArrayItem(headers_json, i);
        cJSON* name_json = cJSON_GetObjectItem(item, "name");
        cJSON* value_json = cJSON_GetObjectItem(item, "value");
        if (!cJSON_IsString(name_json) || !cJSON_IsString(value_json)) continue;
        add_header(&hc, name_json->valuestring, value_json->valuestring);
    }

    ReportList rl;
    report_init(&rl);

    analyze_security_headers_presence(&hc, &rl);
    for (int i = 0; i < hc.count; i++) {
        const HttpHeader* hdr = &hc.headers[i];
        if (strcasecmp(hdr->name, "strict-transport-security") == 0) {
            analyze_hsts(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "x-frame-options") == 0) {
            analyze_x_frame_options(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "content-security-policy") == 0) {
            analyze_csp(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "x-content-type-options") == 0) {
            analyze_x_content_type_options(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "referrer-policy") == 0) {
            analyze_referrer_policy(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "permissions-policy") == 0 || strcasecmp(hdr->name, "feature-policy") == 0) {
            analyze_feature_policy(hdr->value, &rl);
        } else if (strcasecmp(hdr->name, "cache-control") == 0) {
            const char* pragma_val = NULL, *expires_val = NULL;
            int p_idx = find_header(&hc, "pragma");
            if (p_idx >= 0) pragma_val = hc.headers[p_idx].value;
            int e_idx = find_header(&hc, "expires");
            if (e_idx >= 0) expires_val = hc.headers[e_idx].value;
            analyze_cache_headers(hdr->value, pragma_val, expires_val, &rl);
        } else if (strcasecmp(hdr->name, "content-type") == 0) {
            analyze_content_type(hdr->value, body, body_size, &rl);
        } else if (strcasecmp(hdr->name, "content-language") == 0) {
            analyze_content_language(hdr->value, &rl);
        }
    }
    analyze_cookies(&hc, &rl);
    analyze_rate_limiting(url, &rl);
    analyze_xss_sql_injection(url, &rl);

    ReportEntry* entry = rl.head;
    while (entry) {
        if (entry->severity == SEV_CRITICAL) {
            res.score -= 50;
            cJSON_AddItemToArray(res.missing, cJSON_CreateString(entry->message));
        } else if (entry->severity == SEV_WARNING) {
            res.score -= 10;
            cJSON_AddItemToArray(res.notes, cJSON_CreateString(entry->message));
        } else {
            cJSON_AddItemToArray(res.notes, cJSON_CreateString(entry->message));
        }
        entry = entry->next;
    }
    if (res.score < 0) res.score = 0;

    report_print_and_free(&rl);
    return res;
}

void grading_result_free(grading_result* result) {
    if (!result) return;
    if (result->missing) { cJSON_Delete(result->missing); result->missing = NULL; }
    if (result->notes) { cJSON_Delete(result->notes); result->notes = NULL; }
}

int strcasestr_exists(const char* haystack, const char* needle) {
    if (!haystack || !needle) return 0;
    size_t needle_len = strlen(needle);
    size_t haystack_len = strlen(haystack);
    if (needle_len == 0) return 1;
    for (size_t i = 0; i <= haystack_len - needle_len; i++) {
        if (strncasecmp(&haystack[i], needle, needle_len) == 0) return 1;
    }
    return 0;
}

static const char* severity_to_str(Severity sev) {
    switch (sev) {
        case SEV_INFO: return "INFO";
        case SEV_WARNING: return "WARNING";
        case SEV_CRITICAL: return "CRITICAL";
        default: return "UNKNOWN";
    }
}

void report_init(ReportList* rl) {
    rl->head = NULL;
    rl->tail = NULL;
}

void report_add(ReportList* rl, Severity sev, const char* fmt, ...) {
    ReportEntry* entry = malloc(sizeof(ReportEntry));
    if (!entry) return;
    va_list args;
    va_start(args, fmt);
    vsnprintf(entry->message, sizeof(entry->message), fmt, args);
    va_end(args);
    entry->severity = sev;
    entry->next = NULL;
    if (!rl->head) {
        rl->head = rl->tail = entry;
    } else {
        rl->tail->next = entry;
        rl->tail = entry;
    }
}

void report_print_and_free(ReportList* rl) {
    ReportEntry* e = rl->head;
    while (e) {
        printf("[%s] %s\n", severity_to_str(e->severity), e->message);
        ReportEntry* next = e->next;
        free(e);
        e = next;
    }
    rl->head = rl->tail = NULL;
}

void parse_directives(const char* header_value, DirectiveList* dl) {
    dl->count = 0;
    const char* pos = header_value;
    while (*pos && dl->count < 64) {
        while (*pos && (isspace((unsigned char)*pos) || *pos == ';')) pos++;
        if (!*pos) break;
        const char* key_start = pos;
        while (*pos && *pos != '=' && *pos != ';') pos++;
        int key_len = (int)(pos - key_start);
        if (key_len <= 0) break;
        char key[64];
        if (key_len >= (int)sizeof(key)) key_len = sizeof(key) - 1;
        strncpy(key, key_start, key_len);
        key[key_len] = 0;
        int k = key_len - 1;
        while (k >= 0 && isspace((unsigned char)key[k])) { key[k] = 0; k--; }
        char value[256] = "";
        if (*pos == '=') {
            pos++;
            const char* val_start = pos;
            while (*pos && *pos != ';') pos++;
            int val_len = (int)(pos - val_start);
            if (val_len >= (int)sizeof(value)) val_len = sizeof(value) - 1;
            strncpy(value, val_start, val_len);
            value[val_len] = 0;
            int v = val_len - 1;
            while (v >= 0 && isspace((unsigned char)value[v])) { value[v] = 0; v--; }
        }
        strncpy(dl->directives[dl->count].key, key, sizeof(dl->directives[dl->count].key) - 1);
        dl->directives[dl->count].key[sizeof(dl->directives[dl->count].key) - 1] = 0;
        strncpy(dl->directives[dl->count].value, value, sizeof(dl->directives[dl->count].value) - 1);
        dl->directives[dl->count].value[sizeof(dl->directives[dl->count].value) - 1] = 0;
        dl->count++;
    }
}

const char* get_directive_value(const DirectiveList* dl, const char* key) {
    if (!dl || !key) return NULL;
    for (int i = 0; i < dl->count; i++) {
        if (strcasecmp(dl->directives[i].key, key) == 0) {
            return dl->directives[i].value[0] ? dl->directives[i].value : NULL;
        }
    }
    return NULL;
}

void analyze_hsts(const char* value, ReportList* rl) {
    if (!value || value[0] == 0) {
        report_add(rl, SEV_WARNING, "Strict-Transport-Security header is missing or empty.");
        return;
    }
    DirectiveList dl;
    parse_directives(value, &dl);
    const char* max_age_str = get_directive_value(&dl, "max-age");
    if (!max_age_str) {
        report_add(rl, SEV_CRITICAL, "HSTS header missing mandatory max-age directive.");
        return;
    }
    long max_age = strtol(max_age_str, NULL, 10);
    if (max_age < 15768000) {
        report_add(rl, SEV_WARNING, "HSTS max-age is too low (%ld); recommended at least 6 months.", max_age);
    } else if (max_age < 31536000) {
        report_add(rl, SEV_INFO, "HSTS max-age set to %ld seconds; considered acceptable.", max_age);
    } else {
        report_add(rl, SEV_INFO, "HSTS max-age set to %ld seconds; very good.", max_age);
    }
    if (get_directive_value(&dl, "includesubdomains") || get_directive_value(&dl, "includeSubDomains")) {
        report_add(rl, SEV_INFO, "HSTS includes 'includeSubDomains' directive.");
    } else {
        report_add(rl, SEV_WARNING, "HSTS missing 'includeSubDomains' directive; recommended to include.");
    }
    if (get_directive_value(&dl, "preload")) {
        report_add(rl, SEV_INFO, "HSTS 'preload' directive is set. Ensure domain is submitted to HSTS preload lists.");
    } else {
        report_add(rl, SEV_INFO, "HSTS 'preload' directive not set.");
    }
}

void analyze_x_frame_options(const char* value, ReportList* rl) {
    if (!value || value[0] == 0) {
        report_add(rl, SEV_WARNING, "X-Frame-Options header missing.");
        return;
    }
    if (strcasecmp(value, "deny") == 0) {
        report_add(rl, SEV_INFO, "X-Frame-Options set to DENY (strictest).");
    } else if (strcasecmp(value, "sameorigin") == 0) {
        report_add(rl, SEV_INFO, "X-Frame-Options set to SAMEORIGIN.");
    } else if (strncasecmp(value, "allow-from", 10) == 0) {
        report_add(rl, SEV_WARNING, "X-Frame-Options uses deprecated ALLOW-FROM.");
    } else {
        report_add(rl, SEV_WARNING, "X-Frame-Options has unknown or non-standard value '%s'.", value);
    }
}

void analyze_csp(const char* csp, ReportList* rl) {
    if (!csp || csp[0] == 0) {
        report_add(rl, SEV_WARNING, "Content-Security-Policy header missing or empty.");
        return;
    }
    if (strcasestr_exists(csp, "'unsafe-inline'")) {
        report_add(rl, SEV_WARNING, "CSP contains 'unsafe-inline' which weakens script protections.");
    }
    if (strcasestr_exists(csp, "'unsafe-eval'")) {
        report_add(rl, SEV_WARNING, "CSP contains 'unsafe-eval' which weakens script protections.");
    }
    if (!strcasestr_exists(csp, "default-src")) {
        report_add(rl, SEV_WARNING, "CSP missing 'default-src' directive; consider adding for better defaults.");
    } else {
        if (strcasestr_exists(csp, "default-src *") || strcasestr_exists(csp, "default-src 'unsafe-inline'") || strcasestr_exists(csp, "default-src data:")) {
            report_add(rl, SEV_WARNING, "CSP default-src allows wildcard or risky sources which can weaken security.");
        } else {
            report_add(rl, SEV_INFO, "CSP default-src directive looks restrictive.");
        }
    }
}

void analyze_x_content_type_options(const char* value, ReportList* rl) {
    if (!value) {
        report_add(rl, SEV_WARNING, "X-Content-Type-Options header missing.");
        return;
    }
    if (strcasecmp(value, "nosniff") == 0) {
        report_add(rl, SEV_INFO, "X-Content-Type-Options correctly set to 'nosniff'.");
    } else {
        report_add(rl, SEV_WARNING, "X-Content-Type-Options has unexpected value '%s'; recommended 'nosniff'.", value);
    }
}

void analyze_referrer_policy(const char* value, ReportList* rl) {
    if (!value || value[0] == 0) {
        report_add(rl, SEV_INFO, "Referrer-Policy header missing; browser default applied.");
        return;
    }
    const char* valid_policies[] = {
        "no-referrer", "no-referrer-when-downgrade", "origin", "origin-when-cross-origin",
        "same-origin", "strict-origin", "strict-origin-when-cross-origin", "unsafe-url"
    };
    int valid = 0;
    for (size_t i = 0; i < sizeof(valid_policies)/sizeof(valid_policies[0]); i++) {
        if (strcasecmp(value, valid_policies[i]) == 0) { valid = 1; break; }
    }
    if (valid) {
        report_add(rl, SEV_INFO, "Referrer-Policy is set to '%s'.", value);
    } else {
        report_add(rl, SEV_WARNING, "Referrer-Policy has non-standard or weak value '%s'.", value);
    }
}

void analyze_feature_policy(const char* value, ReportList* rl) {
    if (!value || value[0] == 0) {
        report_add(rl, SEV_INFO, "Feature-Policy or Permissions-Policy header missing.");
        return;
    }
    char val_copy[MAX_HEADER_VALUE];
    strncpy(val_copy, value, sizeof(val_copy)-1);
    val_copy[sizeof(val_copy)-1] = 0;
    for (size_t i = 0; i < strlen(val_copy); ++i) val_copy[i] = (char)tolower((unsigned char)val_copy[i]);
    if (strstr(val_copy, "camera 'none'") || strstr(val_copy, "microphone 'none'")) {
        report_add(rl, SEV_INFO, "Feature-Policy restricts camera and microphone usage.");
    } else {
        report_add(rl, SEV_WARNING, "Feature-Policy does not restrict sensitive features like camera or microphone.");
    }
    if (strstr(val_copy, "geolocation 'none'")) {
        report_add(rl, SEV_INFO, "Feature-Policy restricts geolocation.");
    }
    if (strstr(val_copy, "*") || strstr(val_copy, "allow=*")) {
        report_add(rl, SEV_WARNING, "Feature-Policy includes wildcard or very permissive allow rules.");
    }
}

void analyze_cache_headers(const char* cache_control, const char* pragma, const char* expires, ReportList* rl) {
    if (!cache_control) {
        report_add(rl, SEV_WARNING, "Cache-Control header missing.");
        return;
    }
    if (strcasestr_exists(cache_control, "no-store") && strcasestr_exists(cache_control, "no-cache")) {
        report_add(rl, SEV_INFO, "Cache-Control correctly prevents caching (no-store, no-cache).");
    } else if (strcasestr_exists(cache_control, "max-age")) {
        report_add(rl, SEV_INFO, "Cache-Control specifies max-age (caching enabled). Check for sensitive content.");
    } else if (strcasestr_exists(cache_control, "public")) {
        report_add(rl, SEV_WARNING, "Cache-Control 'public' is set. Review if sensitive content is exposed.");
    } else {
        report_add(rl, SEV_WARNING, "Cache-Control header present but no clear caching policy found.");
    }
    if (pragma && strcasestr_exists(pragma, "no-cache")) {
        report_add(rl, SEV_INFO, "Pragma set to no-cache.");
    }
    if (expires) {
        if (strstr(expires, "1970") || strstr(expires, "-1")) {
            report_add(rl, SEV_INFO, "Expires header set to epoch or invalid value, effectively disabling caching.");
        } else {
            report_add(rl, SEV_INFO, "Expires header value: %s", expires);
        }
    }
}

void analyze_content_language(const char* value, ReportList* rl) {
    if (!value) {
        report_add(rl, SEV_INFO, "Content-Language header missing.");
        return;
    }
    report_add(rl, SEV_INFO, "Content-Language set to '%s'.", value);
}

void analyze_security_headers_presence(const HeaderCollection* hc, ReportList* rl) {
    const char* critical_headers[] = {
        "strict-transport-security", "content-security-policy", "x-content-type-options",
        "x-frame-options", "referrer-policy", "permissions-policy"
    };
    for (size_t i = 0; i < sizeof(critical_headers)/sizeof(critical_headers[0]); i++) {
        int found = 0;
        for (int j = 0; j < hc->count; j++) {
            if (strcasecmp(hc->headers[j].name, critical_headers[i]) == 0) { found = 1; break; }
        }
        if (!found) {
            report_add(rl, SEV_WARNING, "Security header '%s' is missing.", critical_headers[i]);
        }
    }
}