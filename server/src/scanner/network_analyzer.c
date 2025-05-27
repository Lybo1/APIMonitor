#include "../../include/scanner/network_analyzer.h"
#include <arpa/inet.h>
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/time.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <strings.h>
#include <regex.h>
#include <time.h>
#include <openssl/err.h>

typedef struct {
    uint16_t port;
    const char* protocol;
    const char* probe;
    const char* service_name;
} NAServiceProbe;

static const NAServiceProbe service_probes[] = {
    {80, "tcp", "GET / HTTP/1.1\r\nHost: %s\r\n\r\n", "http"},
    {443, "tcp", "GET / HTTP/1.1\r\nHost: %s\r\n\r\n", "https"},
    {21, "tcp", "HELP\r\n", "ftp"},
    {22, "tcp", "", "ssh"},
    {25, "tcp", "HELO localhost\r\n", "smtp"},
    {0, NULL, NULL, NULL} // Sentinel
};

// TLS version mapping
static const NATLSInfo tls_versions[] = {
    {"SSLv3", 0},
    {"TLSv1.0", 0},
    {"TLSv1.1", 0},
    {"TLSv1.2", 1},
    {"TLSv1.3", 1},
    {NULL, 0}
};

// Initialize report list
void na_report_init(NAReportList* rl) {
    if (!rl) return;
    rl->head = rl->tail = NULL;
    pthread_mutex_init(&rl->mutex, NULL);
}

// Add report entry (thread-safe)
void na_report_add(NAReportList* rl, NASeverity sev, const char* fmt, ...) {
    if (!rl) return;

    NAReportEntry* entry = calloc(1, sizeof(NAReportEntry));
    if (!entry) {
        return;
    }

    va_list args;
    va_start(args, fmt);
    vsnprintf(entry->message, sizeof(entry->message), fmt, args);
    va_end(args);

    entry->severity = sev;
    entry->next = NULL;

    pthread_mutex_lock(&rl->mutex);
    if (!rl->head) {
        rl->head = rl->tail = entry;
    } else {
        rl->tail->next = entry;
        rl->tail = entry;
    }
    pthread_mutex_unlock(&rl->mutex);
}

// Print and free report list
void na_report_print_and_free(NAReportList* rl) {
    if (!rl) return;

    NAReportEntry* e = rl->head;
    while (e) {
        const char* sev_str = e->severity == NA_SEV_INFO ? "INFO" :
                              e->severity == NA_SEV_WARNING ? "WARNING" : "CRITICAL";
        printf("[%s] %s\n", sev_str, e->message);
        NAReportEntry* next = e->next;
        free(e);
        e = next;
    }
    rl->head = rl->tail = NULL;
    pthread_mutex_destroy(&rl->mutex);
}

