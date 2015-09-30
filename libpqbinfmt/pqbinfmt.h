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

extern DECLSPEC int __fastcall getmsg_bool(const char *ptr);
extern DECLSPEC void __fastcall setmsg_bool(void *payload, int b, char **param_val, int **param_len);

extern DECLSPEC int64_t __fastcall getmsg_int8(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int8(void *payload, int64_t i, char **param_val, int **param_len);

extern DECLSPEC int32_t __fastcall getmsg_int4(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int4(void *payload, int32_t i, char **param_val, int **param_len);

extern DECLSPEC int16_t __fastcall getmsg_int2(const char *ptr);
extern DECLSPEC void __fastcall setmsg_int2(void *payload, int16_t i, char **param_val, int **param_len);

extern DECLSPEC const char* __fastcall getmsg_text(const char *ptr, int *len);
extern DECLSPEC void __fastcall setmsg_text(void *payload, const char *t, char **param_val, int **param_len);
extern DECLSPEC wchar_t* __fastcall getmsg_unicode_text(const char *ptr, int *utf16_len);
extern DECLSPEC void __fastcall free_unicode_text(wchar_t *p);

extern DECLSPEC uint32_t __fastcall getmsg_oid(const char *ptr);
extern DECLSPEC void __fastcall setmsg_oid(void *payload, uint32_t i, char **param_val, int **param_len);

extern DECLSPEC float __fastcall getmsg_float4(const char *ptr);
extern DECLSPEC void __fastcall setmsg_float4(void *payload, float f, char **param_val, int **param_len);

extern DECLSPEC double __fastcall getmsg_float8(const char *ptr);
extern DECLSPEC void __fastcall setmsg_float8(void *payload, double f, char **param_val, int **param_len);

extern DECLSPEC double __fastcall getmsg_numeric(const char *ptr);
extern DECLSPEC void __fastcall setmsg_numeric(void *payload, double d, char **param_val, int **param_len);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINFMT_H */