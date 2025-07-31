#include <"server/src/platform/os.h">

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/sysinfo.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <dirent.h>

const char* get_cpu_architecture(void) {
    #if defined(__x86_64__)
        return "x86_64";
    #elif defined(__aarch64__)
        return "ARM64";
    #elif defined(__i386__)
        return "x86";
    #elif defined(__arm__)
        return "ARM";
    #else
        return "Unknown";
    #endif
}

int get_cpu_count_logical(void) {
    return sysconf(__SC_NPROCESSORS_ONLN);
}

int get_cpu_count_physical(void) {
    FILE* f = fopen("/proc/cpuinfo", "r");

    if (!f) {
        return -1;
    }

    int physical[256] = {0};
    int core[256] = {0};
    int count = 0;
    char line[512];
    int phys_id = -1, core_id = -1;
}
