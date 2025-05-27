#include "../include//mac_scanner.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <time.h>
#include <pthread.h>
#include <syslog.h>
#include <sys/prctl.h>
#include <pwd.h>
#include <signal.h>
#include <curl/curl.h>
#include <cjson/cJSON.h>
#include <stdatomic.h>
#include <sys/resource.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#else
#include <sys/mman.h>
#include <fcntl.h>
#include <sys/stat.h>
#endif

#define RING_BUFFER_SIZE (1 << 16)
#define MAX_RESPONSE_SIZE 8192
#define MAX_THREAD_RETRIES 3
#define CONNECTION_TIMEOUT_MS 10000
#define SSL_CIPHER_LIST "HIGH:!aNULL:!MD5:!RC4"
#define DEFAULT_POLL_INTERVAL_MS 1000
#define MAX_POLL_INTERVAL_MS 60000
#define MIN_POLL_INTERVAL_MS 100
#define MAC_SCANNER_VERSION "1.0.1"

#define THREAD_LOCAL __thread

#define LOG_LEVEL_DEBUG 0
#define LOG_LEVEL_INFO  1
#define LOG_LEVEL_WARN  2
#define LOG_LEVEL_ERROR 3

#define SAFE_FREE(ptr) do { if (ptr) { free(ptr); ptr = NULL; } } while(0)

static void cleanup_thread_resources(void* arg);

static atomic_int log_level = LOG_LEVEL_INFO;
static atomic_int use_syslog = 0;
static atomic_int sig_atomic_t running = 1;

void init_logger(int level, int syslog_enabled) {
    atomic_store(&log_level, level);
    atomic_store(&use_syslog, syslog_enabled);

    if (syslog_enabled) {
        openlog("mac_scanner", LOG_PID | LOG_NDELAY, LOG_USER);
        setlogmask(LOG_UPTO(LOG_DEBUG));
    }
}

void log_message(int level, const char* fmt, ...) {
    if (level < atomic_load(&log_level)) {
        return;
    }

    va_list args;
    va_start(args, fmt);

    if (atomic_load(&use_syslog)) {
        int syslog_level = level == LOG_LEVEL_ERROR ? LOG_ERR :
                          level == LOG_LEVEL_WARN ? LOG_WARNING :
                          level == LOG_LEVEL_INFO ? LOG_INFO : LOG_DEBUG;

        vsyslog(syslog_level, fmt, args);
    } else {
        time_t now = time(NULL);
        struct tm* tm_info = localtime(&now);
        char timestamp[26];
        strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S", tm_info);

        char buf[512];
        vsnprintf(buf, sizeof(buf), fmt, args);

        fprintf(stderr, "[%s] [%s] %s\n",
                timestamp,
                level == LOG_LEVEL_ERROR ? "ERROR" :
                level == LOG_LEVEL_WARN ? "WARN" :
                level == LOG_LEVEL_INFO ? "INFO" : "DEBUG",
                buf);
    }

    va_end(args);
}

static void handle_signal(int sig) {
    if (sig == SIGINT || sig == SIGTERM) {
        atomic_store(&running, 0);
        log_message(LOG_LEVEL_INFO, "Received signal %d, shutting down...", sig);
    }
}

typedef struct {
    mac_pair_t* buffer;
    _Atomic size_t head, tail;
    size_t size;
    pthread_mutex_t mutex;
    pthread_cond_t not_empty;
    pthread_cond_t not_full;
    _Atomic int running;
} ring_buffer_t;

struct mac_scanner {
    CURLM* curl_multi;
    pthread_t* polling_threads;
    size_t url_count;
    char** api_urls;  // Owned copies, not just pointers
    ring_buffer_t ring_buffer;
    _Atomic int active;
    pthread_mutex_t mutex;
    pthread_rwlock_t status_lock;
    mac_scanner_status_t status;
    int poll_interval_ms;
    char* ca_cert_path;  // Owned copy
    _Atomic int shutdown_requested;
    _Atomic uint64_t last_error_time;
    _Atomic size_t active_thread_count;
    char* version;
};

// HTTP response buffer with improved memory safety
typedef struct {
    char* data;
    size_t size;
    size_t capacity;
} response_t;