int na_parse_user_input(const char* input, char* hostname, size_t hostname_len, uint16_t* port, NAReportList* rl) {
    if (!input || !hostname || !port || hostname_len < 1 || !rl) {
        na_report_add(rl, NA_SEV_CRITICAL, "Invalid input parameters for parsing");
        return -1;
    }

    if (strnlen(input, MAX_INPUT_LEN) >= MAX_INPUT_LEN) {
        na_report_add(rl, NA_SEV_CRITICAL, "Input too long: %.*s", MAX_INPUT_LEN - 1, input);
        return -1;
    }

    regex_t regex;
    regmatch_t matches[4];
    const char* pattern = "^(https?|ftp)://([^:/]+)(:([0-9]{1,5}))?";

    if (regcomp(&regex, pattern, REG_EXTENDED | REG_ICASE) != 0) {
        na_report_add(rl, NA_SEV_WARNING, "Failed to compile regex for input parsing");
        return -1;
    }

    char temp_hostname[MAX_HOSTNAME];
    uint16_t temp_port = 0;
    int result = -1;

    if (regexec(&regex, input, 4, matches, 0) == 0) {
        // Extract hostname
        size_t host_start = matches[2].rm_so;
        size_t host_len = matches[2].rm_eo - host_start;
        if (host_len >= hostname_len || host_len >= MAX_HOSTNAME) {
            na_report_add(rl, NA_SEV_CRITICAL, "Hostname too long in input: %.*s", MAX_INPUT_LEN - 1, input);
            goto cleanup;
        }
        strncpy(temp_hostname, input + host_start, host_len);
        temp_hostname[host_len] = '\0';

        if (matches[4].rm_so != -1) {
            char port_str[6];
            size_t port_len = matches[4].rm_eo - matches[4].rm_so;
            if (port_len >= sizeof(port_str)) {
                na_report_add(rl, NA_SEV_CRITICAL, "Port number too long in input: %.*s", MAX_INPUT_LEN - 1, input);
                goto cleanup;
            }
            strncpy(port_str, input + matches[4].rm_so, port_len);
            port_str[port_len] = '\0';
            temp_port = (uint16_t)atoi(port_str);
            if (temp_port == 0) {
                na_report_add(rl, NA_SEV_CRITICAL, "Invalid port number in input: %.*s", MAX_INPUT_LEN - 1, input);
                goto cleanup;
            }
        } else {
            // Assign default port based on scheme
            if (strncasecmp(input, "https", 5) == 0) temp_port = 443;
            else if (strncasecmp(input, "http", 4) == 0) temp_port = 80;
            else if (strncasecmp(input, "ftp", 3) == 0) temp_port = 21;
            else {
                na_report_add(rl, NA_SEV_CRITICAL, "Unknown scheme in input: %.*s", MAX_INPUT_LEN - 1, input);
                goto cleanup;
            }
        }
        result = 0;
    } else {
        // Try hostname:port format or bare hostname
        const char* simple_pattern = "^([^:]+)(:([0-9]{1,5}))?$";
        if (regcomp(&regex, simple_pattern, REG_EXTENDED | REG_ICASE) != 0) {
            na_report_add(rl, NA_SEV_WARNING, "Failed to compile simple regex for input parsing");
            return -1;
        }
        if (regexec(&regex, input, 4, matches, 0) == 0) {
            size_t host_start = matches[1].rm_so;
            size_t host_len = matches[1].rm_eo - host_start;
            if (host_len >= hostname_len || host_len >= MAX_HOSTNAME) {
                na_report_add(rl, NA_SEV_CRITICAL, "Hostname too long in input: %.*s", MAX_INPUT_LEN - 1, input);
                goto cleanup;
            }
            strncpy(temp_hostname, input + host_start, host_len);
            temp_hostname[host_len] = '\0';

            if (matches[3].rm_so != -1) {
                char port_str[6];
                size_t port_len = matches[3].rm_eo - matches[3].rm_so;
                if (port_len >= sizeof(port_str)) {
                    na_report_add(rl, NA_SEV_CRITICAL, "Port number too long in input: %.*s", MAX_INPUT_LEN - 1, input);
                    goto cleanup;
                }
                strncpy(port_str, input + matches[3].rm_so, port_len);
                port_str[port_len] = '\0';
                temp_port = (uint16_t)atoi(port_str);
                if (temp_port == 0) {
                    na_report_add(rl, NA_SEV_CRITICAL, "Invalid port number in input: %.*s", MAX_INPUT_LEN - 1, input);
                    goto cleanup;
                }
            } else {
                temp_port = 443; // Default to HTTPS port
            }
            result = 0;
        } else {
            na_report_add(rl, NA_SEV_CRITICAL, "Invalid input format: %.*s", MAX_INPUT_LEN - 1, input);
        }
    }

    if (result == 0) {
        strncpy(hostname, temp_hostname, hostname_len - 1);
        hostname[hostname_len - 1] = '\0';
        *port = temp_port;
        na_report_add(rl, NA_SEV_INFO, "Parsed input %.*s: hostname=%s, port=%u", MAX_INPUT_LEN - 1, input, hostname, *port);
    }

cleanup:
    regfree(&regex);
    return result;
}

// Resolve hostname to IP
static int resolve_host(const char* hostname, struct sockaddr_in* addr) {
    if (!hostname || !addr) return -1;

    struct addrinfo hints = {0}, *res = NULL;
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_STREAM;

    int status = getaddrinfo(hostname, NULL, &hints, &res);
    if (status != 0) {
        return -1;
    }

    memcpy(addr, res->ai_addr, sizeof(struct sockaddr_in));
    freeaddrinfo(res);
    return 0;
}

