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



DECLSPEC size_t __fastcall
pqbf_get_buflen(PQExpBuffer s)
{
	return s->len;
}

DECLSPEC char * __fastcall
pqbf_get_bufval(PQExpBuffer s)
{
	return s->data;
}

/*
 * add NULL value parameter of specified type
 */
DECLSPEC void __fastcall
pqbf_add_null(pqparam_buffer *pb, uint32_t o)
{
	BAILIFNULL(pb);
	pqpb_add(pb, o, 0);
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

#define pqbf_encode_bool(s,b) do { appendPQExpBufferChar(s, (char) b & 0x1); } while(0)

DECLSPEC void __fastcall
pqbf_set_bool(PQExpBuffer s, int b)
{
	BAILIFNULL(s);
	pqbf_encode_bool(s, b);
}

DECLSPEC void __fastcall
pqbf_add_bool(pqparam_buffer *pb, int b)
{
	BAILIFNULL(pb);
	
	/* encode bool */
	pqbf_encode_bool(pb->payload, b);

	pqpb_add(pb, BOOLOID, sizeof(char));
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

#define pqbf_encode_bytea(s,buf,buflen) do { appendBinaryPQExpBuffer(s, buf, buflen); } while(0)

DECLSPEC void __fastcall
pqbf_set_bytea(PQExpBuffer s, const char* buf, size_t buflen)
{
	BAILIFNULL(s);
	pqbf_encode_bytea(s, buf, buflen);
}

DECLSPEC void __fastcall
pqbf_add_bytea(pqparam_buffer *pb, const char* buf, size_t buflen)
{
	BAILIFNULL(pb);

	/* copy bytea as is */
	pqbf_encode_bytea(pb->payload, buf, buflen);

	pqpb_add(pb, BYTEAOID, buflen);
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
	return BYTESWAP8(*((uint64_t *) p));
}

#define pqbf_encode_int8(s,i) do { i = BYTESWAP8(i); appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i)); } while(0)

DECLSPEC void __fastcall
pqbf_set_int8(PQExpBuffer s, int64_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int8(s, i);
}

DECLSPEC void __fastcall
pqbf_add_int8(pqparam_buffer *pb, int64_t i)
{
	BAILIFNULL(pb);
	
	/* encode integer in network order */
	pqbf_encode_int8(pb->payload, i);

	pqpb_add(pb, INT8OID, sizeof(i));
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
	return BYTESWAP2(*((uint16_t *) p));
}

#define pqbf_encode_int2(s,i) do { i = BYTESWAP2(i); appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i)); } while(0)

DECLSPEC void __fastcall
pqbf_set_int2(PQExpBuffer s, int16_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int2(s, i);
}

DECLSPEC void __fastcall
pqbf_add_int2(pqparam_buffer *pb, int16_t i)
{
	BAILIFNULL(pb);
	
	/* encode integer in network order */
	pqbf_encode_int2(pb->payload, i);

	pqpb_add(pb, INT2OID, sizeof(i));
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
	return BYTESWAP4(*((uint32_t *) p));
}

#define pqbf_encode_int4(s,i) do { i = BYTESWAP4(i); appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i)); } while(0)

DECLSPEC void __fastcall
pqbf_set_int4(PQExpBuffer s, int32_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, i);
}

DECLSPEC void __fastcall
pqbf_add_int4(pqparam_buffer *pb, int32_t i)
{
	BAILIFNULL(pb);

	/* encode integer in network order */
	pqbf_encode_int4(pb->payload, i);

	pqpb_add(pb, INT4OID, sizeof(i));
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

	if (*len == 0)
		*len = strlen(p); /* total utf8 length given by strlen */

	return p;
}

DECLSPEC wchar_t* __fastcall
pqbf_get_unicode_text(const char *p, size_t *utf16_len)
{
	wchar_t *obuf;
	int retry;
	size_t len;

	BAILWITHVALUEIFNULL(p, NULL);

	if (*utf16_len == 0)
		len = strlen(p); /* total utf8 length given by strlen */
	else
		len = *utf16_len; /* total utf8 length given as parameter */

	/* allocate enough room to hold standard utf16 text (2 bytes per char) */
	*utf16_len = 2 * len + 1;
	obuf = (wchar_t *) malloc(*utf16_len);
	if (obuf == NULL)
	{
		*utf16_len = 0;
		return NULL;
  }
	
	/* decode UTF-8 as UTF-16 */

	for (retry = 0; retry < 2; retry++)
	{
		*utf16_len = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, p, len, obuf, *utf16_len);

		if (*utf16_len == 0)
		{
			if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
			{
				wchar_t *new_obuf;

				/* in case of non-BMP unicode we need 4 bytes per char in utf-16 */
				*utf16_len = 4 * len + 1;
				new_obuf = (wchar_t *) realloc(obuf, *utf16_len);

				if (new_obuf == NULL)
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

#define pqbf_encode_text(s,t) do { appendPQExpBufferStr(s, t); } while(0)

DECLSPEC void __fastcall
pqbf_set_text(PQExpBuffer s, const char *t)
{
	BAILIFNULL(s);
	pqbf_encode_text(s, t);
}

DECLSPEC void __fastcall
pqbf_add_text(pqparam_buffer *pb, const char *t)
{
	size_t len;

	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	len = pb->payload->len; /* save current length of payload */

	pqbf_encode_text(pb->payload, t);

	pqpb_add(pb, TEXTOID, pb->payload->len - len);
}

/*
 * https://msdn.microsoft.com/en-us/library/dd374081.aspx
 * windows utf-16 strings
 */

#define pqbf_encode_unicode_text(s,t) \
	do {               \
		int utf16_len;   \
		int utf8_len;    \
		char *obuf;      \
		                 \
		utf16_len = wcslen(t); /* get number of characters in utf-16 string */  \
		\
		/* allocate enough room to hold standard utf8 text */             \
		/* utf-8 requires 8, 16, 24, or 32 bits for a single character */ \
		utf8_len = utf16_len * 4 + 1;                                     \
		\
		obuf = (char *) malloc(utf8_len); \
		if (obuf == NULL)	return;         \
		\
		/* encode UTF-16 as UTF-8 */ \
		utf8_len = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, t, -1, obuf, utf8_len, NULL, NULL); \
		if (utf8_len == 0) { free(obuf); return; } \
		\
		appendPQExpBufferStr(s, obuf); \
		free(obuf); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_unicode_text(PQExpBuffer s, const wchar_t *t)
{
	if (s == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;
	pqbf_encode_unicode_text(s, t);
}

DECLSPEC void __fastcall
pqbf_add_unicode_text(pqparam_buffer *pb, const wchar_t *t)
{
	size_t len;
	
	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	len = pb->payload->len; /* save current length of payload */

	pqbf_encode_unicode_text(pb->payload, t);

	pqpb_add(pb, TEXTOID, pb->payload->len - len);
}

/*
 * oid 26: oid
 *
 * see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
 */
DECLSPEC uint32_t __fastcall
pqbf_get_oid(const char *p)
{
	BAILWITHVALUEIFNULL(p, UINT32_MAX);
	return BYTESWAP4(*((uint32_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_oid(PQExpBuffer s, uint32_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, i);
}

DECLSPEC void __fastcall
pqbf_add_oid(pqparam_buffer *pb, uint32_t i)
{
	BAILIFNULL(pb);

	pqbf_encode_int4(pb->payload, i);

	pqpb_add(pb, OIDOID, sizeof(i));
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
	swap.i = BYTESWAP4(*((uint32_t*)p));
	return swap.f;
}


#define pqbf_encode_float4(s,f) \
	do { \
		union float_swap swap; \
		swap.f = f; \
		swap.i = BYTESWAP4(swap.i); \
		appendBinaryPQExpBuffer(s, (const char*) &swap.i, sizeof(swap.i)); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_float4(PQExpBuffer s, float f)
{
	BAILIFNULL(s);
	pqbf_encode_float4(s,f);
}

DECLSPEC void __fastcall
pqbf_add_float4(pqparam_buffer *pb, float f)
{
	BAILIFNULL(pb);

	/* encode float4 */
	pqbf_encode_float4(pb->payload, f);

	pqpb_add(pb, FLOAT4OID, 4);
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
	swap.i = BYTESWAP8(*((uint64_t*)p));
	return swap.f;
}

#define pqbf_encode_float8(s,f) \
	do { \
		union double_swap swap; \
		swap.f = f; \
		swap.i = BYTESWAP8(swap.i); \
		appendBinaryPQExpBuffer(s, (const char*) &swap.i, sizeof(swap.i)); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_float8(PQExpBuffer s, double f)
{
	BAILIFNULL(s);
	pqbf_encode_float8(s,f);
}

DECLSPEC void __fastcall
pqbf_add_float8(pqparam_buffer *pb, double f)
{
	BAILIFNULL(pb);

	/* encode float8 */
	pqbf_encode_float8(pb->payload, f);

	pqpb_add(pb, FLOAT8OID, 8);
}


/*
 * oid 1082: date
 */
DECLSPEC int32_t __fastcall
pqbf_get_date(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return BYTESWAP4(*((uint32_t *) p));
}

DECLSPEC void __fastcall
pqbf_set_date(PQExpBuffer s, int32_t t)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, t);
}

DECLSPEC void __fastcall
pqbf_add_date(pqparam_buffer *pb, int32_t t)
{
	BAILIFNULL(pb);

	pqbf_encode_int4(pb->payload, t);

	pqpb_add(pb, DATEOID, sizeof(t));
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
	i = BYTESWAP8( *( (uint64_t *)p ) );
		
	return POSTGRES_EPOCH_DATE + (int64_t) (i * POSTGRES_TICKS_PER_MILLISECOND);
}

#define pqbf_encode_time(s,t) \
	do { \
		t -= POSTGRES_EPOCH_DATE; \
		t *= POSTGRES_MEGA; \
		t = BYTESWAP8(t); \
		appendBinaryPQExpBuffer(s, (const char*) &t, sizeof(t)); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_time(PQExpBuffer s, time_t t)
{
	BAILIFNULL(s);
	pqbf_encode_time(s, t);
}

DECLSPEC void __fastcall
pqbf_add_time(pqparam_buffer *pb, time_t t)
{
	BAILIFNULL(pb);

	pqbf_encode_time(pb->payload, t);

	pqpb_add(pb, TIMEOID, sizeof(t));
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
	i = BYTESWAP8( *( (uint64_t *)p ) );
		
	*sec = POSTGRES_EPOCH_DATE + (int64_t) (i / POSTGRES_MEGA);
	*usec = i % POSTGRES_MEGA;
}

#define pqbf_encode_timestamp(s,sec,usec) \
	do { \
		sec -= POSTGRES_EPOCH_DATE; \
		sec *= POSTGRES_MEGA; \
		sec = BYTESWAP8((uint64_t) (sec + usec)); \
		appendBinaryPQExpBuffer(s, (const char*) &sec, sizeof(sec)); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_timestamp(PQExpBuffer s, time_t sec, int usec)
{
	BAILIFNULL(s);
	pqbf_encode_timestamp(s, sec, usec);
}

DECLSPEC void __fastcall
pqbf_add_timestamp(pqparam_buffer *pb, time_t sec, int usec)
{
	BAILIFNULL(pb);

	pqbf_encode_timestamp(pb->payload, sec, usec);

	pqpb_add(pb, TIMESTAMPOID, sizeof(sec));
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
	i = BYTESWAP8( *( (uint64_t *)p ) );
		
	*sec = POSTGRES_EPOCH_DATE + (int64_t) (i / POSTGRES_MEGA);
	*usec = i % POSTGRES_MEGA;
}

DECLSPEC void __fastcall
pqbf_set_timestamptz(PQExpBuffer s, time_t sec, int usec)
{
	BAILIFNULL(s);
	pqbf_encode_timestamp(s, sec, usec);
}

DECLSPEC void __fastcall
pqbf_add_timestamptz(pqparam_buffer *pb, time_t sec, int usec)
{
	BAILIFNULL(pb);

	pqbf_encode_timestamp(pb->payload, sec, usec);

	pqpb_add(pb, TIMESTAMPTZOID, sizeof(sec));
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
	*offset =  BYTESWAP8(*((uint64_t*)p));
	p += sizeof(*offset);

	*day = BYTESWAP4(*((uint32_t*)p));
	p += sizeof(*day);

	*month = BYTESWAP4(*((uint32_t*)p));
}

#define pqbf_encode_interval(s,offset,day,month) \
	do { \
		offset = BYTESWAP8(offset); \
		appendBinaryPQExpBuffer(s, (const char*) &offset, sizeof(offset)); \
		day = BYTESWAP4(day); \
		appendBinaryPQExpBuffer(s, (const char*) &day, sizeof(day)); \
		month = BYTESWAP4(month); \
		appendBinaryPQExpBuffer(s, (const char*) &month, sizeof(month)); \
	} while(0)

DECLSPEC void __fastcall
pqbf_set_interval(PQExpBuffer s, int64_t offset, int32_t day, int32_t month)
{
	BAILIFNULL(s);
	pqbf_encode_interval(s,offset,day,month);
}

DECLSPEC void __fastcall
pqbf_add_interval(pqparam_buffer *pb, int64_t offset, int32_t day, int32_t month)
{
	BAILIFNULL(pb);

	pqbf_encode_interval(pb->payload, offset, day, month);

	pqpb_add(pb, INTERVALOID, 16);
}

/*
 * oid 1266: timetz
 *
 * TODO: adapt to local timezone
 */

DECLSPEC time_t __fastcall
pqbf_get_timetz(const char *p)
{
	// TODO
	BAILWITHVALUEIFNULL(p, 0);

	return 0;
}

DECLSPEC void __fastcall
pqbf_set_timetz(PQExpBuffer s, time_t t)
{
	// TODO
	BAILIFNULL(s);
	pqbf_encode_int8(s, t);
}

DECLSPEC void __fastcall
pqbf_add_timetz(pqparam_buffer *pb, time_t t)
{
	BAILIFNULL(pb);

	pqbf_encode_int8(pb->payload, t);

	pqpb_add(pb, INTERVALOID, sizeof(t));
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
	BAILIFNULL(pb);

	appendPQExpBufferChar(pb->payload, (char) b & 0x1);

	pqpb_add(pb, BITOID, sizeof(char));
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