#ifndef PLATFORM_OS_H
#define PLATFORM_OS_H

#include <stddef.h>
#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
* @file os.h
* @brief Cross-platform runtime system information interface.
*
* This header provides a unified interface for retrieving low-level system
* and environment information at runtime. The implementation is platform-specific,
* but the interface remains consistent across Linux, Windows, macOS, BSD, and others.
*
* Typical use cases:
* - Monitoring system health (e.g., load, uptime, memory).
* - Displaying runtime metadata in CLI/GUI tools.
* - Gathering diagnostics for remote endpoints.
*
* All functions return 0 on success, or a negative value on failure,
* unless otherwise specified.
*/

/* ----------------------------- Core OS Info ----------------------------- */

/**
* @brief Retrieves the operating system family name.
*
* Examples include "Linux", "Windows", "Macintosh", "FreeBSD".
*
* @param[out] buffer, pointer to a character array to hold the result.
* @param[in] size, maximum number of bytes to write into the buffer.
* @return 0 on success, -1 on failure (e.g., null buffer or detection error).
*/
int get_os_name(char *buffer, size_t size);

/**
* @brief Retrieves the OS kernel or build version.
*
* Examples: "6.5.0-rc3" (Linux), "Build 22621" (Windows), "23.4.0" (macOS).
*
* @param[out] buffer, output buffer for the version string.
* @param[in] size, buffer size in bytes.
* @return 0 on success, -1 on failure.
*/
int get_os_version(char *buffer, size_t size);

/**
* @brief Retrieves the Linux distribution name and version.
*
* Examples: "Debian GNU/Linux 12", "Arch Linux", "Ubuntu 22.04".
* Returns a human-readable description if available, or fails silently
* on non-Linux systems.
*
* @param[out] buffer, output buffer for the distro string.
* @param[in] size, buffer size in bytes.
* @return 0 on success (Linux only), -1 on failure or unsupported OS.
*/
int get_os_distro(char *buffer, size_t size);

/**
* @brief Provides a friendly OS string for user interfaces.
*
* Combines name, version, and distro (if available) into a concise summary.
* Intended for display in dashboards, CLI, or GUIs.
*
* Example: "Debian GNU/Linux 12 (Linux 6.1.0)"
*
* @param[out] buffer, output buffer.
* @param[in] size, buffer size in bytes.
* @return 0 on success, -1 on failure.
*/
int get_os_pretty(char *buffer, size_t size);

/* ----------------------------- CPU Info ----------------------------- */

/**
* @brief Retrieves the CPU architecture string.
*
* Examples: "x86_64", "arm64", "i386".
*
* @return pointer to a static string. Never NULL.
*/
const char* get_cpu_architecture(void);

/**
* @brief Returns the number of logical CPU cores (hyperthreads).
*
* Includes all available processor threads.
*
* @return Number of logical cores, or -1 on failure.
*/
int get_cpu_count_logical(void);

/**
* @brief Returns the number of physical CPU cores.
*
* Attempts to detect physical core count excluding hyperthreads.
* May fall back to logical count on platforms where detection is not possible.
*
* @return number of physical cores, or -1 on failure.
*/
int get_cpu_count_physical(void);

/* ----------------------------- Memory Info ----------------------------- */

/**
* @brief Retrieves the total physical memory available on the system.
*
* @return total memory in bytes, or 0 on failure.
*/
uint64_t get_total_memory_bytes(void);

/**
* @brief Retrieves the currently available (free/unused) physical memory.
*
* @return Free memory in bytes, or 0 on failure.
*/
uint64_t get_available_memory_bytes(void);

/* ----------------------------- Load / Usage ----------------------------- */

/**
* @brief Calculates the current CPU usage percentage (system-wide).
*
* The implementation uses sampling over a short window.
*
* @return CPU load as a float between 0.0 and 100.0, or -1.0 on failure.
*/
float get_cpu_load_percentage(void);

/**
* @brief Calculates the current memory usage as a percentage.
*
* @return memory usage as a float between 0.0 and 100.0, or -1.0 on failure.
*/
float get_memory_usage_percentage(void);

/* ----------------------------- Uptime ----------------------------- */

/**
* @brief Retrieves the time since the last system boot.
*
* @return Uptime in seconds, or 0 on failure.
*/
uint64_t get_uptime_seconds(void);

/* ----------------------------- Environment Flags ----------------------------- */

/**
* @brief Determines whether the current system is a virtual machine.
*
* Uses best-effort heuristics (e.g., BIOS info, CPU vendor strings, DMI).
*
* @return 1 if VM detected, 0 if physical machine, -1 if unknown.
*/
int is_virtual_machine(void);

/**
* @brief Detects whether the application is running inside a container.
*
* Checks for cgroup/container runtime hints.
*
* @return 1 if containerized, 0 if native host, -1 if unknown.
*/
int is_containerized(void);

/**
* @brief Checks whether the system is powered by battery.
*
* Intended for laptops or portable devices.
*
* @return 1 if battery-powered, 0 if not, -1 if unknown or unsupported.
*/
int is_battery_powered(void);

/**
* @brief Checks if the current user has administrative/root privileges.
*
* @return 1 if admin/root, 0 if regular user, -1 on error.
*/
int is_admin_user(void);

/* ----------------------------- Filesystem ----------------------------- */

/**
* @brief Retrieves the total disk space for a given filesystem path.
*
* @param[in] path A valid mount point or directory path.
* @return total disk space in bytes, or 0 on failure.
*/
uint64_t get_disk_total_space(const char* path);

/**
* @brief Retrieves the available free disk space for a given path.
*
* @param[in] path A valid mount point or directory path.
* @return free space in bytes, or 0 on failure.
*/
uint64_t get_disk_free_space(const char* path);

/* ----------------------------- Network ----------------------------- */

/**
* @brief Retrieves the hostname of the system.
*
* Example: "my-server.local"
*
* @param[out] buffer, destination buffer for the hostname.
* @param[in] size, buffer size in bytes.
* @return 0 on success, -1 on failure.
*/
int get_hostname(char *buffer, size_t size);

/**
* @brief Attempts to retrieve the primary outbound IP address.
*
* Best-effort based on outbound socket probing (non-connecting).
*
* @param[out] buffer Destination buffer for the IP address (IPv4/IPv6).
* @param[in] size Buffer size in bytes.
* @return 0 on success, -1 on failure.
*/
int get_primary_ip(char *buffer, size_t size);

#ifdef __cplusplus
}
#endif

#endif // PLATFORM_OS_H


