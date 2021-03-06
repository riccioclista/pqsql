/**
 * @file pqparam_buffer.h
 * @brief binary input parameter format handling for PQexecParams()
 * @date 2015-09-30 
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 */

#ifndef __PQPARAM_BUFFER_H
#define __PQPARAM_BUFFER_H

#include <stddef.h>

/*
 * Oid datatype
 */
#include <postgres_ext.h>

/*
 * PQExpBuffer
 */
#include "pgadt/pqexpbuffer.h"

#include "pqbinfmt_config.h"

#ifdef  __cplusplus
extern "C" {
#endif

/*
 * data structure encapsulating exec parameters
 */
typedef struct pqparam_buffer
{
	size_t num_param;
	PQExpBuffer payload;
	Oid   *param_typ;
	ptrdiff_t *param_dif;
	int   *param_len;
	int   *param_fmt;
	char **param_vals;
} pqparam_buffer;

extern DECLSPEC pqparam_buffer * pqpb_create(void);
extern DECLSPEC void pqpb_free(pqparam_buffer *p);
extern DECLSPEC void pqpb_reset(pqparam_buffer *p);

extern void pqpb_add(pqparam_buffer *buf, Oid typ, size_t len);

extern DECLSPEC int pqpb_get_num(pqparam_buffer *p);
extern DECLSPEC Oid * pqpb_get_types(pqparam_buffer *p);
extern DECLSPEC char ** pqpb_get_vals(pqparam_buffer *p);
extern DECLSPEC int * pqpb_get_lens(pqparam_buffer *p);
extern DECLSPEC int * pqpb_get_frms(pqparam_buffer *p);

extern DECLSPEC Oid pqpb_get_type(pqparam_buffer *p, int i);
extern DECLSPEC char * pqpb_get_val(pqparam_buffer *p, int i);
extern DECLSPEC int pqpb_get_len(pqparam_buffer *p, int i);

#ifdef  __cplusplus
}
#endif

#endif /* __PQPARAM_BUFFER_H */