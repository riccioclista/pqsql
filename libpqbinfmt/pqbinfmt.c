/**
 * @file pqbinfmt.c
 * @brief encode/decode postgresql binary format to native datatype for PQgetvalue() and pqparam_buffer
 * @date 2015-09-30 
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 * @note postgresql source src/backend/libpq/pqformat.c
 * @note postgresql source src/backend/utils/adt/timestamp.c
 * 
 * @todo bit					src/backend/utils/adt/varbit.c
 * @todo "char"				src/backend/utils/adt/char.c
 * @todo cidr					src/backend/utils/adt/network.c
 * @todo inet					src/backend/utils/adt/network.c
 * @todo macaddr			src/backend/utils/adt/mac.c
 * @todo uuid					src/backend/utils/adt/uuid.c
 * @todo xid					src/backend/utils/adt/xid.c
 */

#ifdef _WIN32
#include <windows.h>
#endif /* _WIN32 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <float.h>

/* oid values */
#include "pgadt/pg_type.h"


#define DLL_EXPORT
#include "pqbinfmt.h"
#include "pqparam_buffer.h"
#include "pgadt/numeric.h"
#include "pgadt/builtins.h"


DECLSPEC size_t
pqbf_get_buflen(PQExpBuffer s)
{
	return s->len;
}

DECLSPEC char *
pqbf_get_bufval(PQExpBuffer s)
{
	return s->data;
}

/*
 * add NULL value parameter of specified type
 */
DECLSPEC void
pqbf_add_null(pqparam_buffer *pb, uint32_t oid)
{
	BAILIFNULL(pb);
	pqpb_add(pb, oid, 0);
}

/*
 * get single byte from result
 */
DECLSPEC unsigned char
pqbf_get_byte(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return *p;
}

/*
 * oid 16: bool
 */
DECLSPEC int
pqbf_get_bool(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p);
}

inline void
pqbf_encode_bool(PQExpBuffer s, int b)
{
	appendPQExpBufferChar(s, (char) b & 0x1);
}

DECLSPEC void
pqbf_set_bool(PQExpBuffer s, int b)
{
	BAILIFNULL(s);
	pqbf_encode_bool(s, b);
}

DECLSPEC void
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
DECLSPEC void
pqbf_get_bytea(const char *p, char* buf, size_t len)
{
	BAILIFNULL(p);
	BAILIFNULL(buf);

	/* copy len bytes from p to buf */
	memcpy(buf, p, len);
}

inline void
pqbf_encode_bytea(PQExpBuffer s, const char* buf, size_t buflen)
{
	appendBinaryPQExpBuffer(s, buf, buflen);
}

DECLSPEC void
pqbf_set_bytea(PQExpBuffer s, const char* buf, size_t buflen)
{
	BAILIFNULL(s);
	pqbf_encode_bytea(s, buf, buflen);
}

DECLSPEC void
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
DECLSPEC int64_t
pqbf_get_int8(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT64_MIN);
	return BYTESWAP8(*((uint64_t *) p));
}

inline void
pqbf_encode_int8(PQExpBuffer s, int64_t i)
{
	i = BYTESWAP8(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));
}

DECLSPEC void
pqbf_set_int8(PQExpBuffer s, int64_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int8(s, i);
}

DECLSPEC void
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

DECLSPEC int16_t
pqbf_get_int2(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT16_MIN);
	return BYTESWAP2(*((uint16_t *) p));
}

inline void
pqbf_encode_int2(PQExpBuffer s, int16_t i)
{
	i = BYTESWAP2(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));
}

DECLSPEC void
pqbf_set_int2(PQExpBuffer s, int16_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int2(s, i);
}

DECLSPEC void
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

DECLSPEC int32_t
pqbf_get_int4(const char *p)
{
	BAILWITHVALUEIFNULL(p, INT32_MIN);
	return BYTESWAP4(*((uint32_t *) p));
}

inline void
pqbf_encode_int4(PQExpBuffer s, int32_t i)
{
	i = BYTESWAP4(i);
	appendBinaryPQExpBuffer(s, (const char*) &i, sizeof(i));
}

DECLSPEC void
pqbf_set_int4(PQExpBuffer s, int32_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, i);
}

DECLSPEC void
pqbf_add_int4(pqparam_buffer *pb, int32_t i)
{
	BAILIFNULL(pb);

	/* encode integer in network order */
	pqbf_encode_int4(pb->payload, i);

	pqpb_add(pb, INT4OID, sizeof(i));
}

/*
 * oid 19:   name
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

DECLSPEC const char*
pqbf_get_text(const char *p, size_t *len)
{
	BAILWITHVALUEIFNULL(p, NULL);

	if (len && *len == 0)
		*len = strlen(p); /* total utf8 length given by strlen */

	return p;
}

