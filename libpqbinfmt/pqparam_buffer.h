#ifndef __PQPARAM_BUFFER_H
#define __PQPARAM_BUFFER_H

#include <stdint.h>

/*
 * Oid datatype
 */
#include <postgres_ext.h>

#if defined DLL_EXPORT
#define DECLSPEC __declspec(dllexport)
#else
#define DECLSPEC __declspec(dllimport)
#endif

#ifdef  __cplusplus
extern "C" {
#endif

/*
 * libpq
 * from pqexpbuffer.h
 */
typedef struct PQExpBufferData
{
  char       *data;
  size_t      len;
  size_t      maxlen;
} PQExpBufferData;
typedef PQExpBufferData *PQExpBuffer;

extern PQExpBuffer createPQExpBuffer(void);
extern void destroyPQExpBuffer(PQExpBuffer str);
extern void resetPQExpBuffer(PQExpBuffer str);

extern void initPQExpBuffer(PQExpBuffer str);
extern void termPQExpBuffer(PQExpBuffer str);

extern void appendPQExpBufferStr(PQExpBuffer str, const char *data);
extern void appendPQExpBufferChar(PQExpBuffer str, char ch);
extern void appendBinaryPQExpBuffer(PQExpBuffer str, const char *data, size_t datalen);

/*
 * data structure encapsulating exec parameters
 */
typedef struct pqparam_buffer
{
	PQExpBuffer payload;
	int num_param;
	Oid   *param_typ;
	char **param_val;
	int   *param_len;
	int   *param_fmt;
} pqparam_buffer;

extern DECLSPEC void * __fastcall create_parameter_buffer(void);
extern DECLSPEC void __fastcall free_parameter_buffer(void *p);
extern DECLSPEC void __fastcall reset_parameter_buffer(void *p);

extern void __fastcall add_parameter_buffer(pqparam_buffer *buf, Oid typ, const char *val, int len);

extern DECLSPEC int __fastcall get_num_param(void *p);
extern DECLSPEC void * __fastcall get_param_types(void *p);
extern DECLSPEC void * __fastcall get_param_vals(void *p);
extern DECLSPEC void * __fastcall get_param_lens(void *p);
extern DECLSPEC void * __fastcall get_param_frms(void *p);

extern DECLSPEC uint32_t __fastcall get_param_type(void *p, int i);
extern DECLSPEC void * __fastcall get_param_val(void *p, int i);
extern DECLSPEC int __fastcall get_param_len(void *p, int i);

#ifdef  __cplusplus
}
#endif

#endif /* __PQPARAM_BUFFER_H */