/**
 * @file pqparam_buffer.c
 * @brief binary input parameter format handling for PQexecParams()
 * @date 2015-09-30 
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 */

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>

#define DLL_EXPORT
#include "pqbinfmt_config.h"
#include "pqparam_buffer.h"

#ifdef  __cplusplus
extern "C" {
#endif


DECLSPEC pqparam_buffer *
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
		b->param_dif = NULL;
		b->param_len = NULL;
		b->param_fmt = NULL;
		b->param_vals = NULL;
	}

	return b;
}

#define XFREE(p) do { if(p) { free(p); p = NULL; } } while(0)

DECLSPEC void
pqpb_free(pqparam_buffer *b)
{
	if (b)
	{
		b->num_param = 0;
		destroyPQExpBuffer(b->payload);

		XFREE(b->param_typ);
		XFREE(b->param_dif);
		XFREE(b->param_len);
		XFREE(b->param_fmt);
		XFREE(b->param_vals);
		XFREE(b);
	}
}

DECLSPEC void
pqpb_reset(pqparam_buffer *b)
{
	if (b)
	{
		b->num_param = 0;
		resetPQExpBuffer(b->payload);

		XFREE(b->param_typ);
		XFREE(b->param_dif);
		XFREE(b->param_len);
		XFREE(b->param_fmt);
		XFREE(b->param_vals);
	}
}


#define REMALLOC(type, ptr, n, ret) \
	do { \
		if (ptr) { \
			type *newptr = (type*) realloc(ptr, (n + 1) * sizeof(type)); \
			if (newptr != NULL) { \
				ptr = newptr ; \
			} else { \
				ret = -1; \
			} \
		}	else { \
			ptr = (type*) malloc(sizeof(type)); \
		} } while(0)


void
pqpb_add(pqparam_buffer *b, Oid typ, size_t len)
{
	int ret = 0;

	/* bail out in case param_vals is fixed */
	if (b->param_vals != NULL)
		return;

	/* OID of type */
	REMALLOC(Oid, b->param_typ, b->num_param, ret);
	if (b->param_typ == NULL || ret == -1) return;
	b->param_typ[b->num_param] = typ;
	
	/* byte offset from b->payload->data to start of parameter value */
	REMALLOC(ptrdiff_t, b->param_dif, b->num_param, ret);
	if (b->param_dif == NULL || ret == -1) return;
	b->param_dif[b->num_param] = b->payload->len - len;

	/* data length */
	REMALLOC(int, b->param_len, b->num_param, ret);
	if (b->param_len == NULL || ret == -1) return;
	b->param_len[b->num_param] = len;

	REMALLOC(int, b->param_fmt, b->num_param, ret);
	if (b->param_fmt == NULL || ret == -1) return;
	b->param_fmt[b->num_param] = 1; /* binary format */

	b->num_param++;
}

DECLSPEC int
pqpb_get_num(pqparam_buffer *b)
{
	if (b)
	{
		return b->num_param;
	}

	return -1;
}

DECLSPEC Oid *
pqpb_get_types(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_typ;
	}

	return NULL;
}

DECLSPEC char **
pqpb_get_vals(pqparam_buffer *b)
{
	if (b)
	{
		if (b->param_vals == NULL && b->num_param > 0)
		{
			int i;
			b->param_vals = (char**) malloc(b->num_param * sizeof(char*));

			if (b->param_vals == NULL)
				return NULL;

			/* set parameter value start to difference from start of payload data
			 * we can only do this after PQExpBuffer is fixed, i.e., no realloc()
			 * will be called on b->payload->data anymore
			 */
			for (i = 0; i < b->num_param; i++)
			{
				if (b->param_len[i] > 0)
				{
					b->param_vals[i] = b->payload->data + b->param_dif[i];
				}
				else
				{
					b->param_vals[i] = NULL; // NULL value
				}
			}
		}

		return b->param_vals;
	}

	return NULL;
}

DECLSPEC int *
pqpb_get_lens(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_len;
	}

	return NULL;
}

DECLSPEC int *
pqpb_get_frms(pqparam_buffer *b)
{
	if (b)
	{
		return b->param_fmt;
	}

	return NULL;
}

DECLSPEC uint32_t
pqpb_get_type(pqparam_buffer *b, int i)
{
	if (b && i >= 0 && i < b->num_param)
	{
		return b->param_typ[i];
	}

	return UINT32_MAX;
}

DECLSPEC char *
pqpb_get_val(pqparam_buffer *b, int i)
{
	if (b && i >= 0 && i < b->num_param)
	{
		char **vals = pqpb_get_vals(b);
		return vals[i];
	}

	return NULL;
}

DECLSPEC int
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