/*
 * standard utf-8 string
 */

inline void
pqbf_encode_text(PQExpBuffer s, const char *t)
{
	appendPQExpBufferStr(s, t);
}

DECLSPEC void
pqbf_set_text(PQExpBuffer s, const char *t)
{
	BAILIFNULL(s);
	pqbf_encode_text(s, t);
}

DECLSPEC void
pqbf_add_text(pqparam_buffer *pb, const char *t, uint32_t oid)
{
	size_t len;

	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	len = pb->payload->len; /* save current length of payload */

	pqbf_encode_text(pb->payload, t);

	pqpb_add(pb, oid, pb->payload->len - len);
}


/*
 * https://msdn.microsoft.com/en-us/library/dd374081.aspx
 * windows utf-16 strings
 */
#ifdef _WIN32

DECLSPEC wchar_t*
pqbf_get_unicode_text(const char *p, int32_t *utf16_len)
{
	wchar_t *obuf;
	int32_t num_wchars;
	int32_t utf8_len;
	int32_t is_terminated;

	if (p == NULL || utf16_len == NULL)
		return NULL;

	is_terminated = *utf16_len == 0;
	
	if (is_terminated) /* p might be empty or not */
	{
		/* MultiByteToWideChar terminates with L'\0' */
		utf8_len = -1;
	}
	else /* *utf16_len > 0: we assume that p is a non-NUL-terminated non-empty string of length *utf16_len */
	{
		/* don't terminate with L'\0', utf8_len > 0 */
		utf8_len = *utf16_len;
	}

	/* total UTF-8 length (including NUL byte if utf8_len == -1) as given by MultiByteToWideChar */
	num_wchars = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, p, utf8_len, NULL, 0);
	if (num_wchars <= 0) /* p is invalid, don't do MultiByteToWideChar */
	{
		*utf16_len = 0;
		return NULL;
	}

	/* allocate enough room to hold UTF-16 text */
	obuf = (wchar_t *) malloc(num_wchars * sizeof(wchar_t));
	if (obuf == NULL)
	{
		*utf16_len = 0;
		return NULL;
	}

	/* always return the number of characters in the UTF-16 string */
	if (is_terminated) /* MultiByteToWideChar will NUL terminate the string */
	{
		*utf16_len = num_wchars - 1;
								 
		if (num_wchars == 1) /* p is the empty string, don't do MultiByteToWideChar */
		{
			*obuf = L'\0'; /* we NUL terminate the empty string */
			return obuf;
		}
	}
	else /* MultiByteToWideChar won't NUL terminate the string */
	{
		*utf16_len = num_wchars;
	}

	/* decode UTF-8 as UTF-16 */
	num_wchars = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, p, utf8_len, obuf, num_wchars);
	if (num_wchars == 0) /* conversion failed, ouch! */
	{
		free(obuf);
		*utf16_len = 0;
		return NULL;
	}

	return obuf;
}


DECLSPEC void
pqbf_free_unicode_text(wchar_t *p)
{
	if (p)
	{
		free(p);
	}
}


inline void
pqbf_encode_unicode_text(PQExpBuffer s, const wchar_t *t)
{
	int utf8_len;
	char *obuf;

	/* get number of bytes + NUL byte for the UTF-8 conversion */
	utf8_len = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, t, -1, NULL, 0, 0, 0);

	if (utf8_len < 2) /* empty string (1), or invalid input (0): nothing to append */
		return;

	obuf = (char *) malloc(utf8_len);
	if (obuf == NULL)
		return;

	/* encode UTF-16 as UTF-8 */
	utf8_len = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, t, -1, obuf, utf8_len, NULL, NULL);
	if (utf8_len > 0)
	{
		/* WideCharToMultiByte will NUL terminate obuf for us */
		appendPQExpBufferStr(s, obuf);
	}

	free(obuf);
}

DECLSPEC void
pqbf_set_unicode_text(PQExpBuffer s, const wchar_t *t)
{
	if (s == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;
	pqbf_encode_unicode_text(s, t);
}

DECLSPEC void
pqbf_add_unicode_text(pqparam_buffer *pb, const wchar_t *t, uint32_t oid)
{
	size_t len;
	
	if (pb == NULL || t == NULL) /* use pqbf_set_null for NULL parameters */
		return;

	len = pb->payload->len; /* save current length of payload */

	pqbf_encode_unicode_text(pb->payload, t);

	pqpb_add(pb, oid, pb->payload->len - len);
}

#endif /* _WIN32 */


/*
 * oid 26: oid
 *
 * see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
 */

DECLSPEC uint32_t
pqbf_get_oid(const char *p)
{
	BAILWITHVALUEIFNULL(p, UINT32_MAX);
	return BYTESWAP4(*((uint32_t *) p));
}

DECLSPEC void
pqbf_set_oid(PQExpBuffer s, uint32_t i)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, i);
}