// Set socket timeout
static int set_socket_timeout(int sock, int timeout_ms) {
    struct timeval tv;
    tv.tv_sec = timeout_ms / 1000;
    tv.tv_usec = (timeout_ms % 1000) * 1000;
    return setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof(tv)) ||
           setsockopt(sock, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof(tv));
}

// Helper to capture OpenSSL errors
static void log_openssl_errors(NAReportList* rl, const char* context, const char* hostname, uint16_t port) {
    if (!rl) return;

    char err_buf[256];
    unsigned long err;
    while ((err = ERR_get_error()) != 0) {
        ERR_error_string_n(err, err_buf, sizeof(err_buf));
        err_buf[sizeof(err_buf) - 1] = '\0';
        na_report_add(rl, NA_SEV_WARNING, "%s for %s:%u: %s", context, hostname, port, err_buf);
    }
}

// Analyze TLS protocol version
int na_analyze_tls_protocol(const char* hostname, uint16_t port, NAReportList* rl) {
    if (!hostname || !rl) {
        na_report_add(rl, NA_SEV_CRITICAL, "Invalid parameters for TLS analysis");
        return -1;
    }

#if OPENSSL_VERSION_NUMBER < 0x10100000L
    SSL_library_init();
    OpenSSL_add_all_algorithms();
    SSL_load_error_strings();
#else
    OPENSSL_init_ssl(OPENSSL_INIT_LOAD_SSL_STRINGS | OPENSSL_INIT_ADD_ALL_CIPHERS, NULL);
#endif

    SSL_CTX* ctx = SSL_CTX_new(TLS_client_method());
    if (!ctx) {
        log_openssl_errors(rl, "Failed to create SSL context", hostname, port);
        return -1;
    }

    SSL_CTX_set_options(ctx, SSL_OP_NO_SSLv2 | SSL_OP_NO_SSLv3 | SSL_OP_NO_TLSv1 | SSL_OP_NO_TLSv1_1);

    int sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Socket creation failed for %s:%u: %s", hostname, port, strerror(errno));
        SSL_CTX_free(ctx);
        return -1;
    }

    struct sockaddr_in server;
    if (resolve_host(hostname, &server) < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Failed to resolve hostname %s: %s", hostname, gai_strerror(errno));
        close(sock);
        SSL_CTX_free(ctx);
        return -1;
    }
    server.sin_port = htons(port);

    if (set_socket_timeout(sock, SCAN_TIMEOUT_MS) < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Failed to set socket timeout for %s:%u: %s", hostname, port, strerror(errno));
        close(sock);
        SSL_CTX_free(ctx);
        return -1;
    }

    if (connect(sock, (struct sockaddr*)&server, sizeof(server)) < 0) {
        na_report_add(rl, NA_SEV_INFO, "Connection failed to %s:%u: %s", hostname, port, strerror(errno));
        close(sock);
        SSL_CTX_free(ctx);
        return -1;
    }

    SSL* ssl = SSL_new(ctx);
    if (!ssl) {
        log_openssl_errors(rl, "Failed to create SSL object", hostname, port);
        close(sock);
        SSL_CTX_free(ctx);
        return -1;
    }

    SSL_set_fd(ssl, sock);
    SSL_set_tlsext_host_name(ssl, hostname);

    if (SSL_connect(ssl) <= 0) {
        log_openssl_errors(rl, "TLS handshake failed", hostname, port);
        SSL_free(ssl);
        close(sock);
        SSL_CTX_free(ctx);
        return -1;
    }

    const SSL_CIPHER* cipher = SSL_get_current_cipher(ssl);
    const char* version = SSL_get_version(ssl);
    int is_secure = 0;
    for (int i = 0; tls_versions[i].version_str; i++) {
        if (strcmp(version, tls_versions[i].version_str) == 0) {
            is_secure = tls_versions[i].is_secure;
            break;
        }
    }

    if (is_secure) {
        na_report_add(rl, NA_SEV_INFO, "Secure TLS version %s detected on %s:%u (Cipher: %s)",
                      version, hostname, port, SSL_CIPHER_get_name(cipher));
    } else {
        na_report_add(rl, NA_SEV_CRITICAL, "Insecure TLS version %s detected on %s:%u (Cipher: %s)",
                      version, hostname, port, SSL_CIPHER_get_name(cipher));
    }

    SSL_free(ssl);
    close(sock);
    SSL_CTX_free(ctx);
    return 0;
}

