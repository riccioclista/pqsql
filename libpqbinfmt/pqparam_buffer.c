#include <stdio.h>
#include <stdlib.h>

#define DLL_EXPORT
#include "pqparam_buffer.h"

#ifdef  __cplusplus
extern "C" {
#endif


DECLSPEC void * __fastcall
create_parameter_buffer(void)
{
	pqparam_buffer *b = (pqparam_buffer*) malloc(sizeof(pqparam_buffer));

	if (b)
	{
		b->payload = createPQExpBuffer();
		if (b->payload == NULL)
		{
			free(b);
			return NULL;
		}

		b->num_param = 0;
		b->param_typ = NULL;
		b->param_val = NULL;
		b->param_len = NULL;
		b->param_fmt = NULL;
	}

	return b;
}

#define XFREE(p) do { if(p) { free(p); p = NULL; } } while(0)

DECLSPEC void __fastcall
free_parameter_buffer(void *p)
{
	if (p)
	{
		pqparam_buffer *b = (pqparam_buffer*) p;

		b->num_param = 0;
		destroyPQExpBuffer(b->payload);

		XFREE(b->param_typ);
		XFREE(b->param_val);
		XFREE(b->param_len);
		XFREE(b->param_fmt);

		XFREE(b);
	}
}

DECLSPEC void __fastcall
reset_parameter_buffer(void *p)
{
	if (p)
	{
		pqparam_buffer *b = (pqparam_buffer*) p;

		b->num_param = 0;
		resetPQExpBuffer(b->payload);

		XFREE(b->param_typ);
		XFREE(b->param_val);
		XFREE(b->param_len);
		XFREE(b->param_fmt);
	}
}


#define REMALLOC(type, ptr, n, ret) \
	do { \
		if (ptr) { \
			type *newptr = (type*) realloc(ptr, n * sizeof(type)); \
			if (newptr != NULL) { \
				ptr = newptr ; \
			} else { \
				ret = -1; \
			} \
		}	else { \
			ptr = (type*) malloc(sizeof(type)); \
		} } while(0)


void __fastcall
add_parameter_buffer(pqparam_buffer *buf, Oid typ, const char *val, int len)
{
	int ret = 0;

	REMALLOC(Oid, buf->param_typ, buf->num_param, ret);
	if (buf->param_typ == NULL || ret == -1) return;
	buf->param_typ[buf->num_param] = typ; // OID of type
	
	REMALLOC(char*, buf->param_val, buf->num_param, ret);
	if (buf->param_typ == NULL || ret == -1) return;
	buf->param_val[buf->num_param] = (char*) val; // pointer to beginning of data in payload

	REMALLOC(int, buf->param_len, buf->num_param, ret);
	if (buf->param_typ == NULL || ret == -1) return;
	buf->param_len[buf->num_param] = len; // data length

	REMALLOC(int, buf->param_fmt, buf->num_param, ret);
	if (buf->param_typ == NULL || ret == -1) return;
	buf->param_fmt[buf->num_param] = 1; // binary format

	buf->num_param++;
}

DECLSPEC int __fastcall
get_num_param(void *p)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf)
	{
		return buf->num_param;
	}

	return -1;
}

DECLSPEC void * __fastcall
get_param_types(void *p)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf)
	{
		return buf->param_typ;
	}

	return NULL;
}

DECLSPEC void * __fastcall
get_param_vals(void *p)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf)
	{
		return buf->param_val;
	}

	return NULL;
}

DECLSPEC void * __fastcall
get_param_lens(void *p)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf)
	{
		return buf->param_len;
	}

	return NULL;
}

DECLSPEC void * __fastcall
get_param_frms(void *p)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf)
	{
		return buf->param_fmt;
	}

	return NULL;
}

DECLSPEC uint32_t __fastcall
get_param_type(void *p, int i)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf && i >= 0 && i < buf->num_param)
	{
		return buf->param_typ[i];
	}

	return UINT32_MAX;
}

DECLSPEC void * __fastcall
get_param_val(void *p, int i)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf && i >= 0 && i < buf->num_param)
	{
		return buf->param_val[i];
	}

	return NULL;
}

DECLSPEC int __fastcall
get_param_len(void *p, int i)
{
	pqparam_buffer *buf = (pqparam_buffer*) p;

	if (buf && i >= 0 && i < buf->num_param)
	{
		return buf->param_len[i];
	}

	return INT32_MIN;
}


#ifdef  __cplusplus
}
#endif