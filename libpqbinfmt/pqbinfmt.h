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


extern DECLSPEC void __fastcall pqbf_set_null(pqparam_buffer *pb, uint32_t oid);

extern DECLSPEC int __fastcall pqbf_get_bool(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_bool(pqparam_buffer *pb, int b);

extern DECLSPEC void __fastcall pqbf_get_bytea(const char *p, char* buf, size_t len);
extern DECLSPEC void __fastcall pqbf_set_bytea(pqparam_buffer *pb, const char* buf, size_t buflen);

extern DECLSPEC int64_t __fastcall pqbf_get_int8(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int8(pqparam_buffer *pb, int64_t i);

extern DECLSPEC int32_t __fastcall pqbf_get_int4(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int4(pqparam_buffer *pb, int32_t i);

extern DECLSPEC int16_t __fastcall pqbf_get_int2(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_int2(pqparam_buffer *pb, int16_t i);

extern DECLSPEC const char* __fastcall pqbf_get_text(const char *ptr, size_t *len);
extern DECLSPEC void __fastcall pqbf_set_text(pqparam_buffer *pb, const char *t);

extern DECLSPEC wchar_t* __fastcall pqbf_get_unicode_text(const char *ptr, size_t *utf16_len);
extern DECLSPEC void __fastcall pqbf_free_unicode_text(wchar_t *p);
extern DECLSPEC void __fastcall pqbf_set_unicode_text(pqparam_buffer *pb, const wchar_t *t);

extern DECLSPEC uint32_t __fastcall pqbf_get_oid(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_oid(pqparam_buffer *pb, uint32_t i);

extern DECLSPEC float __fastcall pqbf_get_float4(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_float4(pqparam_buffer *pb, float f);

extern DECLSPEC double __fastcall pqbf_get_float8(const char *ptr);
extern DECLSPEC void __fastcall pqbf_set_float8(pqparam_buffer *p, double f);

extern DECLSPEC double __fastcall pqbf_get_numeric(const char *ptr, int32_t typmod);
extern DECLSPEC void __fastcall pqbf_set_numeric(pqparam_buffer *pb, double d);

extern DECLSPEC void __fastcall pqbf_get_timestamp(const char *p, time_t *sec, int *usec);
extern DECLSPEC void __fastcall pqbf_set_timestamp(pqparam_buffer *pb, time_t sec, int usec);

extern DECLSPEC void __fastcall pqbf_get_timestamptz(const char *p, time_t *sec, time_t *usec);
extern DECLSPEC void __fastcall pqbf_set_timestamptz(pqparam_buffer *pb, time_t sec, time_t usec);

extern DECLSPEC void __fastcall pqbf_get_interval(const char *ptr, int64_t *offset, int32_t *day, int32_t *month);
extern DECLSPEC void __fastcall pqbf_set_interval(pqparam_buffer *pb, int64_t offset, int32_t day, int32_t month);

extern DECLSPEC time_t __fastcall pqbf_get_time(const char *p);
extern DECLSPEC void __fastcall pqbf_set_time(pqparam_buffer *pb, time_t t);

extern DECLSPEC time_t __fastcall pqbf_get_timetz(const char *p);
extern DECLSPEC void __fastcall pqbf_set_timetz(pqparam_buffer *pb, time_t t);

extern DECLSPEC int32_t __fastcall pqbf_get_date(const char *p);
extern DECLSPEC void __fastcall pqbf_set_date(pqparam_buffer *pb, int32_t t);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINFMT_H */