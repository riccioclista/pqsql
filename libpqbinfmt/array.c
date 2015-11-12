#include <stdio.h>
#include <stdlib.h>

#define DLL_EXPORT
#include "pqbinfmt.h"
#include "pq_types.h"


DECLSPEC const char * __fastcall
pqbf_get_array(const char* p,
								int32_t* ndim, int32_t* flags, uint32_t* o,
								int* dim[MAXDIM],	int* lbound[MAXDIM])
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



DECLSPEC const char * __fastcall
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


#define pqbf_encode_array(s,ndim,flags,oid,dim,lbound) \
	do { \
		int i; \
		uint32_t v; \
		/* 12 byte array header */ \
		v = BYTESWAP4(ndim); \
		appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v)); \
		v = BYTESWAP4(flags); \
		appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v)); \
		v = BYTESWAP4(oid); \
		appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v)); \
		/* ndim * 8 byte dimension header */ \
		for (i = 0; i < ndim; i++) { /* we trust pgsql here for correct lower and upper bounds */ \
			v = BYTESWAP4(dim[i]); \
			appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v)); \
			v = BYTESWAP4(lbound[i]); \
			appendBinaryPQExpBuffer(s, (const char*) &v, sizeof(v)); \
		} \
	} while(0)


DECLSPEC void __fastcall
pqbf_set_array(PQExpBuffer s,
								int32_t ndim, int32_t flags, uint32_t oid,
								int dim[MAXDIM],	int lbound[MAXDIM])
{
	BAILIFNULL(s);
	pqbf_encode_array(s, ndim, flags, oid, dim, lbound);
	/* next comes array data */
}



DECLSPEC void __fastcall
pqbf_set_array_itemlength(PQExpBuffer a, int32_t itemlen)
{
	BAILIFNULL(a);
	itemlen = BYTESWAP4(itemlen);
	appendBinaryPQExpBuffer(a, (const char*) &itemlen, sizeof(itemlen)); /* add item length */
	/* next comes array item */
}

#if 0
DECLSPEC void __fastcall
pqbf_set_array_value(PQExpBuffer a, const char* p, int32_t itemlen)
{
	BAILIFNULL(a);
	/* null values have itemlen == -1 */
	if (p != NULL && itemlen >= 0) /* add non-null value array item */
		appendBinaryPQExpBuffer(a, p, itemlen);
}
#endif

DECLSPEC void __fastcall
pqbf_add_array(pqparam_buffer *pb, PQExpBuffer a, uint32_t oid)
{
	size_t len;

	BAILIFNULL(pb);
	BAILIFNULL(a);

	len = pb->payload->len; /* save current length of payload */

	appendBinaryPQExpBuffer(pb->payload, a->data, a->len); /* add array header + data */

	pqpb_add(pb, oid,  pb->payload->len - len);
}