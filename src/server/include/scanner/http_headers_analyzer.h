#ifndef HTTP_HEADERS_ANALYZER_H
#define HTTP_HEADERS_ANALYZER_H

#include <cjson/cJSON.h>

#define MAX_HEADER_NAME 128
#define MAX_HEADER_VALUE 1024
#define MAX_HEADERS 100

typedef enum { SEV_INFO, SEV_WARNING, SEV_CRITICAL } Severity;

typedef struct {
    char name[MAX_HEADER_NAME];
    char value[MAX_HEADER_VALUE];
    int duplicates;
} HttpHeader;

typedef struct {
    HttpHeader headers[MAX_HEADERS];
    int count;
} HeaderCollection;

typedef struct ReportEntry {
    char message[1024];
    Severity severity;
    struct ReportEntry* next;
} ReportEntry;

typedef struct {
    ReportEntry* head;
    ReportEntry* tail;
} ReportList;

typedef struct {
    char key[64];
    char value[256];
} Directive;

typedef struct {
    Directive directives[64];
    int count;
} DirectiveList;

typedef struct {
    int score;
    cJSON* missing;
    cJSON* notes;
} grading_result;

void report_init(ReportList* rl);
void report_add(ReportList* rl, Severity sev, const char* fmt, ...);
void report_print_and_free(ReportList* rl);
int http_fetch_url(const char* url, cJSON** out_headers, char** out_html);
void normalize_name(char* dst, const char* src);
void trim_whitespace(char** str_ptr);
int find_header(HeaderCollection* hc, const char* name);
void add_header(HeaderCollection* hc, const char* name_raw, const char* value_raw);
void parse_raw_headers(const char* raw_headers, HeaderCollection* hc);
void detect_file_type(const char* data, size_t size, char* file_type, size_t file_type_len);
void analyze_content_type(const char* value, const char* body, size_t body_size, ReportList* rl);
void analyze_rate_limiting(const char* url, ReportList* rl);
void analyze_xss_sql_injection(const char* url, ReportList* rl);
void analyze_cookies(const HeaderCollection* hc, ReportList* rl);
grading_result grading_analyze(cJSON* headers_json, const char* url, char* body, size_t body_size);
void grading_result_free(grading_result* result);
int strcasestr_exists(const char* haystack, const char* needle);
void parse_directives(const char* header_value, DirectiveList* dl);
const char* get_directive_value(const DirectiveList* dl, const char* key);
void analyze_hsts(const char* value, ReportList* rl);
void analyze_x_frame_options(const char* value, ReportList* rl);
void analyze_csp(const char* csp, ReportList* rl);
void analyze_x_content_type_options(const char* value, ReportList* rl);
void analyze_referrer_policy(const char* value, ReportList* rl);
void analyze_feature_policy(const char* value, ReportList* rl);
void analyze_cache_headers(const char* cache_control, const char* pragma, const char* expires, ReportList* rl);
void analyze_content_language(const char* value, ReportList* rl);
void analyze_security_headers_presence(const HeaderCollection* hc, ReportList* rl);

#endif