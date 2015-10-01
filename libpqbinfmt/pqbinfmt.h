#ifndef __PQ_BINFMT_H
#define __PQ_BINFMT_H

#include <stdint.h>

#if defined DLL_EXPORT
#define DECLSPEC __declspec(dllexport)
#else
#define DECLSPEC __declspec(dllimport)
#endif

#ifdef  __cplusplus
extern "C" {
#endif

extern DECLSPEC void __fastcall setmsg_null(void *p, uint32_t oid);

extern DECLSPEC int __fastcall getmsg_bool(const char *ptr);
extern DECLSPEC void __fastcall setmsg_bool(void *p, int b);

extern DECLSPEC int64_t __fastcall getmsg_int8(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int8(void *p, int64_t i);

extern DECLSPEC int32_t __fastcall getmsg_int4(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int4(void *p, int32_t i);

extern DECLSPEC int16_t __fastcall getmsg_int2(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int2(void *p, int16_t i);

extern DECLSPEC const char* __fastcall getmsg_text(const char *ptr, size_t *len);
extern DECLSPEC void __fastcall setmsg_text(void *p, const char *t);

extern DECLSPEC wchar_t* __fastcall getmsg_unicode_text(const char *ptr, size_t *utf16_len);
extern DECLSPEC void __fastcall free_unicode_text(wchar_t *p);
extern DECLSPEC void __fastcall setmsg_unicode_text(void *p, const wchar_t *t);

extern DECLSPEC uint32_t __fastcall getmsg_oid(const char *ptr);
extern DECLSPEC void __fastcall setmsg_oid(void *p, uint32_t i);

extern DECLSPEC float __fastcall getmsg_float4(const char *ptr);
extern DECLSPEC void __fastcall setmsg_float4(void *p, float f);

extern DECLSPEC double __fastcall getmsg_float8(const char *ptr);
extern DECLSPEC void __fastcall setmsg_float8(void *p, double f);

extern DECLSPEC double __fastcall getmsg_numeric(const char *ptr);
extern DECLSPEC void __fastcall setmsg_numeric(void *p, double d);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINFMT_H */