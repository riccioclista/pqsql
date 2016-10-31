/**
 * @file pqbinfmt.h
 * @brief encode/decode postgresql binary format to native datatype for PQgetvalue() and pqparam_buffer
 * @date 2015-09-30
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2016, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 */

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

/* BYTESWAP for network byte order */
#ifdef _WIN32
#define BYTESWAP2(x) _byteswap_ushort(x)
#define BYTESWAP4(x) _byteswap_ulong(x)
#define BYTESWAP8(x) _byteswap_uint64(x)
#endif /* _WIN32 */

extern DECLSPEC size_t pqbf_get_buflen(PQExpBuffer s);
extern DECLSPEC char * pqbf_get_bufval(PQExpBuffer s);

extern DECLSPEC void pqbf_add_null(pqparam_buffer *pb, uint32_t oid);

extern DECLSPEC int pqbf_get_bool(const char *ptr);
extern DECLSPEC void pqbf_set_bool(PQExpBuffer s, int b);
extern DECLSPEC void pqbf_add_bool(pqparam_buffer *pb, int b);

extern DECLSPEC void pqbf_get_bytea(const char *p, char* buf, size_t len);
extern DECLSPEC void pqbf_set_bytea(PQExpBuffer s, const char* buf, size_t buflen);
extern DECLSPEC void pqbf_add_bytea(pqparam_buffer *pb, const char* buf, size_t buflen);

extern DECLSPEC int64_t pqbf_get_int8(const char *ptr);
extern DECLSPEC void pqbf_set_int8(PQExpBuffer s, int64_t i);
extern DECLSPEC void pqbf_add_int8(pqparam_buffer *pb, int64_t i);

extern DECLSPEC int32_t pqbf_get_int4(const char *ptr);
extern DECLSPEC void pqbf_set_int4(PQExpBuffer s, int32_t i);
extern DECLSPEC void pqbf_add_int4(pqparam_buffer *pb, int32_t i);

extern DECLSPEC int16_t pqbf_get_int2(const char *ptr);
extern DECLSPEC void pqbf_set_int2(PQExpBuffer s, int16_t i);
extern DECLSPEC void pqbf_add_int2(pqparam_buffer *pb, int16_t i);

extern DECLSPEC const char* pqbf_get_text(const char *ptr, size_t *len);
extern DECLSPEC void pqbf_set_text(PQExpBuffer s, const char *t);
extern DECLSPEC void pqbf_add_text(pqparam_buffer *pb, const char *t, uint32_t oid);

#ifdef _WIN32
extern DECLSPEC wchar_t* pqbf_get_unicode_text(const char *ptr, size_t *utf16_len);
extern DECLSPEC void pqbf_free_unicode_text(wchar_t *p);
extern DECLSPEC void pqbf_set_unicode_text(PQExpBuffer s, const wchar_t *t);
extern DECLSPEC void pqbf_add_unicode_text(pqparam_buffer *pb, const wchar_t *t, uint32_t oid);
#endif /* _WIN32 */

extern DECLSPEC uint32_t pqbf_get_oid(const char *ptr);
extern DECLSPEC void pqbf_set_oid(PQExpBuffer s, uint32_t i);
extern DECLSPEC void pqbf_add_oid(pqparam_buffer *pb, uint32_t i);

extern DECLSPEC float pqbf_get_float4(const char *ptr);
extern DECLSPEC void pqbf_set_float4(PQExpBuffer s, float f);
extern DECLSPEC void pqbf_add_float4(pqparam_buffer *pb, float f);

extern DECLSPEC double pqbf_get_float8(const char *ptr);
extern DECLSPEC void pqbf_set_float8(PQExpBuffer s, double f);
extern DECLSPEC void pqbf_add_float8(pqparam_buffer *p, double f);

extern DECLSPEC double pqbf_get_numeric(const char *ptr, int32_t typmod);
extern DECLSPEC void pqbf_set_numeric(PQExpBuffer s, double d);
extern DECLSPEC void pqbf_add_numeric(pqparam_buffer *pb, double d);

extern DECLSPEC void pqbf_get_timestamp(const char *p, time_t *sec, int *usec);
extern DECLSPEC void pqbf_set_timestamp(PQExpBuffer s, time_t sec, int usec);
extern DECLSPEC void pqbf_add_timestamp(pqparam_buffer *pb, time_t sec, int usec, uint32_t oid);

extern DECLSPEC void pqbf_get_interval(const char *ptr, int64_t *offset, int32_t *day, int32_t *month);
extern DECLSPEC void pqbf_set_interval(PQExpBuffer s, int64_t offset, int32_t day, int32_t month);
extern DECLSPEC void pqbf_add_interval(pqparam_buffer *pb, int64_t offset, int32_t day, int32_t month);

extern DECLSPEC time_t pqbf_get_time(const char *p);
extern DECLSPEC void pqbf_set_time(PQExpBuffer s, time_t t);
extern DECLSPEC void pqbf_add_time(pqparam_buffer *pb, time_t t);

extern DECLSPEC int32_t pqbf_get_date(const char *p);
extern DECLSPEC void pqbf_set_date(PQExpBuffer s, int32_t t);
extern DECLSPEC void pqbf_add_date(pqparam_buffer *pb, int32_t t);

#define MAXDIM 6
extern DECLSPEC const char * pqbf_get_array(const char* p, int32_t* ndim, int32_t* flags, uint32_t* oid,	int* dim[MAXDIM],	int* lbound[MAXDIM]);
extern DECLSPEC const char * pqbf_get_array_value(const char* p, int32_t* itemlen);
extern DECLSPEC void pqbf_set_array(PQExpBuffer s, int32_t ndim, int32_t flags, uint32_t oid, int dim[MAXDIM],	int lbound[MAXDIM]);
extern DECLSPEC void pqbf_add_array(pqparam_buffer *pb, PQExpBuffer a, uint32_t oid);
extern DECLSPEC void pqbf_set_array_itemlength(PQExpBuffer a, int32_t itemlen);
extern DECLSPEC void pqbf_update_array_itemlength(PQExpBuffer a, ptrdiff_t offset, int32_t itemlen);
extern DECLSPEC void pqbf_set_array_value(PQExpBuffer a, const char* p, int32_t itemlen);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINFMT_H */