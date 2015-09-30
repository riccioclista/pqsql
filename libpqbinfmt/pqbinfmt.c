#include <windows.h>
#include <intrin.h>

#include <errno.h>
#include <sys/types.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <float.h>
#include <math.h>
#include <time.h>

#define DLL_EXPORT
#include "pqbinfmt.h"

/* libpq pqexpbuffer.h */
typedef struct PQExpBufferData
{
  char       *data;
  size_t      len;
  size_t      maxlen;
} PQExpBufferData;
typedef PQExpBufferData *PQExpBuffer;

extern void appendPQExpBufferStr(PQExpBuffer str, const char *data);
extern void appendPQExpBufferChar(PQExpBuffer str, char ch);
extern void appendBinaryPQExpBuffer(PQExpBuffer str, const char *data, size_t datalen);

/* pgtypes http://www.postgresql.org/docs/current/static/ecpg-pgtypes.html */
#include <pgtypes_date.h>
#include <pgtypes_error.h>
#include <pgtypes_interval.h>
#include <pgtypes_numeric.h>
#include <pgtypes_timestamp.h>


#ifdef  __cplusplus
extern "C" {
#endif


DECLSPEC unsigned char __fastcall
getmsg_byte(const char *ptr)
{
	return *ptr;
}

/*
 * oid 16: bool
 *
 * see char* pq_getmsgstring(StringInfo msg) from pqformat.c
 */
DECLSPEC int __fastcall
getmsg_bool(const char *ptr)
{
	return ((int)*ptr);
}

DECLSPEC void __fastcall
setmsg_bool(void *payload, int b, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position
	
	// encode bool
	appendPQExpBufferChar(s, (char) b & 0x1);

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of bool
	**param_len = sizeof(char);  // size of bool
}

/*
 * oid 20: int8
 *
 * see int pq_getmsgint64(StringInfo msg) from pqformat.c
 */
DECLSPEC int64_t __fastcall
getmsg_int8(const char *ptr)
{
	return _byteswap_uint64(*((uint64_t *) ptr));
}

// see pq_sendint64(StringInfo buf, int64 i) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_int8(void *payload, int64_t i, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position
	
	// encode integer in network order
	i = _byteswap_uint64(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of int8
	**param_len = sizeof(i);     // size of int8
}
/*
 * oid 21: int2
 *
 * see int pq_getmsgint(StringInfo msg) from pqformat.c 
 */
DECLSPEC int16_t __fastcall
getmsg_int2(const char *ptr)
{
	return _byteswap_ushort(*((uint16_t *) ptr));
}

// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_int2(void *payload, int16_t i, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position
	
	// encode integer in network order
	i = _byteswap_ushort(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of int2
	**param_len = sizeof(i);     // size of int2
}

/*
 * oid 23: int4
 *
 * see int pq_getmsgint(StringInfo msg) from pqformat.c 
 */
DECLSPEC int32_t __fastcall
getmsg_int4(const char *ptr)
{
	return _byteswap_ulong(*((uint32_t *) ptr));
}

// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_int4(void *payload, int32_t i, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position
	
	// encode integer in network order
	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of int4
	**param_len = sizeof(i);     // size of int4
}

/*
 * oid 17:   bytea
 * oid 25:   text
 * oid 705:  unknown
 * oid 1043: varchar
 * oid 1790: refcursor
 *
 * see
 *  - char* pq_getmsgstring(StringInfo msg),
 *  - pq_sendtext(StringInfo buf, const char *str, int slen)
 * from src/backend/libpq/pqformat.c
 */
DECLSPEC const char* __fastcall
getmsg_text(const char *ptr, int *len)
{
	*len = strlen(ptr);
	return ptr;
}

DECLSPEC wchar_t* __fastcall
getmsg_unicode_text(const char *ptr, int *utf16_len)
{
	char *ibuf = (char*) ptr;
	wchar_t *obuf;
	size_t ibuf_len = strlen(ptr); // total utf8 length

	// allocate enough room to hold standard utf16 text (2 bytes per char)
	// and also non-BMP unicode (4 bytes per char)
	*utf16_len = 4 * ibuf_len;
	obuf = (wchar_t *) malloc(*utf16_len);
	if (!obuf)
	{
		fprintf(stderr, "getmsg_unicode_text: malloc failed\n");
		return NULL;
  }
	
	*utf16_len = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, ptr, -1, obuf, *utf16_len);

  if (*utf16_len == -1)
	{
		fprintf(stderr, "getmsg_unicode_text: iconv failed: %d\n", errno);
		free(obuf);
		return NULL;
	}

	return (wchar_t*) obuf;
}