// Thread context with retry capability
typedef struct {
    mac_scanner_t* scanner;
    size_t index;
    int retries;
    pthread_t thread_id;
    _Atomic int active;
    char* url;
} thread_context_t;

// Utility functions
static inline void mac_to_int(const uint8_t* mac, mac_addr_t* addr) {
    if (!mac || !addr) return;

    addr->mac_high = ((uint32_t)mac[0] << 24) |
                     ((uint32_t)mac[1] << 16) |
                     ((uint32_t)mac[2] << 8) |
                      (uint32_t)mac[3];
    addr->mac_low = ((uint16_t)mac[4] << 8) | (uint16_t)mac[5];
}

EXPORT void int_to_mac(const mac_addr_t* addr, uint8_t* mac) {
    if (!addr || !mac) return;

    mac[0] = (addr->mac_high >> 24) & 0xFF;
    mac[1] = (addr->mac_high >> 16) & 0xFF;
    mac[2] = (addr->mac_high >> 8) & 0xFF;
    mac[3] = addr->mac_high & 0xFF;
    mac[4] = (addr->mac_low >> 8) & 0xFF;
    mac[5] = addr->mac_low & 0xFF;
}

// Safe string copy with bounds checking
static void safe_strncpy(char* dst, const char* src, size_t dst_size) {
    if (!dst || !src || dst_size == 0) return;

    size_t i;
    for (i = 0; i < dst_size - 1 && src[i] != '\0'; i++) {
        dst[i] = src[i];
    }
    dst[i] = '\0';
}

// Secure privilege dropping with verification
static int drop_privileges(const char* username, char* err_buf, size_t err_buf_size) {
    if (!username || !err_buf) {
        return -1;
    }

    // Get user information
    struct passwd* pw = getpwnam(username);
    if (!pw) {
        snprintf(err_buf, err_buf_size, "User %s not found", username);
        return -1;
    }

    // Drop supplementary groups
    if (setgroups(0, NULL) < 0) {
        snprintf(err_buf, err_buf_size, "Failed to clear supplementary groups: %s", strerror(errno));
        return -1;
    }

    // Set GID first (required order)
    if (setgid(pw->pw_gid) < 0) {
        snprintf(err_buf, err_buf_size, "Failed to set GID: %s", strerror(errno));
        return -1;
    }

    // Set UID
    if (setuid(pw->pw_uid) < 0) {
        snprintf(err_buf, err_buf_size, "Failed to set UID: %s", strerror(errno));
        return -1;
    }

    // Verify privileges were dropped
    if (getuid() != pw->pw_uid || geteuid() != pw->pw_uid ||
        getgid() != pw->pw_gid || getegid() != pw->pw_gid) {
        snprintf(err_buf, err_buf_size, "Failed to verify privilege drop");
        return -1;
    }

    // Prevent privilege escalation
    prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0);

    // Set resource limits
    struct rlimit rlim;
    rlim.rlim_cur = rlim.rlim_max = 0;
    setrlimit(RLIMIT_CORE, &rlim);  // Disable core dumps

    return 0;
}

// Secure memory allocation with sanitization
static void* secure_malloc(size_t size) {
    if (size == 0) return NULL;

    void* ptr = malloc(size);
    if (ptr) {
        memset(ptr, 0, size);
    }
    return ptr;
}

// Secure string duplication
static char* secure_strdup(const char* str) {
    if (!str) return NULL;

    size_t len = strlen(str);
    char* dup = secure_malloc(len + 1);
    if (dup) {
        memcpy(dup, str, len);
        dup[len] = '\0';
    }
    return dup;
}

