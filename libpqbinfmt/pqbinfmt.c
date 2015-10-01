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

/*
 * pgtypes
 * http://www.postgresql.org/docs/current/static/ecpg-pgtypes.html
 */
#include <pgtypes_date.h>
#include <pgtypes_error.h>
#include <pgtypes_interval.h>
#include <pgtypes_numeric.h>
#include <pgtypes_timestamp.h>

/*
 * oid values
 */
#include "pq_types.h"


#define DLL_EXPORT
#include "pqbinfmt.h"
#include "pqparam_buffer.h"



#ifdef  __cplusplus
extern "C" {
#endif

#define BAILIFNULL(p) do { if (p == NULL) return; } while(0)
#define BAILWITHVALUEIFNULL(p,val) do { if (p == NULL) return val; } while(0)

/*
 * add NULL value parameter of specified type
 */
DECLSPEC void __fastcall
setmsg_null(void *p, uint32_t o)
{
	BAILIFNULL(p);
	add_parameter_buffer((pqparam_buffer*) p, o, NULL, 0);
}

/*
 * get single byte from result
 */
DECLSPEC unsigned char __fastcall
getmsg_byte(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return *p;
}

/*
 * oid 16: bool
 */
DECLSPEC int __fastcall
getmsg_bool(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p);
}

DECLSPEC void __fastcall
setmsg_bool(void *p, int b)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = buf->payload;
	top = s->data + s->len; /* save top of payload */
	
	/* encode bool */
	appendPQExpBufferChar(s, (char) b & 0x1);

	add_parameter_buffer(buf, BOOLOID, top, sizeof(char));
}

/*
 * oid 20: int8
 *
 * see
 * - int pq_getmsgint64(StringInfo msg)
 * - pq_sendint64(StringInfo buf, int64 i)
 * from src/backend/libpq/pqformat.c
 */
DECLSPEC int64_t __fastcall
getmsg_int8(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT64_MIN);
	return _byteswap_uint64(*((uint64_t *) p));
}

DECLSPEC void __fastcall
setmsg_int8(void *p, int64_t i)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */
	
	/* encode integer in network order */
	i = _byteswap_uint64(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	add_parameter_buffer(buf, INT8OID, top, sizeof(i));
}


/*
 * oid 21: int2
 *
 * see
 * - int pq_getmsgint(StringInfo msg)
 * - pq_sendint(StringInfo buf, int i, int b)
 * from src/backend/libpq/pqformat.c
 */

DECLSPEC int16_t __fastcall
getmsg_int2(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT16_MIN);
	return _byteswap_ushort(*((uint16_t *) p));
}

DECLSPEC void __fastcall
setmsg_int2(void *p, int16_t i)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; // save top of payload
	
	// encode integer in network order
	i = _byteswap_ushort(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	add_parameter_buffer(buf, INT2OID, top, sizeof(i));
}

/*
 * oid 23: int4
 *
 * see
 * - int pq_getmsgint(StringInfo msg)
 * - pq_sendint(StringInfo buf, int i, int b)
 * from src/backend/libpq/pqformat.c
 */

DECLSPEC int32_t __fastcall
getmsg_int4(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT32_MIN);
	return _byteswap_ulong(*((uint32_t *) p));
}

DECLSPEC void __fastcall
setmsg_int4(void *p, int32_t i)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode integer in network order */
	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	add_parameter_buffer(buf, INT4OID, top, sizeof(i));
	//wprintf(L"setmsg_int4: buf=%p s=%p i_le=%d i_be=%d beg=%p new_data=%p size=%d\n", buf, s, j, i, beg, s->data + s->len, sizeof(i));
	//wprintf(L"setmsg_int4: typ=%d val=%d len=%d fmt=%d\n", buf->param_typ[buf->num_param-1], *(int*)buf->param_val[buf->num_param-1], buf->param_len[buf->num_param-1], buf->param_fmt[buf->num_param-1]);
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
getmsg_text(const char *p, size_t *len)
{
	BAILWITHVALUEIFNULL(p, NULL);
	*len = strlen(p);
	return p;
}

DECLSPEC wchar_t* __fastcall
getmsg_unicode_text(const char *p, size_t *utf16_len)
{
	wchar_t *obuf;
	int retry;
	size_t len;

	BAILWITHVALUEIFNULL(p, NULL);

	len = strlen(p); /* total utf8 length */

	/* allocate enough room to hold standard utf16 text (2 bytes per char) */
	*utf16_len = 2 * len + 1;
	obuf = (wchar_t *) malloc(*utf16_len);
	if (!obuf)
	{
		*utf16_len = 0;
		return NULL;
  }
	
	/* decode UTF-8 as UTF-16 */

	for (retry = 0; retry < 2; retry++)
	{
		*utf16_len = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, p, -1, obuf, *utf16_len);

		if (*utf16_len == 0)
		{
			if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
			{
				wchar_t *new_obuf;

				/* in case of non-BMP unicode we need 4 bytes per char in utf-16 */
				*utf16_len = 4 * len + 1;
				new_obuf = (wchar_t *) realloc(obuf, *utf16_len);
				if (!new_obuf)
				{
					/* oh-oh, shit hits the fan */
					free(obuf);
					*utf16_len = 0;
					return NULL;
				}
				else
				{
					obuf = new_obuf;
				}
			}
			else
			{
				/* bail out */
				retry = 2;
			}
		}
	}

	if (*utf16_len == 0)
	{
		free(obuf);
		*utf16_len = 0;
		return NULL;
	}

	return (wchar_t*) obuf;
}


DECLSPEC void __fastcall
free_unicode_text(wchar_t *p)
{
	if (p)
	{
		free(p);
		p = NULL;
	}
}


