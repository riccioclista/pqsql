/**
 * @file pqbinfmt_array.c
 * @brief encode/decode array binary format to native datatype for PQgetvalue() and pqparam_buffer
 * @date 2015-09-30 
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 * @note postgresql source src/backend/utils/adt/arrayfuncs.c
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stddef.h>

#ifndef _WIN32
#include <sys/time.h>
#endif /* _WIN32 */

#define DLL_EXPORT
#include "pqbinfmt_config.h"
#include "pqbinfmt.h"


DECLSPEC const char *
pqbf_get_array(const char* p, int32_t* ndim, int32_t* flags, uint32_t* o,
				int* dim[MAXDIM], int* lbound[MAXDIM])
{
	char *v;
	int i;

	BAILWITHVALUEIFNULL(p, NULL);

	v = (char*) p;

	/* 12 byte array header */

	*ndim = BYTESWAP4(*((uint32_t *)v));
	v += sizeof(*ndim);

	*flags = BYTESWAP4(*((uint32_t *)v));
	v += sizeof(*flags);

	*o = BYTESWAP4(*((uint32_t *)v));
	v += sizeof(*o);

	/* ndim * 8 byte dimension header */

	for (i = 0; i < *ndim; i++)
	{
		/* we trust pgsql here for correct lower and upper bounds */
		*dim[i] = BYTESWAP4(*((uint32_t *)v));
		v += sizeof(int);

		*lbound[i] = BYTESWAP4(*((uint32_t *)v));
		v += sizeof(int);
	}

	/* array data start */
	return v;
}


DECLSPEC const char *
pqbf_get_array_value(const char* p, int32_t* itemlen)
{
	if (p == NULL) /* null values are encoded with itemlen=-1 */
	{
		*itemlen = -1;
		return NULL;
	}
	/* itemlen == -1: null value, otw. we get the number of bytes for the next array item */
	*itemlen = BYTESWAP4(*((uint32_t *)p));
	return p + sizeof(int);
}


inline void
pqbf_encode_array(PQExpBuffer s, int32_t ndim, int32_t flags, uint32_t oid,
					int dim[MAXDIM], int lbound[MAXDIM])
{
	int i;
	uint32_t v;
	
	/* 12 byte array header */
	v = BYTESWAP4(ndim);
	appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v));
	v = BYTESWAP4(flags);
	appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v));
	v = BYTESWAP4(oid);
	appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v));

	/* ndim * 8 byte dimension header */
	for (i = 0; i < ndim; i++)
	{
		/* we trust pgsql here for correct lower and upper bounds */
		v = BYTESWAP4(dim[i]);
		appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v));
		v = BYTESWAP4(lbound[i]);
		appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v));
	}
}


DECLSPEC void
pqbf_set_array(PQExpBuffer s, int32_t ndim, int32_t flags, uint32_t oid,
				int dim[MAXDIM], int lbound[MAXDIM])
{
	BAILIFNULL(s);
	pqbf_encode_array(s, ndim, flags, oid, dim, lbound);
	/* next comes array data */
}


DECLSPEC void
pqbf_set_array_itemlength(PQExpBuffer a, int32_t itemlen)
{
	BAILIFNULL(a);
	itemlen = BYTESWAP4(itemlen);
	appendBinaryPQExpBuffer(a, (const char*) &itemlen, sizeof(itemlen)); /* add item length */
	/* next comes array item */
}


DECLSPEC void
pqbf_update_array_itemlength(PQExpBuffer a, ptrdiff_t offset, int32_t itemlen)
{
	BAILIFNULL(a);
	itemlen = BYTESWAP4(itemlen);
	/* overwrite data starting at offset position (we assume offset is negative) with new item length */
	memcpy(a->data + a->len + offset, (const char*) &itemlen, sizeof(itemlen));
	/* next comes array item */
}


DECLSPEC void
pqbf_add_array(pqparam_buffer *pb, PQExpBuffer a, uint32_t oid)
{
	size_t len;

	BAILIFNULL(pb);
	BAILIFNULL(a);

	len = pb->payload->len; /* save current length of payload */

	appendBinaryPQExpBuffer(pb->payload, a->data, a->len); /* add array header + data */

	pqpb_add(pb, oid,  pb->payload->len - len);
}


DECLSPEC void
pqbf_set_array_value(PQExpBuffer a, const char* p, int32_t itemlen)
{
	// TODO
}