// Enhanced curl write callback with bounds checking
static size_t write_callback(void* contents, size_t size, size_t nmemb, void* userp) {
    size_t realsize = size * nmemb;
    response_t* resp = (response_t*)userp;

    // Check for overflow
    if (resp->size + realsize < resp->size) {
        log_message(LOG_LEVEL_ERROR, "Integer overflow in response size");
        return 0;
    }

    // Check if we need to expand capacity
    if (resp->size + realsize + 1 > resp->capacity) {
        size_t new_capacity = resp->capacity ? resp->capacity * 2 : MAX_RESPONSE_SIZE;
        while (new_capacity < resp->size + realsize + 1) {
            if (new_capacity > SIZE_MAX / 2) {
                log_message(LOG_LEVEL_ERROR, "Response too large");
                return 0;
            }
            new_capacity *= 2;
        }

        char* ptr = realloc(resp->data, new_capacity);
        if (!ptr) {
            log_message(LOG_LEVEL_ERROR, "Memory allocation failed for response buffer");
            return 0;
        }
        resp->data = ptr;
        resp->capacity = new_capacity;
    }

    memcpy(&(resp->data[resp->size]), contents, realsize);
    resp->size += realsize;
    resp->data[resp->size] = 0;
    return realsize;
}

// Initialize ring buffer with proper error handling
static int init_ring_buffer(ring_buffer_t* rb, size_t size) {
    if (!rb || size == 0 || (size & (size - 1)) != 0) {
        log_message(LOG_LEVEL_ERROR, "Invalid ring buffer size: %zu", size);
        return -1;
    }

#ifdef _WIN32
    rb->buffer = VirtualAlloc(NULL, size * sizeof(mac_pair_t),
                            MEM_COMMIT | MEM_RESERVE,
                            PAGE_READWRITE);
    if (!rb->buffer) {
        log_message(LOG_LEVEL_ERROR, "VirtualAlloc failed: %lu", GetLastError());
        return -1;
    }
#else
    rb->buffer = mmap(NULL, size * sizeof(mac_pair_t),
                     PROT_READ | PROT_WRITE,
                     MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (rb->buffer == MAP_FAILED) {
        log_message(LOG_LEVEL_ERROR, "mmap failed: %s", strerror(errno));
        return -1;
    }

    // Lock memory to prevent paging to swap
    if (mlock(rb->buffer, size * sizeof(mac_pair_t)) != 0) {
        log_message(LOG_LEVEL_WARN, "Could not lock memory: %s", strerror(errno));
    }
#endif

    rb->size = size;
    atomic_store(&rb->head, 0);
    atomic_store(&rb->tail, 0);
    atomic_store(&rb->running, 0);

    pthread_mutexattr_t mutex_attr;
    pthread_mutexattr_init(&mutex_attr);
    pthread_mutexattr_settype(&mutex_attr, PTHREAD_MUTEX_RECURSIVE);
    pthread_mutex_init(&rb->mutex, &mutex_attr);
    pthread_mutexattr_destroy(&mutex_attr);

    pthread_condattr_t cond_attr;
    pthread_condattr_init(&cond_attr);
    pthread_cond_init(&rb->not_empty, &cond_attr);
    pthread_cond_init(&rb->not_full, &cond_attr);
    pthread_condattr_destroy(&cond_attr);

    return 0;
}

// Push to ring buffer with improved thread safety
static int push_ring_buffer(ring_buffer_t* rb, const mac_pair_t* mac, mac_scanner_t* scanner) {
    if (!rb || !mac || !scanner) {
        return -1;
    }

    int result = -1;
    pthread_mutex_lock(&rb->mutex);

    // Wait for space if buffer is full
    size_t next_head = (atomic_load(&rb->head) + 1) & (rb->size - 1);
    while (next_head == atomic_load(&rb->tail) && atomic_load(&rb->running)) {
        struct timespec timeout;
        clock_gettime(CLOCK_REALTIME, &timeout);
        timeout.tv_sec += 1; // 1 second timeout

        int rc = pthread_cond_timedwait(&rb->not_full, &rb->mutex, &timeout);
        if (rc == ETIMEDOUT) {
            pthread_rwlock_wrlock(&scanner->status_lock);
            scanner->status.buffer_full_count++;
            pthread_rwlock_unlock(&scanner->status_lock);
            break;
        }

        next_head = (atomic_load(&rb->head) + 1) & (rb->size - 1);
    }

    if (next_head != atomic_load(&rb->tail) || !atomic_load(&rb->running)) {
        // Copy data with memory barrier for thread safety
        memcpy(&rb->buffer[atomic_load(&rb->head)], mac, sizeof(mac_pair_t));
        atomic_store(&rb->head, next_head);

        pthread_rwlock_wrlock(&scanner->status_lock);
        scanner->status.packets_processed++;
        scanner->status.buffer_fill = (atomic_load(&rb->head) >= atomic_load(&rb->tail)) ?
                                     atomic_load(&rb->head) - atomic_load(&rb->tail) :
                                     rb->size - atomic_load(&rb->tail) + atomic_load(&rb->head);
        pthread_rwlock_unlock(&scanner->status_lock);

        pthread_cond_signal(&rb->not_empty);
        result = 0;
    } else {
        pthread_rwlock_wrlock(&scanner->status_lock);
        scanner->status.requests_failed++;
        pthread_rwlock_unlock(&scanner->status_lock);
    }

    pthread_mutex_unlock(&rb->mutex);
    return result;
}

// Pop from ring buffer with timeout support
static int pop_ring_buffer(ring_buffer_t* rb, mac_pair_t* mac, int timeout_ms) {
    if (!rb || !mac) {
        return -1;
    }

    int result = -1;
    pthread_mutex_lock(&rb->mutex);

    // Wait for data if buffer is empty
    if (atomic_load(&rb->head) == atomic_load(&rb->tail) && atomic_load(&rb->running)) {
        if (timeout_ms > 0) {
            struct timespec timeout;
            clock_gettime(CLOCK_REALTIME, &timeout);
            timeout.tv_sec += timeout_ms / 1000;
            timeout.tv_nsec += (timeout_ms % 1000) * 1000000;
            if (timeout.tv_nsec >= 1000000000) {
                timeout.tv_sec++;
                timeout.tv_nsec -= 1000000000;
            }

            while (atomic_load(&rb->head) == atomic_load(&rb->tail) && atomic_load(&rb->running)) {
                int rc = pthread_cond_timedwait(&rb->not_empty, &rb->mutex, &timeout);
                if (rc == ETIMEDOUT) {
                    break;
                }
            }
        }
    }

    if (atomic_load(&rb->head) != atomic_load(&rb->tail)) {
        // Copy data with memory barrier for thread safety
        memcpy(mac, &rb->buffer[atomic_load(&rb->tail)], sizeof(mac_pair_t));
        atomic_store(&rb->tail, (atomic_load(&rb->tail) + 1) & (rb->size - 1));

        pthread_cond_signal(&rb->not_full);
        result = 0;
    }

    pthread_mutex_unlock(&rb->mutex);
    return result;
}

// Parse MAC address with validation
static int parse_mac(const char* mac_str, mac_addr_t* addr) {
    if (!mac_str || !addr) {
        return -1;
    }

    // Validate MAC format with strict pattern checking
    unsigned int values[6] = {0};
    int items = sscanf(mac_str, "%x:%x:%x:%x:%x:%x",
                      &values[0], &values[1], &values[2],
                      &values[3], &values[4], &values[5]);

    if (items != 6) {
        return -1;
    }

    // Validate each byte is in range
    for (int i = 0; i < 6; i++) {
        if (values[i] > 0xFF) {
            return -1;
        }
    }

    // Convert to binary format
    uint8_t mac[6];
    for (int i = 0; i < 6; i++) {
        mac[i] = (uint8_t)values[i];
    }

    mac_to_int(mac, addr);
    return 0;
}

// Validate IP address format
static int validate_ip(const char* ip_str) {
    if (!ip_str) return 0;

    // Simple validation of IPv4 and IPv6
    struct sockaddr_in sa4;
    struct sockaddr_in6 sa6;

    if (inet_pton(AF_INET, ip_str, &(sa4.sin_addr)) == 1) {
        return 1; // Valid IPv4
    }

    if (inet_pton(AF_INET6, ip_str, &(sa6.sin6_addr)) == 1) {
        return 1; // Valid IPv6
    }

    return 0; // Invalid IP
}

// Configure curl handle securely
static void configure_curl_handle(CURL* curl, const char* url, response_t* response,
                                 const char* ca_cert_path, int timeout_ms) {
    if (!curl || !url || !response) return;

    curl_easy_setopt(curl, CURLOPT_URL, url);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, write_callback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, response);

    // Security settings
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1L);
    curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 2L);
    if (ca_cert_path && *ca_cert_path) {
        curl_easy_setopt(curl, CURLOPT_CAINFO, ca_cert_path);
    }
    curl_easy_setopt(curl, CURLOPT_SSL_CIPHER_LIST, SSL_CIPHER_LIST);
    curl_easy_setopt(curl, CURLOPT_PROTOCOLS, CURLPROTO_HTTPS | CURLPROTO_HTTP);
    curl_easy_setopt(curl, CURLOPT_REDIR_PROTOCOLS, CURLPROTO_HTTPS);
    curl_easy_setopt(curl, CURLOPT_MAXREDIRS, 5L);

    // Connection settings
    curl_easy_setopt(curl, CURLOPT_CONNECTTIMEOUT_MS, CONNECTION_TIMEOUT_MS);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT_MS, timeout_ms);
    curl_easy_setopt(curl, CURLOPT_TCP_KEEPALIVE, 1L);
    curl_easy_setopt(curl, CURLOPT_FORBID_REUSE, 0L);
    curl_easy_setopt(curl, CURLOPT_NOSIGNAL, 1L);

    // Add user agent
    curl_easy_setopt(curl, CURLOPT_USERAGENT, "MAC-Scanner/" MAC_SCANNER_VERSION);

    // Error buffer
    THREAD_LOCAL char error_buffer[CURL_ERROR_SIZE];
    curl_easy_setopt(curl, CURLOPT_ERRORBUFFER, error_buffer);
}