/*
 * standard utf-8 string
 */
DECLSPEC void __fastcall
setmsg_text(void *p, const char *t)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	if (p == NULL || t == NULL) /* use setmsg_null for NULL parameters */
		return;

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	appendPQExpBufferStr(s, t);
	add_parameter_buffer(buf, TEXTOID, top, s->data + s->len - top);
}

/*
 * https://msdn.microsoft.com/en-us/library/dd374081.aspx
 * windows utf-16 strings
 */
DECLSPEC void __fastcall
setmsg_unicode_text(void *p, const wchar_t *t)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;
	
	int utf16_len;
	int utf8_len;
	char *obuf;

	if (p == NULL || t == NULL) /* use setmsg_null for NULL parameters */
		return;

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	utf16_len = wcslen(t); /* get number of characters in utf-16 string */

	/* allocate enough room to hold standard utf8 text */
	/* utf-8 requires 8, 16, 24, or 32 bits for a single character */
	utf8_len = utf16_len * 4 + 1;

	obuf = (char *) malloc(utf8_len);
	if (!obuf)
	{
		//fprintf(stderr, "setmsg_unicode_text: malloc failed\n");
		return;
	}

	/* encode UTF-16 as UTF-8 */
	utf8_len = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, t, -1, obuf, utf8_len, NULL, NULL);
	if (utf8_len == 0)
	{
		free(obuf);
		//fprintf(stderr, "setmsg_unicode_text: WideCharToMultiByte failed with %d\n", GetLastError());
		return;
	}

	appendPQExpBufferStr(s, obuf);
	free(obuf);

	add_parameter_buffer(buf, TEXTOID, top, s->data + s->len - top);
}

/*
 * oid 26: oid
 */
DECLSPEC uint32_t __fastcall
getmsg_oid(const char *p)
{
	BAILWITHVALUEIFNULL(p, UINT32_MAX);
	return _byteswap_ulong(*((uint32_t *) p));
}

// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_oid(void *p, uint32_t i)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	add_parameter_buffer(buf, OIDOID, top, sizeof(i));
}

/*
 * oid 700: float4
 *
 * see float4 pq_getmsgfloat4(StringInfo msg) from pqformat.c
 */
DECLSPEC float __fastcall
getmsg_float4(const char *p)
{
	union
	{
		float f;
		uint32_t i;
	} swap;

	BAILWITHVALUEIFNULL(p, FLT_MIN);

	/* decode float4 */
	swap.i =  _byteswap_ulong(*((uint32_t*)p));
	return swap.f;
}

// see void pq_sendfloat4(StringInfo msg, float4 f) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
setmsg_float4(void *p, float f)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;
	union
	{
		float f;
		uint32_t i;
	} swap;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode float4 */
	swap.f = f;
	swap.i = _byteswap_ulong(swap.i);
	appendBinaryPQExpBuffer(s, (const char*)&swap.i, sizeof(swap.i));

	add_parameter_buffer(buf, FLOAT4OID, top, sizeof(swap.i));
}


/*
 * oid 701: float8
 *
 * see
 * - float8 pq_getmsgfloat8(StringInfo msg)
 * - void pq_sendfloat8(StringInfo msg, float8 f)
 * from src/backend/libpq/pqformat.c
 */
DECLSPEC double __fastcall
getmsg_float8(const char *p)
{
	union
	{
		double f;
		uint64_t i;
	} swap;

	BAILWITHVALUEIFNULL(p, DBL_MIN);

	/* decode float8 */
	swap.i =  _byteswap_uint64(*((uint64_t*)p));
	return swap.f;
}

DECLSPEC void __fastcall
setmsg_float8(void *p, double f)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	union
	{
		double f;
		uint64_t i;
	} swap;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode float8 */
	swap.f = f;
	swap.i = _byteswap_ulong(swap.i);
	appendBinaryPQExpBuffer(s, (const char*)&swap.i, sizeof(swap.i));

	add_parameter_buffer(buf, FLOAT8OID, top, sizeof(swap.i));
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
getmsg_bit(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p) & 0x1;
}

DECLSPEC void __fastcall
setmsg_bit(void *p, int b)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; // save top of payload

	appendPQExpBufferChar(s, (char) b & 0x1);

	add_parameter_buffer(buf, BITOID, top, sizeof(char));
}

/*
 * oid 1562: varbit 
 */

// TODO

/*
 * oid 1700: numeric
 *
 * see  numeric_recv()
 * from src/backend/utils/adt/numeric.c src/interfaces/ecpg/pgtypeslib/numeric.c
 */

// src/interfaces/ecpg/pgtypeslib/common.c
/* Return value is zero-filled. */
static char *
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


DECLSPEC double __fastcall
getmsg_numeric(const char *ptr)
{
	double d;
	int i;
	char *p;
	uint16_t len;
	numeric *n;
	
	BAILWITHVALUEIFNULL(ptr, DBL_MIN);

	p = (char*) ptr;

	len = getmsg_int2(p);
	p += sizeof(uint16_t);

	n = PGTYPESnumeric_new();
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
setmsg_numeric(void *p, double d)
{
	pqparam_buffer *buf;
	PQExpBuffer s;
	char *top;

	int i;
	int16_t i16;
	numeric *n;
	
	BAILIFNULL(p);

	buf = (pqparam_buffer*) p;

	s = (PQExpBuffer) buf->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode double as numeric */
	n = PGTYPESnumeric_new();
	PGTYPESnumeric_from_double(d, n);

	/* encode numeric into binary format */

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

	/* free temp numeric */
	PGTYPESnumeric_free(n);

	add_parameter_buffer(buf, NUMERICOID, top, s->data + s->len - top);
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