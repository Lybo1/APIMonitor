/**
 * @file mac_scanner.h
 * @brief Public interface for the endpoint scanning library.
 * @note This header file provides thread-safe, cross-platform MAC address scanning via API endpoints.
 * @warning Ensure all input URLs are HTTPS and CA certificates are valid to prevent security risks.
 * @ingroup mac_scanner
 */

#ifdef MAC_SCANNER_H
#define MAC_SCANNER_H

#include <stdint.h>
#include <stddef.h>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