// Process JSON packet safely
static void process_json_packet(cJSON* packet, mac_scanner_t* scanner) {
    if (!packet || !scanner) return;

    cJSON* src_mac = cJSON_GetObjectItem(packet, "src_mac");
    cJSON* dst_mac = cJSON_GetObjectItem(packet, "dst_mac");
    cJSON* src_ip = cJSON_GetObjectItem(packet, "src_ip");
    cJSON* dst_ip = cJSON_GetObjectItem(packet, "dst_ip");
    cJSON* timestamp = cJSON_GetObjectItem(packet, "timestamp");

    // Validate required fields
    if (!cJSON_IsString(src_mac) || !cJSON_IsString(dst_mac)) {
        pthread_rwlock_wrlock(&scanner->status_lock);
        scanner->status.error_count++;
        pthread_rwlock_unlock(&scanner->status_lock);
        return;
    }

    mac_pair_t pair = {0};
    if (parse_mac(src_mac->valuestring, &pair.src_mac) < 0 ||
        parse_mac(dst_mac->valuestring, &pair.dst_mac) < 0) {
        pthread_rwlock_wrlock(&scanner->status_lock);
        scanner->status.error_count++;
        pthread_rwlock_unlock(&scanner->status_lock);
        return;
    }

    // Get timestamp (prefer provided, fall back to current time)
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    pair.timestamp_ns = timestamp && cJSON_IsNumber(timestamp) ?
                       (uint64_t)(timestamp->valuedouble * 1000000000.0) :
                       ts.tv_sec * 1000000000ULL + ts.tv_nsec;

    // Copy IP addresses if provided and valid
    if (src_ip && cJSON_IsString(src_ip) && validate_ip(src_ip->valuestring)) {
        safe_strncpy(pair.src_ip, src_ip->valuestring, sizeof(pair.src_ip));
    }

    if (dst_ip && cJSON_IsString(dst_ip) && validate_ip(dst_ip->valuestring)) {
        safe_strncpy(pair.dst_ip, dst_ip->valuestring, sizeof(pair.dst_ip));
    }

    // Add to ring buffer
    if (push_ring_buffer(&scanner->ring_buffer, &pair, scanner) != 0) {
        log_message(LOG_LEVEL_DEBUG, "Failed to push packet to ring buffer (possibly full)");
    }
}

