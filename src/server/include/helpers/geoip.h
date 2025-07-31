#ifndef GEOIP_H
#define GEOIP_H

int geoip_lookup(const char *ip, char *country, char *city, char *asn, size_t maxfield);
void geoip_print_box(const char *ip, const char *country, const char *city, const char *asn);

#endif