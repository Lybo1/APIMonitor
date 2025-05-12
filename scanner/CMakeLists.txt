cmake_minimum_required(VERSION 3.31)
project(scanner C)

set(CMAKE_C_STANDARD 23)

add_library(scanner STATIC library.c
)
