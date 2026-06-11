cmake_minimum_required(VERSION 3.16)
project(mzSpecials VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(PkgConfig REQUIRED)
pkg_check_modules(GTKMM REQUIRED gtkmm-3.0)
pkg_check_modules(RSVG REQUIRED librsvg-2.0)
pkg_check_modules(FONTCONFIG REQUIRED fontconfig)
find_package(Threads REQUIRED)

add_executable(mzSpecials src/main.cpp)

target_include_directories(mzSpecials PRIVATE
    ${GTKMM_INCLUDE_DIRS}
    ${RSVG_INCLUDE_DIRS}
    ${FONTCONFIG_INCLUDE_DIRS}
    ${CMAKE_SOURCE_DIR}/include)
target_link_libraries(mzSpecials PRIVATE
    ${GTKMM_LIBRARIES}
    ${RSVG_LIBRARIES}
    ${FONTCONFIG_LIBRARIES}
    Threads::Threads)
target_compile_options(mzSpecials PRIVATE ${GTKMM_CFLAGS_OTHER} ${RSVG_CFLAGS_OTHER} ${FONTCONFIG_CFLAGS_OTHER})

# Copy assets next to the executable
file(COPY ${CMAKE_SOURCE_DIR}/assets DESTINATION ${CMAKE_BINARY_DIR})
