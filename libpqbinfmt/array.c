
#include <stdio.h>

#define DLL_EXPORT
#include "pqbinfmt.h"


DECLSPEC const char * __fastcall
pqbf_get_array(const char* p,
								int32_t* ndim, int32_t* flags, uint32_t* o,
								int* dim[MAXDIM],	int* lbound[MAXDIM])
{
	char *v;
	int i;

	BAILWITHVALUEIFNULL(p, NULL);

	v = (char*) p;

	/* array header */

	*ndim = pqbf_get_int4(v);
	v += sizeof(*ndim);

	*flags = pqbf_get_int4(v);
	v += sizeof(*flags);

	*o = pqbf_get_oid(v);
	v += sizeof(*o);

	/* dimension header */

	for (i = 0; i < *ndim; i++)
	{
		/* we trust pgsql here for correct lower and upper bounds */
		*dim[i] = pqbf_get_int4(v);
		v += sizeof(int);

		*lbound[i] = pqbf_get_int4(v);
		v += sizeof(int);
	}

	/* array data start */
	return v;
}



DECLSPEC const char * __fastcall
pqbf_get_array_value(const char* p, int32_t* itemlen)
{
	if (p == NULL)
	{
		*itemlen = -1;
		return NULL;
	}

	*itemlen = pqbf_get_int4(p);
	return p + sizeof(int);
}