// Thread cleanup handler
static void cleanup_thread_resources(void* arg) {
    thread_context_t* ctx = (thread_context_t*)arg;
    if (!ctx) return;

    log_message(LOG_LEVEL_DEBUG, "Thread %zu cleanup for URL %s",
               ctx->index, ctx->url ? ctx->url : "unknown");

    atomic_store(&ctx->active, 0);
    if (ctx->scanner) {
        atomic_fetch_sub(&ctx->scanner->active_thread_count, 1);
    }

    SAFE_FREE(ctx->url);
    SAFE_FREE(ctx);
}

// Thread watchdog to restart failed threads
static void* thread_watchdog(void* arg) {
    mac_scanner_t* scanner = (mac_scanner_t*)arg;
    if (!scanner) return NULL;

    while (atomic_load(&scanner->active) && atomic_load(&running)) {
        sleep(30); // Check every 30 seconds

        pthread_mutex_lock(&scanner->mutex);
        for (size_t i = 0; i < scanner->url_count; i++) {
            // Create new thread if needed for each URL
            if (scanner->polling_threads[i] == 0 ||
                pthread_kill(scanner->polling_threads[i], 0) != 0) {

                thread_context_t* ctx = secure_malloc(sizeof(*ctx));
                if (!ctx) {
                    log_message(LOG_LEVEL_ERROR, "Thread context allocation failed in watchdog");
                    continue;
                }

                ctx->scanner = scanner;
                ctx->index = i;
                ctx->retries = 0;
                ctx->url = secure_strdup(scanner->api_urls[i]);
                atomic_store(&ctx->active, 0);

                log_message(LOG_LEVEL_INFO, "Watchdog restarting thread for URL %s", ctx->url);

                int ret = pthread_create(&scanner->polling_threads[i], NULL, poll_api_thread, ctx);
                if (ret != 0) {
                    log_message(LOG_LEVEL_ERROR, "Thread recreation failed: %s", strerror(ret));
                    SAFE_FREE(ctx->url);
                    SAFE_FREE(ctx);
                    scanner->status.error_count++;
                } else {
                    atomic_fetch_add(&scanner->active_thread_count, 1);
                }
            }
        }
        pthread_mutex_unlock(&scanner->mutex);
    }

    return NULL;
}

