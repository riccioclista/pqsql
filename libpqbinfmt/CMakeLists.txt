cmake_minimum_required(VERSION 3.8)

project(pqbinfmt)

list(APPEND SOURCES
    common/fe_memutils.c
    fe_utils/psqlscan.c
    pgadt/datetime.c
    pgadt/float.c
    pgadt/numeric.c
    pqbincopy.c
    pqbinfmt.c
    pqbinfmt_array.c
    pqparam_buffer.c
    pqparse.c
)

if (WIN32)
    list(APPEND SOURCES "${PROJECT_BINARY_DIR}/libpqbinfmt.rc")
    set(CMAKE_SHARED_LIBRARY_PREFIX "lib")
    set(CMAKE_STATIC_LIBRARY_PREFIX "lib")
    set(CMAKE_IMPORT_LIBRARY_PREFIX "lib")
endif()

add_library(pqbinfmt SHARED ${SOURCES})

list(APPEND CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}")
find_package(pq REQUIRED MODULE)

set(pqbinfmt_VERSION ${pq_VERSION})

if (${pq_VERSION} MATCHES "^([0-9]+)(\\.[0-9]+)*")
    set(pqbinfmt_VERSION_MAJOR "${CMAKE_MATCH_1}")
else()
    set(pqbinfmt_VERSION_MAJOR 0)
endif()

if (${pq_VERSION} MATCHES "^[0-9]+\\.([0-9]+)(\\.[0-9]+)*")
    set(pqbinfmt_VERSION_MINOR "${CMAKE_MATCH_1}")
else()
    set(pqbinfmt_VERSION_MINOR 0)
endif()

if (${pq_VERSION} MATCHES "^[0-9]+\\.[0-9]+\\.([0-9]+)(\\.[0-9]+)*")
    set(pqbinfmt_VERSION_PATCH "${CMAKE_MATCH_1}")
else()
    set(pqbinfmt_VERSION_PATCH 0)
endif()

if (${pq_VERSION} MATCHES "^[0-9]+\\.[0-9]+\\.[0-9]+\\.([0-9]+)")
    set(pqbinfmt_VERSION_TWEAK "${CMAKE_MATCH_1}")
else()
    set(pqbinfmt_VERSION_TWEAK 0)
endif()

message(STATUS "Setting pqbinfmt version: ${pqbinfmt_VERSION}")

configure_file(pqbinfmt_config.h.in pqbinfmt_config.h)

if (WIN32)
    configure_file(libpqbinfmt.rc.in libpqbinfmt.rc)
endif()

target_include_directories(pqbinfmt
    PUBLIC
        "${PROJECT_BINARY_DIR}" "${PROJECT_SOURCE_DIR}"
)

add_definitions(-DFRONTEND)

target_link_libraries(pqbinfmt
    PUBLIC
        pq
)

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