DECLSPEC void
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

DECLSPEC float
pqbf_get_float4(const char *p)
{
	union float_swap swap;

	BAILWITHVALUEIFNULL(p, FLT_MIN);

	/* decode float4 */
	swap.i = BYTESWAP4(*((uint32_t*)p));
	return swap.f;
}


inline void
pqbf_encode_float4(PQExpBuffer s, float f)
{
	union float_swap swap;
	swap.f = f;
	swap.i = BYTESWAP4(swap.i);
	appendBinaryPQExpBuffer(s, (const char*) &swap.i, sizeof(swap.i));
}

DECLSPEC void
pqbf_set_float4(PQExpBuffer s, float f)
{
	BAILIFNULL(s);
	pqbf_encode_float4(s,f);
}

DECLSPEC void
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

DECLSPEC double
pqbf_get_float8(const char *p)
{
	union double_swap swap;

	BAILWITHVALUEIFNULL(p, DBL_MIN);

	/* decode float8 */
	swap.i = BYTESWAP8(*((uint64_t*)p));
	return swap.f;
}

inline void
pqbf_encode_float8(PQExpBuffer s, double f)
{
	union double_swap swap;
	swap.f = f;
	swap.i = BYTESWAP8(swap.i);
	appendBinaryPQExpBuffer(s, (const char*)&swap.i, sizeof(swap.i));
}

DECLSPEC void
pqbf_set_float8(PQExpBuffer s, double f)
{
	BAILIFNULL(s);
	pqbf_encode_float8(s,f);
}

DECLSPEC void
pqbf_add_float8(pqparam_buffer *pb, double f)
{
	BAILIFNULL(pb);

	/* encode float8 */
	pqbf_encode_float8(pb->payload, f);

	pqpb_add(pb, FLOAT8OID, 8);
}


/*
 * oid 1082: date
 *
 * TODO check
 */
DECLSPEC int32_t
pqbf_get_date(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return BYTESWAP4(*((uint32_t *) p));
}

DECLSPEC void
pqbf_set_date(PQExpBuffer s, int32_t t)
{
	BAILIFNULL(s);
	pqbf_encode_int4(s, t);
}

DECLSPEC void
pqbf_add_date(pqparam_buffer *pb, int32_t t)
{
	BAILIFNULL(pb);

	pqbf_encode_int4(pb->payload, t);

	pqpb_add(pb, DATEOID, sizeof(t));
}



/*
 * oid 1083: time
 * oid 1266: timetz
 */

/* January 1, 2000, 00:00:00 UTC (in Unix epoch seconds) */
#define POSTGRES_EPOCH_DATE 946684800
#define POSTGRES_MEGA 1000000
#define POSTGRES_TICKS_PER_MILLISECOND 10000

DECLSPEC time_t
pqbf_get_time(const char *p)
{
	uint64_t i;

	BAILWITHVALUEIFNULL(p, 0);

	/* decode 64bit time into unix time */
	i = BYTESWAP8( *( (uint64_t *)p ) );
		
	return POSTGRES_EPOCH_DATE + (int64_t) (i * POSTGRES_TICKS_PER_MILLISECOND);
}

inline void
pqbf_encode_time(PQExpBuffer s, time_t t)
{
	t -= POSTGRES_EPOCH_DATE;
	t *= POSTGRES_MEGA;
	t = BYTESWAP8(t);
	appendBinaryPQExpBuffer(s, (const char*)&t, sizeof(t));
}

DECLSPEC void
pqbf_set_time(PQExpBuffer s, time_t t)
{
	BAILIFNULL(s);
	pqbf_encode_time(s, t);
}

DECLSPEC void
pqbf_add_time(pqparam_buffer *pb, time_t t)
{
	BAILIFNULL(pb);

	pqbf_encode_time(pb->payload, t);

	pqpb_add(pb, TIMEOID, sizeof(t));
}


/*
 * oid 1114: timestamp
 * oid 1184: timestamptz
 */
DECLSPEC void
pqbf_get_timestamp(const char *p, time_t *sec, int *usec)
{
	uint64_t i;

	BAILIFNULL(sec);
	BAILIFNULL(usec);

	if (p == NULL)
	{
		*sec = 0;
		*usec = 0;
		return;
	}

	/* decode 64bit timestamp into sec and usec part */
	i = BYTESWAP8( *( (uint64_t *)p ) );
	
	switch (i)
	{
	case INT64_MAX: // timestamp 'infinity'
	case INT64_MIN: // timestamp '-infinity'
		*sec = i;
		*usec = 0;
		break;
	default:
		*sec = POSTGRES_EPOCH_DATE + (int64_t)(i / POSTGRES_MEGA);
		*usec = i % POSTGRES_MEGA;
		break;
	}
}

