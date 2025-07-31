cmake_minimum_required(VERSION 3.22)
project(server C)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_FLAGS "-Wall -Wextra -pthread")

find_package(CURL REQUIRED)
find_package(OpenSSL REQUIRED)

add_executable(server
        src/main.c
        src/scanner/http_headers_analyzer.c
        src/helpers/grading.c
        src/scanner/network_analyzer.c
)

target_include_directories(server PRIVATE
        ${CURL_INCLUDE_DIRS}
)

target_link_libraries(server PRIVATE
        ${CURL_LIBRARIES}
        cjson
        pthread
        OpenSSL::SSL
        OpenSSL::Crypto
)