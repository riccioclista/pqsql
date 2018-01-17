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

The following files stem from postgresql/REL9_6_STABLE branch: 

src/backend/utils/adt/datetime.c (1b145a700158ab48c387d38f4830136d307a0e87)
src/backend/utils/adt/float.c (1acf7572554515b99ef6e783750aaea8777524ec)
src/backend/utils/adt/numeric.c (def03e4bfe30c230d7532f2d7cfe5d7485a658a8)

src/include/c.h (4a15f87d22773ba208c441142ac53ddeb090d1b8)
src/include/fmgr.h (490734c588c4e70d46950c4f6c64dfd6e592cdcc)
src/include/postgres.h (23b09e15b9f40baeff527ca4dbc40afc823dd962)

src/include/utils/builtins.h (ed0097e4f9e6b1227935e01fa67f12a238b66064)
src/include/utils/datetime.h (ee943004466418595363d567f18c053bae407792)
src/include/utils/numeric.h (9389fbd0385776adf3252eb8cfe6e37a640fdff4)

src/interfaces/ecpg/ecpglib/pg_type.h (ee943004466418595363d567f18c053bae407792)
src/interfaces/libpq/pqexpbuffer.h (ee943004466418595363d567f18c053bae407792)


The following files stem from postgresql/master branch:

src/include/postgres_fe.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)

src/common/fe_memutils.c (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)
src/include/common/fe_memutils.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)

src/fe_utils/psqlscan.l (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)
src/fe_utils/psqlscan.c (generated from psqlscan.l)

src/include/fe_utils/psqlscan.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)
src/include/fe_utils/psqlscan_int.h (9d4649ca49416111aee2c84b7e4441a0b7aa2fac)