inline void
pqbf_encode_timestamp(PQExpBuffer s, time_t sec, int usec)
{
	switch (sec)
	{
	case INT64_MAX: // timestamp 'infinity'
	case INT64_MIN: // timestamp '-infinity'
		sec = BYTESWAP8((uint64_t)sec);
		break;
	default:
		sec -= POSTGRES_EPOCH_DATE;
		sec *= POSTGRES_MEGA;
		sec = BYTESWAP8((uint64_t)(sec + usec));
		break;
	}

	appendBinaryPQExpBuffer(s, (const char*)&sec, sizeof(sec));
}

DECLSPEC void
pqbf_set_timestamp(PQExpBuffer s, time_t sec, int usec)
{
	BAILIFNULL(s);
	pqbf_encode_timestamp(s, sec, usec);
}

DECLSPEC void
pqbf_add_timestamp(pqparam_buffer *pb, time_t sec, int usec, uint32_t oid)
{
	BAILIFNULL(pb);

	pqbf_encode_timestamp(pb->payload, sec, usec);

	pqpb_add(pb, oid, sizeof(sec));
}


/*
 * oid 1186: interval
 *
 * see timestamp_recv() from src/backend/utils/adt/timestamp.c
 */

DECLSPEC void
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

inline void
pqbf_encode_interval(PQExpBuffer s, int64_t offset, int32_t day, int32_t month)
{
	offset = BYTESWAP8(offset);
	appendBinaryPQExpBuffer(s, (const char*) &offset, sizeof(offset));
	day = BYTESWAP4(day);
	appendBinaryPQExpBuffer(s, (const char*) &day, sizeof(day));
	month = BYTESWAP4(month);
	appendBinaryPQExpBuffer(s, (const char*) &month, sizeof(month));
}

DECLSPEC void
pqbf_set_interval(PQExpBuffer s, int64_t offset, int32_t day, int32_t month)
{
	BAILIFNULL(s);
	pqbf_encode_interval(s,offset,day,month);
}

DECLSPEC void
pqbf_add_interval(pqparam_buffer *pb, int64_t offset, int32_t day, int32_t month)
{
	BAILIFNULL(pb);

	pqbf_encode_interval(pb->payload, offset, day, month);

	pqpb_add(pb, INTERVALOID, 16);
}


/*
 * oid 1560: bit
 */
DECLSPEC int
pqbf_get_bit(const char *p)
{
	BAILWITHVALUEIFNULL(p, 0);
	return ((int)*p) & 0x1;
}

DECLSPEC void
pqbf_set_bit(pqparam_buffer *pb, int b)
{
	BAILIFNULL(pb);

	appendPQExpBufferChar(pb->payload, (char) b & 0x1);

	pqpb_add(pb, ZPBITOID, sizeof(char));
}

/*
 * oid 1562: varbit 
 */

// TODO


/*
 * oid 1700: numeric
 *
 * see
 * - numeric_recv()
 * - numeric_send()
 * from
 * - src/backend/utils/adt/numeric.c
 */

DECLSPEC double
pqbf_get_numeric(const char *ptr, int32_t typmod)
{
	Numeric n;
	double ret;

	BAILWITHVALUEIFNULL(ptr, DBL_MIN);

	n = DatumGetNumeric(numeric_recv(ptr, typmod));

	ret = DatumGetFloat8(numeric_float8(n));
	if (n) free(n);

	return ret;
}


DECLSPEC void
pqbf_add_numeric(pqparam_buffer *pb, double d)
{
	size_t len;
	Numeric n;

	BAILIFNULL(pb);

	len = pb->payload->len; /* save current length of payload */

	/* encode double as numeric */
	n = DatumGetNumeric(float8_numeric(d));

	/* encode numeric to binary format */
	numeric_send(pb->payload, n);

	/* free temp numeric */
	if (n) free(n);

	pqpb_add(pb, NUMERICOID, pb->payload->len - len);
}


DECLSPEC void
pqbf_set_numeric(PQExpBuffer s, double d)
{
	Numeric n;

	BAILIFNULL(s);

	/* encode double as numeric */
	n = DatumGetNumeric(float8_numeric(d));

	/* encode numeric to binary format */
	numeric_send(s, n);

	/* free temp numeric */
	if (n) free(n);
}



/*
 * oid 2950: uuid
 */
DECLSPEC void
pqbf_get_uuid(const char *ptr, char *b[])
{
	// TODO
}