DECLSPEC void __fastcall
free_unicode_text(wchar_t *p)
{
	if (p) free(p);
}


DECLSPEC void __fastcall
setmsg_text(void *payload, const char* t, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	appendPQExpBufferStr(s, t);

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of string
	**param_len = s->len - pos;  // strlen
}

/*
 * oid 26: oid
 */
DECLSPEC uint32_t __fastcall
getmsg_oid(const char *ptr)
{
	return _byteswap_ulong(*((uint32_t *) ptr));
}

// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_oid(void *payload, uint32_t i, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of oid
	**param_len = sizeof(i);     // size of oid
}

/*
 * oid 700: float4
 *
 * see float4 pq_getmsgfloat4(StringInfo msg) from pqformat.c
 */
DECLSPEC float __fastcall
getmsg_float4(const char *ptr)
{
	// decode float4
	union
	{
		float f;
		uint32_t i;
	} swap;
	
	swap.i =  _byteswap_ulong(*((uint32_t*)ptr));
	return swap.f;
}

// see void pq_sendfloat4(StringInfo msg, float4 f) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_float4(void *payload, float f, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	// encode float4
	union
	{
		float f;
		uint32_t i;
	} swap;

	swap.f = f;
	swap.i = _byteswap_ulong(swap.i);
	appendBinaryPQExpBuffer(s, (const char*)&swap.i, sizeof(swap.i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos];     // start of float4
	**param_len = sizeof(swap.i); // size of float4
}


/*
 * oid 701: float8
 *
 * see float8 pq_getmsgfloat8(StringInfo msg) from pqformat.c
 */
DECLSPEC double __fastcall
getmsg_float8(const char *ptr)
{
	// decode float8
	union
	{
		double f;
		uint64_t i;
	} swap;
	
	swap.i =  _byteswap_uint64(*((uint64_t*)ptr));
	return swap.f;
}

// see void pq_sendfloat8(StringInfo msg, float8 f) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_float8(void *payload, double f, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	// encode float8
	union
	{
		double f;
		uint64_t i;
	} swap;

	swap.f = f;
	swap.i = _byteswap_ulong(swap.i);
	appendBinaryPQExpBuffer(s, (const char*)&swap.i, sizeof(swap.i));

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos];  // start of float8
	**param_len = sizeof(swap.i); // size of float8
}


/*
 * oid 1082: date
 */
DECLSPEC void __fastcall
getmsg_date(const char *ptr, time_t *sec, time_t *usec)
{
	// TODO
}


/*
 * oid 1083: time
 */
DECLSPEC void __fastcall
getmsg_time(const char *ptr, time_t *sec, time_t *usec)
{
	// TODO
}


/*
 * oid 1114: timestamp
 */
/* January 1, 2000, 00:00:00 UTC (in Unix epoch seconds) */
#define POSTGRES_EPOCH_DATE 946684800

DECLSPEC void __fastcall
getmsg_timestamp(const char *ptr, time_t *sec, time_t *usec)
{
	uint64_t i = _byteswap_uint64( *( (uint64_t *)ptr ) );
		
	*sec = POSTGRES_EPOCH_DATE + i / 1000000;
	*usec = i % 1000000;
}


/*
 * oid 1184: timestamptz
 */
DECLSPEC void __fastcall
getmsg_timestamptz(const char *ptr, time_t *sec, time_t *usec)
{
	uint64_t i = _byteswap_uint64( *( (uint64_t *)ptr ) );
		
	*sec = POSTGRES_EPOCH_DATE + i / 1000000;
	*usec = i % 1000000;
}


/*
 * oid 1186: interval
 *
 * see timestamp_recv() from src/backend/utils/adt/timestamp.c
 */

DECLSPEC void __fastcall
getmsg_interval(const char *ptr, time_t *sec, time_t *usec)
{
	// TODO
}


/*
 * oid 1266: timetz
 */

DECLSPEC void __fastcall
getmsg_timetz(const char *ptr, time_t *sec, time_t *usec)
{
	// TODO
}

/*
 * oid 1560: bit
 */
DECLSPEC int __fastcall
getmsg_bit(const char *ptr)
{
	return ((int)*ptr) & 0x1;
}

