#include <stdio.h>
#include <stdlib.h>

#define DLL_EXPORT
#include "pqparam_buffer.h"

#ifdef  __cplusplus
extern "C" {
#endif


DECLSPEC pqparam_buffer * __fastcall
pqpb_create(void)
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
pqpb_free(pqparam_buffer *b)
{
	if (b)
	{
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
pqpb_reset(pqparam_buffer *b)
{
	if (b)
	{
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
pqpb_add(pqparam_buffer *b, Oid typ, const char *val, int len)
{
	int ret = 0;

	REMALLOC(Oid, b->param_typ, b->num_param, ret);
	if (b->param_typ == NULL || ret == -1) return;
	b->param_typ[b->num_param] = typ; // OID of type
	
	REMALLOC(char*, b->param_val, b->num_param, ret);
	if (b->param_typ == NULL || ret == -1) return;
	b->param_val[b->num_param] = (char*) val; // pointer to beginning of data in payload

	REMALLOC(int, b->param_len, b->num_param, ret);
	if (b->param_typ == NULL || ret == -1) return;
	b->param_len[b->num_param] = len; // data length

	REMALLOC(int, b->param_fmt, b->num_param, ret);
	if (b->param_typ == NULL || ret == -1) return;
	b->param_fmt[b->num_param] = 1; // binary format

	b->num_param++;
}

DECLSPEC int __fastcall
pqpb_get_num(pqparam_buffer *b)
{
	if (b)
	{
		return b->num_param;
	}

	return -1;
}

DECLSPEC Oid * __fastcall
pqpb_get_types(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_typ;
	}

	return NULL;
}

DECLSPEC char ** __fastcall
pqpb_get_vals(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_val;
	}

	return NULL;
}

DECLSPEC int * __fastcall
pqpb_get_lens(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_len;
	}

	return NULL;
}

DECLSPEC int * __fastcall
pqpb_get_frms(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_fmt;
	}

	return NULL;
}

DECLSPEC uint32_t __fastcall
pqpb_get_type(pqparam_buffer *b, int i)
{
	if (b && i >= 0 && i < b->num_param)
	{
		return b->param_typ[i];
	}

	return UINT32_MAX;
}

DECLSPEC char * __fastcall
pqpb_get_val(pqparam_buffer *b, int i)
{
	if (b && i >= 0 && i < b->num_param)
	{
		return b->param_val[i];
	}

	return NULL;
}

DECLSPEC int __fastcall
pqpb_get_len(pqparam_buffer *b, int i)
{
	if (b && i >= 0 && i < b->num_param)
	{
		return b->param_len[i];
	}

	return INT32_MIN;
}


#ifdef  __cplusplus
}
#endif