// Perform service banner grabbing
int na_grab_service_banner(const char* hostname, uint16_t port, char* banner, size_t banner_len, NAReportList* rl) {
    if (!hostname || !banner || banner_len < 1 || !rl) {
        na_report_add(rl, NA_SEV_CRITICAL, "Invalid parameters for banner grabbing");
        return -1;
    }

    int sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Socket creation failed for %s:%u: %s", hostname, port, strerror(errno));
        return -1;
    }

    struct sockaddr_in server;
    if (resolve_host(hostname, &server) < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Failed to resolve hostname %s: %s", hostname, gai_strerror(errno));
        close(sock);
        return -1;
    }
    server.sin_port = htons(port);

    if (set_socket_timeout(sock, SCAN_TIMEOUT_MS) < 0) {
        na_report_add(rl, NA_SEV_WARNING, "Failed to set socket timeout for %s:%u: %s", hostname, port, strerror(errno));
        close(sock);
        return -1;
    }

    if (connect(sock, (struct sockaddr*)&server, sizeof(server)) < 0) {
        na_report_add(rl, NA_SEV_INFO, "Connection failed to %s:%u: %s", hostname, port, strerror(errno));
        close(sock);
        return -1;
    }

    const NAServiceProbe* probe = NULL;
    for (int i = 0; service_probes[i].port; i++) {
        if (service_probes[i].port == port && strcmp(service_probes[i].protocol, "tcp") == 0) {
            probe = &service_probes[i];
            break;
        }
    }

    if (probe && probe->probe[0]) {
        char request[512];
        snprintf(request, sizeof(request), probe->probe, hostname);
        if (send(sock, request, strlen(request), 0) < 0) {
            na_report_add(rl, NA_SEV_WARNING, "Failed to send probe to %s:%u: %s", hostname, port, strerror(errno));
            close(sock);
            return -1;
        }
    }

    char buffer[MAX_BANNER];
    ssize_t received = recv(sock, buffer, sizeof(buffer) - 1, 0);
    if (received <= 0) {
        na_report_add(rl, NA_SEV_INFO, "No banner received from %s:%u", hostname, port);
        close(sock);
        return -1;
    }

    buffer[received] = '\0';
    char* newline = strpbrk(buffer, "\r\n");
    if (newline) *newline = '\0';
    strncpy(banner, buffer, banner_len - 1);
    banner[banner_len - 1] = '\0';

    na_report_add(rl, NA_SEV_INFO, "Banner grabbed from %s:%u: %s", hostname, port, banner);
    close(sock);
    return 0;
}

// Thread worker for port scanning
typedef struct {
    NAScanConfig* config;
    uint16_t port;
    NAPortResult* result;
} NAPortScanArg;

