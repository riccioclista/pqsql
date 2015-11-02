#include <windows.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <float.h>

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


/*
 * add NULL value parameter of specified type
 */
DECLSPEC void __fastcall
pqbf_set_null(pqparam_buffer *pb, uint32_t o)
{
	BAILIFNULL(pb);
	pqpb_add(pb, o, NULL, 0);
}

/*
 * get single byte from result
 */
DECLSPEC unsigned char __fastcall
pqbf_get_byte(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return *p;
}

/*
 * oid 16: bool
 */
DECLSPEC int __fastcall
pqbf_get_bool(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p);
}

DECLSPEC void __fastcall
pqbf_set_bool(pqparam_buffer *pb, int b)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */
	
	/* encode bool */
	appendPQExpBufferChar(s, (char) b & 0x1);

	pqpb_add(pb, BOOLOID, top, sizeof(char));
}

/*
 * oid 17:   bytea
 */
DECLSPEC void __fastcall
pqbf_get_bytea(const char *p, char* buf, size_t len)
{
	BAILIFNULL(p);
	BAILIFNULL(buf);

	/* copy len bytes from p to buf */
	memcpy(buf, p, len);
}

DECLSPEC void __fastcall
pqbf_set_bytea(pqparam_buffer *pb, const char* buf, size_t buflen)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */
	
	/* copy bytea as is */
	appendBinaryPQExpBuffer(s, buf, buflen);

	pqpb_add(pb, BYTEAOID, top, buflen);
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
pqbf_get_int8(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT64_MIN);
	return _byteswap_uint64(*((uint64_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_int8(pqparam_buffer *pb, int64_t i)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */
	
	/* encode integer in network order */
	i = _byteswap_uint64(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, INT8OID, top, sizeof(i));
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
pqbf_get_int2(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT16_MIN);
	return _byteswap_ushort(*((uint16_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_int2(pqparam_buffer *pb, int16_t i)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; // save top of payload
	
	// encode integer in network order
	i = _byteswap_ushort(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, INT2OID, top, sizeof(i));
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
pqbf_get_int4(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT32_MIN);
	return _byteswap_ulong(*((uint32_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_int4(pqparam_buffer *pb, int32_t i)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode integer in network order */
	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*)&i, sizeof(i));

	pqpb_add(pb, INT4OID, top, sizeof(i));
}

/*
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
pqbf_get_text(const char *p, size_t *len)
{
	BAILWITHVALUEIFNULL(p, NULL);
	*len = strlen(p);
	return p;
}

DECLSPEC wchar_t* __fastcall
pqbf_get_unicode_text(const char *p, size_t *utf16_len)
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
pqbf_free_unicode_text(wchar_t *p)
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
pqbf_set_text(pqparam_buffer *pb, const char *t)
{
	PQExpBuffer s;
	char *top;

	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	appendPQExpBufferStr(s, t);

	pqpb_add(pb, TEXTOID, top, s->data + s->len - top);
}

/*
 * https://msdn.microsoft.com/en-us/library/dd374081.aspx
 * windows utf-16 strings
 */
DECLSPEC void __fastcall
pqbf_set_unicode_text(pqparam_buffer *pb, const wchar_t *t)
{
	PQExpBuffer s;
	char *top;
	
	int utf16_len;
	int utf8_len;
	char *obuf;

	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	utf16_len = wcslen(t); /* get number of characters in utf-16 string */

	/* allocate enough room to hold standard utf8 text */
	/* utf-8 requires 8, 16, 24, or 32 bits for a single character */
	utf8_len = utf16_len * 4 + 1;

	obuf = (char *) malloc(utf8_len);
	if (!obuf)
	{
		//fprintf(stderr, "pqbf_set_unicode_text: malloc failed\n");
		return;
	}

	/* encode UTF-16 as UTF-8 */
	utf8_len = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, t, -1, obuf, utf8_len, NULL, NULL);
	if (utf8_len == 0)
	{
		free(obuf);
		//fprintf(stderr, "pqbf_set_unicode_text: WideCharToMultiByte failed with %d\n", GetLastError());
		return;
	}

	appendPQExpBufferStr(s, obuf);
	free(obuf);

	pqpb_add(pb, TEXTOID, top, s->data + s->len - top);
}

/*
 * oid 26: oid
 */
DECLSPEC uint32_t __fastcall
pqbf_get_oid(const char *p)
{
	BAILWITHVALUEIFNULL(p, UINT32_MAX);
	return _byteswap_ulong(*((uint32_t *) p));
}

// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
DECLSPEC void __fastcall
pqbf_set_oid(pqparam_buffer *pb, uint32_t i)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	i = _byteswap_ulong(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, OIDOID, top, sizeof(i));
}


/*
 * oid 700: float4
 *
 * see
 * - float4 pq_getmsgfloat4(StringInfo msg)
 * - void pq_sendfloat4(StringInfo msg, float4 f)
 * from src/backend/libpq/pqformat.c
 */

union float_swap
{
	float f;
	uint32_t i;
};

DECLSPEC float __fastcall
pqbf_get_float4(const char *p)
{
	union float_swap swap;

	BAILWITHVALUEIFNULL(p, FLT_MIN);

	/* decode float4 */
	swap.i =  _byteswap_ulong(*((uint32_t*)p));
	return swap.f;
}

DECLSPEC void __fastcall
pqbf_set_float4(pqparam_buffer *pb, float f)
{
	PQExpBuffer s;
	char *top;
	union float_swap swap;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode float4 */
	swap.f = f;
	swap.i = _byteswap_ulong(swap.i);
	appendBinaryPQExpBuffer(s, (const char*) &swap.i, sizeof(swap.i));

	pqpb_add(pb, FLOAT4OID, top, sizeof(swap.i));
}


/*
 * oid 701: float8
 *
 * see
 * - float8 pq_getmsgfloat8(StringInfo msg)
 * - void pq_sendfloat8(StringInfo msg, float8 f)
 * from src/backend/libpq/pqformat.c
 */

union double_swap
{
	double f;
	uint64_t i;
};

DECLSPEC double __fastcall
pqbf_get_float8(const char *p)
{
	union double_swap swap;

	BAILWITHVALUEIFNULL(p, DBL_MIN);

	/* decode float8 */
	swap.i =  _byteswap_uint64(*((uint64_t*)p));
	return swap.f;
}

DECLSPEC void __fastcall
pqbf_set_float8(pqparam_buffer *pb, double f)
{
	PQExpBuffer s;
	char *top;
	union double_swap swap;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode float8 */
	swap.f = f;
	swap.i = _byteswap_uint64(swap.i);
	appendBinaryPQExpBuffer(s, (const char*) &swap.i, sizeof(swap.i));

	pqpb_add(pb, FLOAT8OID, top, sizeof(swap.i));
}


/*
 * oid 1082: date
 */
DECLSPEC int32_t __fastcall
pqbf_get_date(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return _byteswap_ulong(*((uint32_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_date(pqparam_buffer *pb, int32_t t)
{
	uint32_t i;
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	i = _byteswap_ulong(t);

	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, DATEOID, top, sizeof(i));
}



/*
 * oid 1083: time
 */

/* January 1, 2000, 00:00:00 UTC (in Unix epoch seconds) */
#define POSTGRES_EPOCH_DATE 946684800
#define POSTGRES_MEGA 1000000
#define POSTGRES_TICKS_PER_MILLISECOND 10000

DECLSPEC time_t __fastcall
pqbf_get_time(const char *p)
{
	uint64_t i;

	BAILWITHVALUEIFNULL(p, 0);

	/* decode 64bit time into unix time */
	i = _byteswap_uint64( *( (uint64_t *)p ) );
		
	return POSTGRES_EPOCH_DATE + (int64_t) (i * POSTGRES_TICKS_PER_MILLISECOND);
}

DECLSPEC void __fastcall
pqbf_set_time(pqparam_buffer *pb, time_t t)
{
	PQExpBuffer s;
	char *top;
	uint64_t i;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	t -= POSTGRES_EPOCH_DATE;
	t *= POSTGRES_MEGA;

	/* encode 64bit time */
	i = _byteswap_uint64(t);

	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, TIMEOID, top, sizeof(i));
}


/*
 * oid 1114: timestamp
 */
DECLSPEC void __fastcall
pqbf_get_timestamp(const char *p, time_t *sec, int *usec)
{
	uint64_t i;

	if (p == NULL)
	{
		*sec = 0;
		*usec = 0;
		return;
	}

	/* decode 64bit timestamp into sec and usec part */
	i = _byteswap_uint64( *( (uint64_t *)p ) );
		
	*sec = POSTGRES_EPOCH_DATE + (int64_t) (i / POSTGRES_MEGA);
	*usec = i % POSTGRES_MEGA;
}

DECLSPEC void __fastcall
pqbf_set_timestamp(pqparam_buffer *pb, time_t sec, int usec)
{
	PQExpBuffer s;
	char *top;
	uint64_t i;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	sec -= POSTGRES_EPOCH_DATE;
	sec *= POSTGRES_MEGA;

	/* encode 64bit timestamp from sec and usec part */
	i = _byteswap_uint64((uint64_t) (sec + usec));

	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, TIMESTAMPOID, top, sizeof(i));
}


/*
 * oid 1184: timestamptz
 *
 * TODO: adapt to local timezone
 */
DECLSPEC void __fastcall
pqbf_get_timestamptz(const char *p, time_t *sec, time_t *usec)
{
	uint64_t i;

	if (p == NULL)
	{
		*sec = 0;
		*usec = 0;
		return;
	}

	/* decode 64bit timestamp into sec and usec part */
	i = _byteswap_uint64( *( (uint64_t *)p ) );
		
	*sec = POSTGRES_EPOCH_DATE + (int64_t) (i / POSTGRES_MEGA);
	*usec = i % POSTGRES_MEGA;
}

DECLSPEC void __fastcall
pqbf_set_timestamptz(pqparam_buffer *pb, time_t sec, time_t usec)
{
	PQExpBuffer s;
	char *top;
	uint64_t i;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	sec -= POSTGRES_EPOCH_DATE;
	sec *= POSTGRES_MEGA;

	usec *= POSTGRES_MEGA;

	/* encode 64bit timestamp from sec and usec part */
	i = sec + usec;
	i = _byteswap_uint64(i);

	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));

	pqpb_add(pb, TIMESTAMPTZOID, top, sizeof(i));
}

/*
 * oid 1186: interval
 *
 * see timestamp_recv() from src/backend/utils/adt/timestamp.c
 */

DECLSPEC void __fastcall
pqbf_get_interval(const char *ptr, int64_t *offset, int32_t *day, int32_t *month)
{
	char *p;

	BAILIFNULL(ptr);
	BAILIFNULL(offset);
	BAILIFNULL(day);
	BAILIFNULL(month);

	p = (char *)ptr;

	/* decode interval */
	*offset =  _byteswap_uint64(*((uint64_t*)p));
	p += sizeof(*offset);

	*day = _byteswap_ulong(*((uint32_t*)p));
	p += sizeof(*day);

	*month = _byteswap_ulong(*((uint32_t*)p));
}

DECLSPEC void __fastcall
pqbf_set_interval(pqparam_buffer *pb, int64_t offset, int32_t day, int32_t month)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; /* save top of payload */

	/* encode interval */
	offset = _byteswap_uint64(offset);
	appendBinaryPQExpBuffer(s, (const char*) &offset, sizeof(offset));

	day = _byteswap_ulong(day);
	appendBinaryPQExpBuffer(s, (const char*) &day, sizeof(day));

	month = _byteswap_ulong(month);
	appendBinaryPQExpBuffer(s, (const char*) &month, sizeof(month));

	pqpb_add(pb, INTERVALOID, top, s->data + s->len - top);
}

/*
 * oid 1266: timetz
 */

DECLSPEC time_t __fastcall
pqbf_get_timetz(const char *p)
{
	// TODO
	BAILWITHVALUEIFNULL(p, 0);

	return 0;
}

/*
 * oid 1560: bit
 */
DECLSPEC int __fastcall
pqbf_get_bit(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p) & 0x1;
}

DECLSPEC void __fastcall
pqbf_set_bit(pqparam_buffer *pb, int b)
{
	PQExpBuffer s;
	char *top;

	BAILIFNULL(pb);

	s = pb->payload;
	top = s->data + s->len; // save top of payload

	appendPQExpBufferChar(s, (char) b & 0x1);

	pqpb_add(pb, BITOID, top, sizeof(char));
}

/*
 * oid 1562: varbit 
 */

// TODO


/*
 * oid 2950: uuid
 */
DECLSPEC void __fastcall
pqbf_get_uuid(const char *ptr, char *b[])
{
	// TODO
}

#ifdef  __cplusplus
}
#endif