// Version API
EXPORT const char* mac_scanner_version(void) {
    return MAC_SCANNER_VERSION;
}

// Initialize scanner with improved validation
EXPORT mac_scanner_t* mac_scanner_init(const mac_scanner_config_t* config, char* err_buf, size_t err_buf_size) {
    // Setup signal handlers
    struct sigaction sa;
    memset(&sa, 0, sizeof(sa));
    sa.sa_handler = handle_signal;
    sigaction(SIGINT, &sa, NULL);
    sigaction(SIGTERM, &sa, NULL);
    sigaction(SIGPIPE, &sa, NULL); // Handle broken pipes gracefully

    // Initialize logger
    init_logger(config && config->log_level >= 0 ? config->log_level : LOG_LEVEL_INFO,
                config && config->use_syslog > 0);

    log_message(LOG_LEVEL_INFO, "MAC Scanner v%s initializing", MAC_SCANNER_VERSION);

    // Validate configuration
    if (!config) {
        safe_strncpy(err_buf, "NULL configuration provided", err_buf_size);
        return NULL;
    }

    if (!config->api_urls || config->url_count == 0) {
        safe_strncpy(err_buf, "No API URLs provided", err_buf_size);
        return NULL;
    }

    // Validate buffer size is power of two
    if (config->ring_buffer_size == 0 || (config->ring_buffer_size & (config->ring_buffer_size - 1)) != 0) {
        safe_strncpy(err_buf, "Ring buffer size must be a power of two", err_buf_size);
        return NULL;
    }

    // Validate CA certificate if provided
    if (config->ca_cert_path) {
        struct stat st;
        if (stat(config->ca_cert_path, &st) != 0 || !S_ISREG(st.st_mode)) {
            snprintf(err_buf, err_buf_size, "CA certificate file not found or not a regular file: %s",
                    config->ca_cert_path);
            return NULL;
        }
    }

    // Initialize curl
    curl_global_init(CURL_GLOBAL_ALL);

    // Allocate scanner
    mac_scanner_t* scanner = secure_malloc(sizeof(mac_scanner_t));
    if (!scanner) {
        safe_strncpy(err_buf, "Memory allocation failed for scanner", err_buf_size);
        curl_global_cleanup();
        return NULL;
    }

    // Initialize mutexes
    pthread_mutex_init(&scanner->mutex, NULL);
    pthread_rwlock_init(&scanner->status_lock, NULL);

    // Copy configuration data
    scanner->url_count = config->url_count;
    scanner->poll_interval_ms = config->poll_interval_ms > 0 ?
                               config->poll_interval_ms : DEFAULT_POLL_INTERVAL_MS;

    // Clamp poll interval to reasonable values
    if (scanner->poll_interval_ms < MIN_POLL_INTERVAL_MS) scanner->poll_interval_ms = MIN_POLL_INTERVAL_MS;
    if (scanner->poll_interval_ms > MAX_POLL_INTERVAL_MS) scanner->poll_interval_ms = MAX_POLL_INTERVAL_MS;

    // Copy CA cert path
    scanner->ca_cert_path = config->ca_cert_path ? secure_strdup(config->ca_cert_path) : NULL;

    // Setup version info
    scanner->version = secure_strdup(MAC_SCANNER_VERSION);

    // Copy URLs
    scanner->api_urls = secure_malloc(config->url_count * sizeof(char*));
    if (!scanner->api_urls) {
        safe_strncpy(err_buf, "Memory allocation failed for URL array", err_buf_size);
        mac_scanner_free(scanner);
        return NULL;
    }

    for (size_t i = 0; i < config->url_count; i++) {
        if (!config->api_urls[i]) {
            snprintf(err_buf, err_buf_size, "NULL URL at index %zu", i);
            mac_scanner_free(scanner);
            return NULL;
        }

        scanner->api_urls[i] = secure_strdup(config->api_urls[i]);
        if (!scanner->api_urls[i]) {
            snprintf(err_buf, err_buf_size, "Memory allocation failed for URL %zu", i);
            mac_scanner_free(scanner);
            return NULL;
        }
    }

    // Initialize thread tracking
    scanner->polling_threads = secure_malloc(config->url_count * sizeof(pthread_t));
    if (!scanner->polling_threads) {
        safe_strncpy(err_buf, "Memory allocation failed for thread array", err_buf_size);
        mac_scanner_free(scanner);
        return NULL;
    }

    // Initialize ring buffer
    size_t buffer_size = config->ring_buffer_size / sizeof(mac_pair_t);
    if (buffer_size < 64) buffer_size = 64; // Minimum buffer size

    if (init_ring_buffer(&scanner->ring_buffer, buffer_size) < 0) {
        safe_strncpy(err_buf, "Ring buffer initialization failed", err_buf_size);
        mac_scanner_free(scanner);
        return NULL;
    }

    // Initialize curl multi-handle
    scanner->curl_multi = curl_multi_init();
    if (!scanner->curl_multi) {
        safe_strncpy(err_buf, "CURL multi initialization failed", err_buf_size);
        mac_scanner_free(scanner);
        return NULL;
    }

    // Drop privileges if requested
    if (config->drop_privileges && config->username) {
        if (drop_privileges(config->username, err_buf, err_buf_size) < 0) {
            mac_scanner_free(scanner);
            return NULL;
        }
    }

    // Initialize state
    atomic_store(&scanner->active, 0);
    atomic_store(&scanner->shutdown_requested, 0);
    atomic_store(&scanner->active_thread_count, 0);
    atomic_store(&scanner->last_error_time, 0);

    log_message(LOG_LEVEL_INFO, "Scanner initialized for %zu URLs", config->url_count);
    return scanner;
}
static void* poll_api_thread(void* arg) {
    thread_context_t* ctx = (thread_context_t*)arg;
    if (!ctx || !ctx->scanner || !ctx->url) {
        log_message(LOG_LEVEL_ERROR, "Invalid thread context");
        return NULL;
    }

    // Setup thread cleanup handler
    pthread_cleanup_push(cleanup_thread_resources, ctx);

    // Thread initialization
    atomic_store(&ctx->active, 1);
    mac_scanner_t* scanner = ctx->scanner;
    const char* url = ctx->url;
    CURL* curl = curl_easy_init();
    response_t response = {0};
    struct timespec ts;
    int consecutive_errors = 0;
    uint64_t last_success_time = 0;

    log_message(LOG_LEVEL_INFO, "Started polling thread %zu for URL: %s", ctx->index, url);

    if (!curl) {
        log_message(LOG_LEVEL_ERROR, "CURL init failed for %s", url);
        pthread_rwlock_wrlock(&scanner->status_lock);
        scanner->status.error_count++;
        pthread_rwlock_unlock(&scanner->status_lock);
        pthread_exit(NULL);
    }

    // Exponential backoff parameters
    int backoff_ms = 100;
    int max_backoff_ms = scanner->poll_interval_ms * 10;

    // Main polling loop
    while (atomic_load(&scanner->active) && atomic_load(&running)) {
        // Initialize response buffer
        response.data = NULL;
        response.size = 0;
        response.capacity = 0;

        // Configure and perform request
        configure_curl_handle(curl, url, &response, scanner->ca_cert_path, scanner->poll_interval_ms);
        CURLcode res = curl_easy_perform(curl);

        // Check for success
        if (res == CURLE_OK) {
            // Reset backoff on success
            backoff_ms = 100;
            consecutive_errors = 0;
            clock_gettime(CLOCK_MONOTONIC, &ts);
            last_success_time = ts.tv_sec;

            // Process response if we have valid JSON
            if (response.data && response.size > 0) {
                cJSON* json = cJSON_Parse(response.data);
                if (json) {
                    // Handle both array and object responses
                    if (cJSON_IsArray(json)) {
                        cJSON* packet = NULL;
                        cJSON_ArrayForEach(packet, json) {
                            process_json_packet(packet, scanner);
                        }
                    } else if (cJSON_IsObject(json)) {
                        // Some APIs might return a single object
                        process_json_packet(json, scanner);
                    }

                    cJSON_Delete(json);
                } else {
                    log_message(LOG_LEVEL_WARN, "JSON parse failed for %s: %s",
                              url, cJSON_GetErrorPtr() ? cJSON_GetErrorPtr() : "Unknown error");
                    pthread_rwlock_wrlock(&scanner->status_lock);
                    scanner->status.error_count++;
                    pthread_rwlock_unlock(&scanner->status_lock);
                }
            }
        } else {
            // Handle error with exponential backoff
            log_message(LOG_LEVEL_WARN, "Request failed for %s: %s", url, curl_easy_strerror(res));
            pthread_rwlock_wrlock(&scanner->status_lock);
            scanner->status.requests_failed++;
            pthread_rwlock_unlock(&scanner->status_lock);

            consecutive_errors++;

            // Implement exponential backoff
            if (consecutive_errors > 3) {
                backoff_ms *= 2;
                if (backoff_ms > max_backoff_ms) {
                    backoff_ms = max_backoff_ms;
                }

                log_message(LOG_LEVEL_INFO, "Backing off for %d ms on URL %s after %d errors",
                          backoff_ms, url, consecutive_errors);

                // Sleep with backoff but check for termination
                for (int i = 0; i < backoff_ms / 100 && atomic_load(&scanner->active) && atomic_load(&running); i++) {
                    usleep(100 * 1000);
                }

                // Check for thread restart if errors persist
                clock_gettime(CLOCK_MONOTONIC, &ts);
                if (consecutive_errors > 10 && ts.tv_sec - last_success_time > 300) {
                    log_message(LOG_LEVEL_WARN, "Thread %zu for URL %s has had too many errors, restarting",
                              ctx->index, url);
                    break;
                }

                // Clean up and continue
                SAFE_FREE(response.data);
                continue;
            }
        }

        SAFE_FREE(response.data);

        // Sleep for poll interval with interruption support
        for (int i = 0; i < scanner->poll_interval_ms / 100 && atomic_load(&scanner->active) && atomic_load(&running); i++) {
            usleep(100 * 1000);
        }
    }

    // Cleanup
    curl_easy_cleanup(curl);
    SAFE_FREE(response.data);
    log_message(LOG_LEVEL_INFO, "Polling thread %zu for URL %s exiting", ctx->index, url);

    pthread_cleanup_pop(1); // Execute cleanup handler
    return NULL;
}
