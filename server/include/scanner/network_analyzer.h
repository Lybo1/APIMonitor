#ifndef NETWORK_ANALYZER_H
#define NETWORK_ANALYZER_H

#include <openssl/ssl.h>
#include <pthread.h>
#include <stdint.h>

#define MAX_HOSTNAME 256
#define MAX_PORTS 65536
#define MAX_BANNER 1024
#define MAX_REPORT_MESSAGE 1024
#define SCAN_TIMEOUT_MS 2000
#define MAX_THREADS 16
#define RATE_LIMIT_MS 10
#define MAX_INPUT_LEN 1024

typedef enum {
    NA_SEV_INFO,
    NA_SEV_WARNING,
    NA_SEV_CRITICAL
} NASeverity;

typedef struct NAReportEntry {
    char message[MAX_REPORT_MESSAGE];
    NASeverity severity;
    struct NAReportEntry* next;
} NAReportEntry;

typedef struct {
    NAReportEntry* head;
    NAReportEntry* tail;
    pthread_mutex_t mutex;
} NAReportList;

typedef struct {
    uint16_t port;
    int is_open;
    char service[64];
    char banner[MAX_BANNER];
} NAPortResult;

typedef struct {
    const char* version_str;
    int is_secure;
} NATLSInfo;

typedef struct {
    char hostname[MAX_HOSTNAME];
    uint16_t port_start;
    uint16_t port_end;
    int scan_tcp;
    int scan_udp;
    int scan_icmp;
    int max_threads;
    int timeout_ms;
    NAReportList* report;
} NAScanConfig;

void na_report_init(NAReportList* rl);

void na_report_add(NAReportList* rl, NASeverity sev, const char* fmt, ...);

void na_report_print_and_free(NAReportList* rl);

int na_parse_user_input(const char* input, char* hostname, size_t hostname_len, uint16_t* port, NAReportList* rl);

int na_analyze_tls_protocol(const char* hostname, uint16_t port, NAReportList* rl);

int na_grab_service_banner(const char* hostname, uint16_t port, char* banner, size_t banner_len, NAReportList* rl);

int na_port_scan(NAScanConfig* config, NAPortResult* results, size_t* result_count);

void na_cleanup_openssl(void);

#endif
