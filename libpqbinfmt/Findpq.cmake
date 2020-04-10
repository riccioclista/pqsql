find_package(PkgConfig)
pkg_check_modules(PC_pq QUIET libpq)

find_path(pq_INCLUDE_DIR
    NAMES libpq-fe.h
    PATHS ${PC_pq_INCLUDE_DIRS}
)

set(pq_LIBRARY ${PC_pq_LINK_LIBRARIES})

set(pq_VERSION ${PC_pq_VERSION})

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(pq
    REQUIRED_VARS pq_INCLUDE_DIR pq_LIBRARY
    VERSION_VAR pq_VERSION
)

mark_as_advanced(pq_FOUND pq_INCLUDE_DIR pq_VERSION, pq_LIBRARY)

if(pq_FOUND)
    set(pq_INCLUDE_DIRS ${pq_INCLUDE_DIR})
    set(pq_LIBRARIES ${pq_LIBRARY})
endif()

if(pq_FOUND AND NOT TARGET pq::pq)
    add_library(pq INTERFACE IMPORTED)
    set_target_properties(pq
        PROPERTIES
            INTERFACE_INCLUDE_DIRECTORIES "${pq_INCLUDE_DIR}"
            INTERFACE_LINK_LIBRARIES "${pq_LIBRARY}"
    )
endif()
