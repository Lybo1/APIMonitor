#define _POSIX_C_SOURCE 200809L

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/stat.h>
#include <netdb.h>
#include <pthread.h>
#include <regex.h>
#include "../include/scanner/network_analyzer.h"
#include "../include/scanner/http_headers_analyzer.h"
#include <cjson/cJSON.h>

#define SOCKET_PATH "/tmp/analyzer.sock"
#define MAX_BUFFER 8192
#define MAX_URL 2048
#define BACKLOG 5

static int validate_url(const char *restrict url, ReportList *restrict rl) {
    regex_t regex;
    const char *pattern = "^https?://[a-zA-Z0-9.-]+(:[0-9]+)?(/.*)?$";

    if (regcomp(&regex, pattern, REG_EXTENDED | REG_NOSUB) != 0) {
        report_add(rl, SEV_CRITICAL, "Failed to compile URL validation regex");
        return 0;
    }

    const int valid = regexec(&regex, url, 0, NULL, 0) == 0;

    regfree(&regex);

    if (!valid) {
        report_add(rl, SEV_WARNING, "Invalid URL format: %s", url);
    }
    return valid;
}

static int extract_hostname(const char *restrict url, char *restrict hostname, size_t hostname_len, ReportList *restrict rl) {
    const char *start = strstr(url, "://");

    if (!start) {
        report_add(rl, SEV_CRITICAL, "Invalid URL, no protocol found: %s", url);
        return 0;
    }

    start += 3;
    const char *end = strchr(start, '/');

    if (!end) {
        end = start + strlen(start);
    }

    size_t len = end - start;

    if (len >= hostname_len) {
        report_add(rl, SEV_CRITICAL, "Hostname too long in URL: %s", url);
        return 0;
    }

    strncpy(hostname, start, len);
    hostname[len] = '\0';
    char *port = strchr(hostname, ':');

    if (port) {
        *port = '\0';
    }

    return 1;
}

static cJSON *report_list_to_json(ReportList *restrict rl) {
    cJSON *array = cJSON_CreateArray();

    for (ReportEntry *entry = rl->head; entry; entry = entry->next) {
        cJSON *item = cJSON_CreateObject();
        cJSON_AddStringToObject(item, "message", entry->message);

        const char *sev_str = entry->severity == SEV_INFO ? "info" :
                              entry->severity == SEV_WARNING ? "warning" : "critical";

        cJSON_AddStringToObject(item, "severity", sev_str);
        cJSON_AddItemToArray(array, item);
    }
    return array;
}

static cJSON *na_report_list_to_json(NAReportList *restrict rl) {
    cJSON *array = cJSON_CreateArray();
    pthread_mutex_lock(&rl->mutex);

    for (NAReportEntry *entry = rl->head; entry; entry = entry->next) {
        cJSON *item = cJSON_CreateObject();
        cJSON_AddStringToObject(item, "message", entry->message);

        const char *sev_str = entry->severity == NA_SEV_INFO ? "info" :
                              entry->severity == NA_SEV_WARNING ? "warning" : "critical";

        cJSON_AddStringToObject(item, "severity", sev_str);
        cJSON_AddItemToArray(array, item);
    }
    pthread_mutex_unlock(&rl->mutex);
    return array;
}

static void process_http(const char *restrict url, cJSON *restrict response, ReportList *restrict rl) {
    cJSON *headers_json = NULL;
    char *html = NULL;

    if (http_fetch_url(url, &headers_json, &html) != 0) {
        report_add(rl, SEV_CRITICAL, "Failed to fetch URL: %s", url);
        cJSON_AddStringToObject(response, "status", "error");
        cJSON_AddItemToObject(response, "request", report_list_to_json(rl));
        return;
    }

    grading_result gr = grading_analyze(headers_json, url, html, html ? strlen(html) : 0);
    cJSON *report = report_list_to_json(rl);
    cJSON_AddStringToObject(response, "status", "success");
    cJSON_AddItemToObject(response, "report", report);
    cJSON *grading = cJSON_CreateObject();
    cJSON_AddNumberToObject(grading, "score", gr.score);
    if (gr.missing) cJSON_AddItemToObject(grading, "missing", gr.missing);
    if (gr.notes) cJSON_AddItemToObject(grading, "notes", gr.notes);
    cJSON_AddItemToObject(response, "grading", grading);

    grading_result_free(&gr);
    cJSON_Delete(headers_json);
    free(html);
}

