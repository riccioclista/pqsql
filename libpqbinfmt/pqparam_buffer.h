#ifndef __PQPARAM_BUFFER_H
#define __PQPARAM_BUFFER_H

/*
 * Oid datatype
 */
#include <postgres_ext.h>

/*
 * PQExpBuffer
 */
#include "pqexpbuffer.h"

#if defined DLL_EXPORT
#define DECLSPEC __declspec(dllexport)
#else
#define DECLSPEC __declspec(dllimport)
#endif

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
	char **param_val;
	int   *param_len;
	int   *param_fmt;
} pqparam_buffer;

extern DECLSPEC pqparam_buffer * __fastcall pqpb_create(void);
extern DECLSPEC void __fastcall pqpb_free(pqparam_buffer *p);
extern DECLSPEC void __fastcall pqpb_reset(pqparam_buffer *p);

extern void __fastcall pqpb_add(pqparam_buffer *buf, Oid typ, const char *val, size_t len);

extern DECLSPEC int __fastcall pqpb_get_num(pqparam_buffer *p);
extern DECLSPEC Oid * __fastcall pqpb_get_types(pqparam_buffer *p);
extern DECLSPEC char ** __fastcall pqpb_get_vals(pqparam_buffer *p);
extern DECLSPEC int * __fastcall pqpb_get_lens(pqparam_buffer *p);
extern DECLSPEC int * __fastcall pqpb_get_frms(pqparam_buffer *p);

extern DECLSPEC Oid __fastcall pqpb_get_type(void *p, int i);
extern DECLSPEC char * __fastcall pqpb_get_val(void *p, int i);
extern DECLSPEC int __fastcall pqpb_get_len(void *p, int i);

#ifdef  __cplusplus
}
#endif

#endif /* __PQPARAM_BUFFER_H */