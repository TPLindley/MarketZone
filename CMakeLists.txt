cmake_minimum_required(VERSION 3.16)
project(szSpecials VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(PkgConfig REQUIRED)
pkg_check_modules(GTKMM REQUIRED gtkmm-3.0)

add_executable(szSpecials src/main.cpp)

target_include_directories(szSpecials PRIVATE ${GTKMM_INCLUDE_DIRS})
target_link_libraries(szSpecials PRIVATE ${GTKMM_LIBRARIES})
target_compile_options(szSpecials PRIVATE ${GTKMM_CFLAGS_OTHER})
