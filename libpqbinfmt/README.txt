Supported features
==================

- PQgetvalue(): decoding binary format to native datatype using pqbf_get_TYPE() functions (see supported datatypes below)

- PQexecParams(): send binary input parameter format using pqparam_buffer and pqbf_add_TYPE() functions

- frontend to PQputCopyData() and PQputCopyEnd() (COPY FROM STDIN BINARY) using pqcopy_buffer and pqbf_set_TYPE() functions

- arrays as parameter input and results in binary format using
	- decoding binary format: pqbf_get_array(), pqbf_get_array_value()
	- send binary input parameter: pqbf_add_array()
	- frontend to PQputCopyData() and PQputCopyEnd(): pqbf_set_array()
	- helper functions: pqbf_set_array_itemlength(), pqbf_update_array_itemlength(), pqbf_set_array_value() 

- statement parser based on psqlscan with parameter replacement (:p => $N)


Supported PostgreSQL datatypes
==============================

bool

bytea

"char"

text
varchar
varchar(N)
char(N)
unknown

int8
int4
int2
oid

float4
float8

numeric

time
timetz
timestamp
timestamptz
interval
date

and arrays of the above types as element type



Unsupported features
====================

- COPY TO STDOUT BINARY

- COPY FROM STDIN BINARY
  Flags field: Bit 16 (if 1, OIDs are included in the data; if 0, not)

- numeric can be accessed only as double currently


Unsupported datatypes
=====================

bit					src/backend/utils/adt/varbit.c
cidr				src/backend/utils/adt/network.c
inet				src/backend/utils/adt/network.c
macaddr				src/backend/utils/adt/mac.c
uuid				src/backend/utils/adt/uuid.c
xid					src/backend/utils/adt/xid.c
...


TODO
====

- add basic unit tests


Incorporated Postgresql code
============================

The following list of files have been adapted by removing unecessary code parts. In parenthesis is the latest commit id.

The following files stem from postgresql/REL_10_16 tag: 

src/backend/utils/adt/datetime.c
src/backend/utils/adt/float.c
src/backend/utils/adt/numeric.c

src/include/c.h
src/include/fmgr.h
src/include/postgres.h

src/include/utils/builtins.h
src/include/utils/datetime.h
src/include/utils/numeric.h
src/include/datatype/timestamp.h

src/interfaces/ecpg/ecpglib/pg_type.h
src/interfaces/libpq/pqexpbuffer.h

src/include/postgres_fe.h

src/common/fe_memutils.c
src/include/common/fe_memutils.h


The following files stem from postgresql/master branch:

src/fe_utils/psqlscan.l (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)
src/fe_utils/psqlscan.c (generated from psqlscan.l)

src/include/fe_utils/psqlscan.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)
src/include/fe_utils/psqlscan_int.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)


The following files stem from the postgresql 10.16-2 binary release for Windows:

include/server/utils/fmgrprotos.h