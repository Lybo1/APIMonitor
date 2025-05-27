#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <netdb.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include "../../include/helpers/geoip.h"

// --- Simplified JSON extraction (does not require a full parser) ---
static int extract_json_field(const char *json, const char *key, char *out, size_t max_out) {
    char find[64];
    snprintf(find, sizeof(find), "\"%s\":\"", key);
    char *start = strstr(json, find);
    if (!start) return 0;
    start += strlen(find);
    char *end = strchr(start, '"');
    if (!end) return 0;
    size_t len = (size_t)(end - start);
    if (len >= max_out) len = max_out - 1;
    memcpy(out, start, len);
    out[len] = 0;
    return 1;
}

// --- GET request to ip-api.com for the given ip ---
static int fetch_geoip_json(const char *ip, char *buf, size_t max) {
    char host[] = "ip-api.com";
    char getreq[128];
    snprintf(getreq, sizeof(getreq),
             "GET /json/%s?fields=country,city,as,query,status,message HTTP/1.0\r\nHost: ip-api.com\r\n\r\n", ip);

    struct hostent *he = gethostbyname(host);
    if (!he) { fprintf(stderr, "geoip: DNS failure\n"); return 0; }

    int sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock < 0) { perror("socket"); return 0; }

    struct sockaddr_in addr;
    addr.sin_family = AF_INET;
    addr.sin_port = htons(80);
    addr.sin_addr = *((struct in_addr *)he->h_addr);

    if (connect(sock, (struct sockaddr*)&addr, sizeof(addr)) < 0) {
        perror("connect"); close(sock); return 0;
    }

    if (write(sock, getreq, strlen(getreq)) < 0) {
        perror("write"); close(sock); return 0;
    }

    ssize_t n, off = 0;
    while ((n = read(sock, buf + off, max - off - 1)) > 0) {
        off += n;
        if (off >= (ssize_t)max - 1) break;
    }
    buf[off] = 0;
    close(sock);

    // skip HTTP headers
    char *body = strstr(buf, "\r\n\r\n");
    if (!body) return 0;
    memmove(buf, body + 4, strlen(body + 4) + 1);
    return 1;
}

// --- Main user-callable function ---
int geoip_lookup(const char *ip, char *country, char *city, char *asn, size_t maxfield) {
    char buf[4096];
    if (!fetch_geoip_json(ip, buf, sizeof(buf))) return 0;
    char status[24], message[128];
    if (!extract_json_field(buf, "status", status, sizeof(status)) || strcmp(status, "success") != 0) {
        extract_json_field(buf, "message", message, sizeof(message));
        fprintf(stderr, "geoip: lookup error: %s\n", message[0] ? message : "unknown failure");
        return 0;
    }
    extract_json_field(buf, "country", country, maxfield);
    extract_json_field(buf, "city", city, maxfield);
    extract_json_field(buf, "as", asn, maxfield);
    return 1;
}

// --- Pretty output ---
void geoip_print_box(const char *ip, const char *country, const char *city, const char *asn) {
    printf(
        "+-----------------+-------------------------------+\n"
        "| %-15s | %-29s |\n", "Field", "Value");
    printf("+-----------------+-------------------------------+\n");
    printf("| %-15s | %-29s |\n", "IP Address", ip);
    printf("| %-15s | %-29s |\n", "Country", country && *country ? country : "(n/a)");
    printf("| %-15s | %-29s |\n", "City", city && *city ? city : "(n/a)");
    printf("| %-15s | %-29s |\n", "ASN", asn && *asn ? asn : "(n/a)");
    printf("+-----------------+-------------------------------+\n");
}