// see void pq_sendfloat8(StringInfo msg, float8 f) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_bit(void *payload, int b, char **param_val, int **param_len)
{
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	appendPQExpBufferChar(s, (char) b & 0x1);

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of bit
	**param_len = sizeof(char);  // size of bit
}

/*
 * oid 1562: varbit 
 */


/*
 * oid 1700: numeric
 *
 * see  numeric_recv() from src/backend/utils/adt/numeric.c
 */

// src/interfaces/ecpg/pgtypeslib/common.c
/* Return value is zero-filled. */
char *
pgtypes_alloc(long size)
{
	char *n = (char *) calloc(1L, size);
	if (!n)	errno = ENOMEM;
	return (n);
}
// src/interfaces/ecpg/pgtypeslib/common.c

// src/interfaces/ecpg/pgtypeslib/numeric.c
#define digitbuf_alloc(size) ((NumericDigit *) pgtypes_alloc(size))
#define digitbuf_free(buf)      \
        do { \
                  if ((buf) != NULL) \
                           free(buf); \
           } while (0)

/* ----------
 *  alloc_var() -
 *
 *   Allocate a digit buffer of ndigits digits (plus a spare digit for rounding)
 * ----------
 */
static int
alloc_var(numeric *var, int ndigits)
{
	digitbuf_free(var->buf);
	var->buf = digitbuf_alloc(ndigits + 1);
	if (var->buf == NULL)
		return -1;
	var->buf[0] = 0;
	var->digits = var->buf + 1;
	var->ndigits = ndigits;
	return 0;
}
// src/interfaces/ecpg/pgtypeslib/numeric.c


DECLSPEC double __fastcall
getmsg_numeric(const char *ptr)
{
	int i;
	char *p = (char*) ptr;
	double d;
	numeric *n = PGTYPESnumeric_new();

	uint16_t len = getmsg_int2(p);
	p += sizeof(uint16_t);

  alloc_var(n, len);

	n->weight = getmsg_int2(p);
	p += sizeof(uint16_t);
	
	n->sign = getmsg_int2(p);
	p += sizeof(uint16_t);
  
  n->dscale = getmsg_int2(p);
	p += sizeof(uint16_t);
  
  for (i = 0; i < len; i++, p += sizeof(NumericDigit))
  {
    NumericDigit d = getmsg_byte(ptr);
    n->digits[i] = d;
  }
   
  /*
   * If the given dscale would hide any digits, truncate those digits away.
   * We could alternatively throw an error, but that would take a bunch of
   * extra code (about as much as trunc_var involves), and it might cause
   * client compatibility issues.
   */
  //trunc_var(&value, value.dscale);
  //apply_typmod(&value, typmod);
   
	// decode numeric to double
	PGTYPESnumeric_to_double(n, &d);
	PGTYPESnumeric_free(n);

	return d;
}

// see  numeric_send() from src/backend/utils/adt/numeric.c
DECLSPEC void __fastcall
setmsg_numeric(void *payload, double d, char **param_val, int **param_len)
{
	int i;
	int16_t i16;
	PQExpBuffer s = (PQExpBuffer) payload;
	int pos = s->len; // save current position

	// encode double as numeric
	numeric *n = PGTYPESnumeric_new();
	PGTYPESnumeric_from_double(d, n);

	// encode numeric into binary format

	i16 = _byteswap_ulong(n->ndigits);
	appendBinaryPQExpBuffer(s, (const char*)&i16, sizeof(i16));

	i16 = _byteswap_ulong(n->weight);
	appendBinaryPQExpBuffer(s, (const char*)&i16, sizeof(i16));

	i16 = _byteswap_ulong(n->sign);
	appendBinaryPQExpBuffer(s, (const char*)&i16, sizeof(i16));

	i16 = _byteswap_ulong(n->dscale);
	appendBinaryPQExpBuffer(s, (const char*)&i16, sizeof(i16));

	for (i = 0; i < n->ndigits; i++)
	{
		appendBinaryPQExpBuffer(s, (const char*)&n->digits[i], sizeof(NumericDigit));
	}

	// free numeric
	PGTYPESnumeric_free(n);

	/* assumption: param_val and param_len point to the current top of the stack */
	*param_val  = &s->data[pos]; // start of numeric
	**param_len = s->len - pos;  // size of numeric
}

/*
 * oid 2950: uuid
 */
DECLSPEC void __fastcall
getmsg_uuid(const char *ptr, char *b[])
{
	// TODO
}

#ifdef  __cplusplus
}
#endif