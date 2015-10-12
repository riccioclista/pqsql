#ifndef __PQ_BINFMT_H
#define __PQ_BINFMT_H

#include <stdint.h>
#include "pqparam_buffer.h"

#if defined DLL_EXPORT
#define DECLSPEC __declspec(dllexport)
#else
#define DECLSPEC __declspec(dllimport)
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#define BAILIFNULL(p) do { if (p == NULL) return; } while(0)
#define BAILWITHVALUEIFNULL(p,val) do { if (p == NULL) return val; } while(0)


extern DECLSPEC void __fastcall pqbf_set_null(pqparam_buffer *p, uint32_t oid);

extern DECLSPEC int __fastcall pqbf_get_bool(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_bool(pqparam_buffer *p, int b);

extern DECLSPEC void __fastcall pqbf_get_bytea(const char *p, char* buf, size_t len);
extern DECLSPEC void __fastcall pqbf_set_bytea(pqparam_buffer *pb, const char* buf, size_t buflen);

extern DECLSPEC int64_t __fastcall pqbf_get_int8(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int8(pqparam_buffer *p, int64_t i);

extern DECLSPEC int32_t __fastcall pqbf_get_int4(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int4(pqparam_buffer *p, int32_t i);

extern DECLSPEC int16_t __fastcall pqbf_get_int2(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int2(pqparam_buffer *p, int16_t i);

extern DECLSPEC const char* __fastcall pqbf_get_text(const char *ptr, size_t *len);
extern DECLSPEC void __fastcall pqbf_set_text(pqparam_buffer *p, const char *t);

extern DECLSPEC wchar_t* __fastcall pqbf_get_unicode_text(const char *ptr, size_t *utf16_len);
extern DECLSPEC void __fastcall pqbf_free_unicode_text(wchar_t *p);
extern DECLSPEC void __fastcall pqbf_set_unicode_text(pqparam_buffer *p, const wchar_t *t);

extern DECLSPEC uint32_t __fastcall pqbf_get_oid(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_oid(pqparam_buffer *p, uint32_t i);

extern DECLSPEC float __fastcall pqbf_get_float4(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_float4(pqparam_buffer *p, float f);

extern DECLSPEC double __fastcall pqbf_get_float8(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_float8(pqparam_buffer *p, double f);

extern DECLSPEC double __fastcall pqbf_get_numeric(const char *ptr, int32_t typmod);
extern DECLSPEC void __fastcall pqbf_set_numeric(pqparam_buffer *p, double d);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINFMT_H */