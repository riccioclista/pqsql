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



Supported PostgreSQL datatypes
==============================

bool

bytea

text

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




Unsupported datatypes
=====================

bit					src/backend/utils/adt/varbit.c
"char"			src/backend/utils/adt/char.c
cidr				src/backend/utils/adt/network.c
inet				src/backend/utils/adt/network.c
macaddr			src/backend/utils/adt/mac.c
uuid				src/backend/utils/adt/uuid.c
xid					src/backend/utils/adt/xid.c
...


Incorporated Postgresql code
============================
src/backend/utils/adt/float.c
src/backend/utils/adt/numeric.c
src/include/utils/numeric.h
src/interfaces/ecpg/ecpglib/pg_type.h
src/interfaces/libpq/pqexpbuffer.h