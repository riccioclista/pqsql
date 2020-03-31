cmake_minimum_required(VERSION 3.8)

project(pqbinfmt VERSION 9.6.17.0)

configure_file(pqbinfmt_config.h.in pqbinfmt_config.h)

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
    configure_file(libpqbinfmt.rc.in libpqbinfmt.rc)
    list(APPEND SOURCES "${PROJECT_BINARY_DIR}/libpqbinfmt.rc")
endif()

add_library(pqbinfmt SHARED ${SOURCES})

list(APPEND EXTRA_INCLUDES "/opt/pgsql/9.6.17/bin/include")

target_include_directories(pqbinfmt PUBLIC
                           "${PROJECT_BINARY_DIR}"
                           ${PROJECT_SOURCE_DIR}
                           ${EXTRA_INCLUDES}
                           )

add_definitions(-DFRONTEND)

list(APPEND EXTRA_LIBS "/opt/pgsql/9.6.17/bin/lib/libpq.so")

target_link_libraries(pqbinfmt PUBLIC
                      ${EXTRA_LIBS}
                      )

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