static void* scan_port_worker(void* arg) {
    NAPortScanArg* scan_arg = (NAPortScanArg*)arg;
    if (!scan_arg) return NULL;

    NAScanConfig* config = scan_arg->config;
    uint16_t port = scan_arg->port;
    NAPortResult* result = scan_arg->result;

    if (!config || !config->report || !result) {
        na_report_add(config ? config->report : NULL, NA_SEV_CRITICAL, "Invalid scan arguments for port %u", port);
        return NULL;
    }

    result->port = port;
    result->is_open = 0;
    strncpy(result->service, "unknown", sizeof(result->service) - 1);
    result->service[sizeof(result->service) - 1] = '\0';
    result->banner[0] = '\0';

    int sock = -1;
    struct sockaddr_in server;
    if (resolve_host(config->hostname, &server) < 0) {
        na_report_add(config->report, NA_SEV_WARNING, "Failed to resolve hostname %s for port %u: %s", config->hostname, port, gai_strerror(errno));
        return NULL;
    }
    server.sin_port = htons(port);

    if (config->scan_tcp) {
        sock = socket(AF_INET, SOCK_STREAM, 0);
        if (sock < 0) {
            na_report_add(config->report, NA_SEV_WARNING, "TCP socket creation failed for %s:%u: %s", config->hostname, port, strerror(errno));
            return NULL;
        }

        if (set_socket_timeout(sock, config->timeout_ms) < 0) {
            na_report_add(config->report, NA_SEV_WARNING, "Failed to set TCP socket timeout for %s:%u: %s", config->hostname, port, strerror(errno));
            close(sock);
            return NULL;
        }

        if (connect(sock, (struct sockaddr*)&server, sizeof(server)) == 0) {
            result->is_open = 1;
            for (int i = 0; service_probes[i].port; i++) {
                if (service_probes[i].port == port) {
                    strncpy(result->service, service_probes[i].service_name, sizeof(result->service) - 1);
                    result->service[sizeof(result->service) - 1] = '\0';
                    break;
                }
            }
            na_grab_service_banner(config->hostname, port, result->banner, sizeof(result->banner), config->report);
        }
        close(sock);
    }

    if (config->scan_udp && !result->is_open) {
        sock = socket(AF_INET, SOCK_DGRAM, 0);
        if (sock < 0) {
            na_report_add(config->report, NA_SEV_WARNING, "UDP socket creation failed for %s:%u: %s", config->hostname, port, strerror(errno));
            return NULL;
        }

        if (set_socket_timeout(sock, config->timeout_ms) < 0) {
            na_report_add(config->report, NA_SEV_WARNING, "Failed to set UDP socket timeout for %s:%u: %s", config->hostname, port, strerror(errno));
            close(sock);
            return NULL;
        }

        char probe[] = "\x00\x00\x00\x00";
        if (sendto(sock, probe, sizeof(probe), 0, (struct sockaddr*)&server, sizeof(server)) >= 0) {
            char buffer[64];
            if (recvfrom(sock, buffer, sizeof(buffer), 0, NULL, NULL) > 0) {
                result->is_open = 1;
                strncpy(result->service, "udp-service", sizeof(result->service) - 1);
                result->service[sizeof(result->service) - 1] = '\0';
            }
        }
        close(sock);
    }

    if (result->is_open) {
        na_report_add(config->report, NA_SEV_INFO, "Port %u/%s open on %s: %s%s%s",
                      port, config->scan_tcp ? "tcp" : "udp", config->hostname, result->service,
                      result->banner[0] ? " (Banner: " : "", result->banner[0] ? result->banner : "");
    }

    return NULL;
}

// Perform advanced port scanning
int na_port_scan(NAScanConfig* config, NAPortResult* results, size_t* result_count) {
    if (!config || !results || !result_count || !config->report || config->port_end < config->port_start) {
        na_report_add(config ? config->report : NULL, NA_SEV_CRITICAL, "Invalid scan configuration");
        return -1;
    }

    size_t port_count = config->port_end - config->port_start + 1;
    if (port_count > MAX_PORTS) {
        na_report_add(config->report, NA_SEV_CRITICAL, "Port range too large: %u-%u exceeds MAX_PORTS (%u)",
                      config->port_start, config->port_end, MAX_PORTS);
        return -1;
    }

    *result_count = 0;
    pthread_t threads[MAX_THREADS];
    NAPortScanArg args[MAX_PORTS];
    size_t thread_count = 0;

    for (uint16_t port = config->port_start; port <= config->port_end; port++) {
        if (*result_count >= MAX_PORTS) {
            na_report_add(config->report, NA_SEV_CRITICAL, "Result count exceeds MAX_PORTS (%u)", MAX_PORTS);
            break;
        }

        args[*result_count].config = config;
        args[*result_count].port = port;
        args[*result_count].result = &results[*result_count];

        if (thread_count < (size_t)config->max_threads) {
            if (pthread_create(&threads[thread_count], NULL, scan_port_worker, &args[*result_count]) != 0) {
                na_report_add(config->report, NA_SEV_WARNING, "Failed to create thread for port %u: %s", port, strerror(errno));
                continue;
            }
            thread_count++;
            (*result_count)++;
        } else {
            for (size_t i = 0; i < thread_count; i++) {
                pthread_join(threads[i], NULL);
            }
            thread_count = 0;
            usleep(RATE_LIMIT_MS * 1000);
            port--;
        }
    }

    for (size_t i = 0; i < thread_count; i++) {
        pthread_join(threads[i], NULL);
    }

    return 0;
}

// Free OpenSSL resources
void na_cleanup_openssl(void) {
#if OPENSSL_VERSION_NUMBER < 0x10100000L
    ERR_free_strings();
    EVP_cleanup();
#elif OPENSSL_VERSION_NUMBER >= 0x30000000L
    OPENSSL_cleanup();
#endif
}