// Process network analysis
static void process_network(const char *restrict url, cJSON *restrict response, ReportList *restrict tmp_rl) {
    char hostname[MAX_HOSTNAME] = {0};
    if (!extract_hostname(url, hostname, sizeof(hostname), tmp_rl)) {
        cJSON_AddStringToObject(response, "status", "error");
        cJSON_AddItemToObject(response, "report", report_list_to_json(tmp_rl));
        return;
    }

    NAReportList na_rl;
    na_report_init(&na_rl);

    // TLS analysis
    na_analyze_tls_protocol(hostname, 443, &na_rl);

    NAScanConfig config = {0};

    strncpy(config.hostname, hostname, sizeof(config.hostname) - 1);
    config.hostname[sizeof(config.hostname) - 1] = '\0';

    config.port_start = 80;
    config.port_end = 443;
    config.scan_tcp = 1;
    config.scan_udp = 0;
    config.scan_icmp = 0;
    config.max_threads = MAX_THREADS;
    config.timeout_ms = SCAN_TIMEOUT_MS;
    config.report = &na_rl;


    NAPortResult results[MAX_PORTS] = {0};
    size_t result_count = 0;
    na_port_scan(&config, results, &result_count);

    cJSON *report = na_report_list_to_json(&na_rl);
    cJSON_AddStringToObject(response, "status", "success");
    cJSON_AddItemToObject(response, "report", report);

    na_report_print_and_free(&na_rl); // Also destroys mutex
}

// Handle client request
static void handle_client(int client_fd, ReportList *restrict rl) {
    char buffer[MAX_BUFFER] = {0};
    ssize_t bytes = recv(client_fd, buffer, sizeof(buffer) - 1, 0);
    if (bytes <= 0) {
        report_add(rl, SEV_WARNING, "Failed to receive data from client");
        return;
    }

    cJSON *request = cJSON_Parse(buffer);
    if (!request) {
        report_add(rl, SEV_CRITICAL, "Invalid JSON request: %s", cJSON_GetErrorPtr());
        return;
    }

    cJSON *url_json = cJSON_GetObjectItemCaseSensitive(request, "url");
    cJSON *analyzer_json = cJSON_GetObjectItemCaseSensitive(request, "analyzer");
    if (!cJSON_IsString(url_json) || !cJSON_IsString(analyzer_json)) {
        report_add(rl, SEV_CRITICAL, "Missing or invalid 'url' or 'analyzer' in request");
        cJSON_Delete(request);
        return;
    }

    const char *url = url_json->valuestring;
    const char *analyzer = analyzer_json->valuestring;
    if (!validate_url(url, rl)) {
        cJSON_Delete(request);
        return;
    }

    cJSON *response = cJSON_CreateObject();
    if (strcmp(analyzer, "http") == 0) {
        process_http(url, response, rl);
    } else if (strcmp(analyzer, "network") == 0) {
        process_network(url, response, rl);
    } else {
        report_add(rl, SEV_WARNING, "Unknown analyzer type: %s", analyzer);
        cJSON_AddStringToObject(response, "status", "error");
        cJSON_AddItemToObject(response, "report", report_list_to_json(rl));
    }

    char *response_str = cJSON_PrintUnformatted(response);
    send(client_fd, response_str, strlen(response_str), 0);
    cJSON_Delete(request);
    cJSON_Delete(response);
    free(response_str);
}

int main(void) {
    ReportList rl;
    report_init(&rl);

    // Create UDS socket
    int server_fd = socket(AF_UNIX, SOCK_STREAM, 0);
    if (server_fd < 0) {
        report_add(&rl, SEV_CRITICAL, "Failed to create socket: %m");
        report_print_and_free(&rl);
        return EXIT_FAILURE;
    }

    // Unlink existing socket file
    unlink(SOCKET_PATH);

    struct sockaddr_un addr = { .sun_family = AF_UNIX };
    strncpy(addr.sun_path, SOCKET_PATH, sizeof(addr.sun_path) - 1);
    if (bind(server_fd, (struct sockaddr*)&addr, sizeof(addr)) < 0) {
        report_add(&rl, SEV_CRITICAL, "Failed to bind socket: %m");
        close(server_fd);
        report_print_and_free(&rl);
        return EXIT_FAILURE;
    }

    // Set socket permissions
    if (chmod(SOCKET_PATH, 0600) < 0) {
        report_add(&rl, SEV_WARNING, "Failed to set socket permissions: %m");
    }

    if (listen(server_fd, BACKLOG) < 0) {
        report_add(&rl, SEV_CRITICAL, "Failed to listen on socket: %m");
        close(server_fd);
        unlink(SOCKET_PATH);
        report_print_and_free(&rl);
        return EXIT_FAILURE;
    }

    while (1) {
        int client_fd = accept(server_fd, NULL, NULL);
        if (client_fd < 0) {
            report_add(&rl, SEV_WARNING, "Failed to accept client: %m");
            continue;
        }

        handle_client(client_fd, &rl);
        close(client_fd);
    }

    close(server_fd);
    unlink(SOCKET_PATH);
    na_cleanup_openssl();
    report_print_and_free(&rl);
    return EXIT_SUCCESS;
}