#include <stdlib.h>
#include <string.h>

#define DLL_EXPORT
#include "pqbincopy.h"
#include "pqbinfmt.h"

static const char BinarySignature[11] = "PGCOPY\n\377\r\n\0";
static const char NullValue[4] = "\377\377\377\377";


DECLSPEC pqcopy_buffer * __fastcall
pqcb_create(PGconn *conn, int num_cols)
{
	uint32_t i = 0;
	pqcopy_buffer *buf;

	BAILWITHVALUEIFNULL(conn, NULL);

	if (num_cols <= 0)
	{
		return NULL;
	}

	buf = (pqcopy_buffer *) malloc(sizeof(pqcopy_buffer));

	if (buf)
	{
		buf->conn = conn;
		pqcb_reset(buf, num_cols);
	}

	return buf;
}


DECLSPEC void __fastcall
pqcb_free(pqcopy_buffer *p)
{
	if (p)
	{
		free(p);
	}
}


DECLSPEC void __fastcall
pqcb_reset(pqcopy_buffer *buf, int num_cols)
{
	if (buf && num_cols > 0)
	{
		uint32_t i = 0;

		buf->num_cols = num_cols;
		buf->pos_cols = -1; /* no column added yet, next call to pqbc_put_col will add tuple length field first */

		/* The file header consists of 15 bytes of fixed fields,
		 * followed by a variable-length header extension area.
		 * The fixed fields are:
		 */

		/* Signature: 11-byte sequence PGCOPY\n\377\r\n\0 */
		memcpy(buf->payload, BinarySignature, 11);
		/* Flags field: 32-bit integer bit mask to denote important aspects of the file format (always 0 for now) */
		memcpy(&buf->payload[11], &i, sizeof(i));
		/* Header extension: 32-bit integer, length in bytes of remainder of header, not including self (no extension for now) */
		memcpy(&buf->payload[15], &i, sizeof(i));

		buf->len = 19;
	}
}


#define PQCOPYFLUSH(p, ret) \
	do { \
		if (p->len >= PQBUFSIZ) { \
			ret = PQputCopyData(p->conn, p->payload, PQBUFSIZ); \
			if (ret == 0)	p->len = 0; \
		} \
	} while(0)


/* add len bytes starting from v to p->payload.
 * flushes buffer if p->payload is full during copying
 */
int __fastcall
pqcb_put_buf(pqcopy_buffer *p, char* v, uint32_t len)
{
	int copied = 0;
	int ret = 0;

	do
	{
		int free = PQBUFSIZ - p->len;

		if (free >= len)
		{
			/* buffer can hold all of v */
			memcpy(&p->payload[p->len], v, len);
			p->len += len;
			return 0;
		}
		else if (free > 0)
		{
			/* exactly free bytes left */
			memcpy(&p->payload[p->len], v, free);
			p->len += free;
			v += free;
			copied += free;
		}

		PQCOPYFLUSH(p, ret);

	} while (ret == 0 && copied < len);

	return ret;
}


/* add val to pqcopy_buffer, potentially flushing */
DECLSPEC int __fastcall
pqcb_put_col(pqcopy_buffer *p, const char *val, uint32_t len)
{
	int ret;
	char *v;
	int16_t tuple_len;
	int32_t col_len;

	BAILWITHVALUEIFNULL(p, -1);

	/* invalid pqcopy_buffer? */
	if (p->pos_cols == -2)
	{
		return -1;
	}

	/* start of new tuple?
	 * Each tuple begins with a 16-bit integer count of the number of fields in the tuple.
	 * (Presently, all tuples in a table will have the same count, but that might not always be true.)
	 */

	if (p->pos_cols == -1 || p->pos_cols >= p->num_cols)
	{
		tuple_len = BYTESWAP2(p->num_cols);
		v = (char*) &tuple_len;

		/* add tuple length to buffer / flush */
		ret = pqcb_put_buf(p, v, sizeof(tuple_len));
		if (ret != 0) return ret;

		p->pos_cols = 0;
	}

	/* start of new column
	 * Then, repeated for each field in the tuple, there is a 32-bit length word followed by that many bytes of field data.
	 * (The length word does not include itself, and can be zero.)
	 * As a special case, -1 indicates a NULL field value. No value bytes follow in the NULL case.
	 */

	if (val == NULL && len > 0)
	{
		len = 0; /* NULL value, ignore field value */
		v = (char*) NullValue;
	}
	else
	{
		/* len >= 0, we might want to copy empty strings or bytea */
		col_len = BYTESWAP4(len);
		v = (char*) &col_len;
	}

	/* add field length to buffer / flush */
	ret = pqcb_put_buf(p, v, sizeof(col_len));
	if (ret != 0) return ret;

	/* potentially add field value to buffer / flush */
	if (len > 0)
	{
		v = (char*) val;
		ret = pqcb_put_buf(p, v, len);
		if (ret != 0) return ret;
	}

	p->pos_cols++;

	return ret;
}


/* flush pqcopy_buffer and send trailer */
DECLSPEC int __fastcall
pqcb_put_end(pqcopy_buffer *p)
{
	BAILWITHVALUEIFNULL(p, -1);

	/* invalid pqcopy_buffer? */
	if (p->pos_cols == -2)
	{
		return -1;
	}

	if (p->len > 0)
	{
		/* flush remaining buffer */
		int ret = PQputCopyData(p->conn, p->payload, p->len);
		if (ret != 0)	return ret;
		p->len = 0;
		p->pos_cols = -2; /* marks pqcopy_buffer as invalid */
	}

	return PQputCopyEnd(p->conn, NULL);
}