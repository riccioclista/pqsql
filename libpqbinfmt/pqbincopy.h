#ifndef __PQ_BINCOPY_H
#define __PQ_BINCOPY_H

#include <stdint.h>
#include <libpq-fe.h>

#if defined DLL_EXPORT
#define DECLSPEC __declspec(dllexport)
#else
#define DECLSPEC __declspec(dllimport)
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#define PQBUFSIZ 8192

/*
 * data structure encapsulating 8k COPY FROM buffer
 */
typedef struct pqcopy_buffer
{
	PGconn *conn;
	uint16_t num_cols;
	int16_t pos_cols;
	size_t len;
	char payload[PQBUFSIZ];
} pqcopy_buffer;


extern DECLSPEC pqcopy_buffer * __fastcall pqcb_create(PGconn *conn, int num_cols);
extern DECLSPEC void __fastcall pqcb_free(pqcopy_buffer *p);
extern DECLSPEC void __fastcall pqcb_reset(pqcopy_buffer *p, int num_cols);

extern DECLSPEC int __fastcall pqcb_put_col(pqcopy_buffer *buf, const char *val, uint32_t len);
extern DECLSPEC int __fastcall pqcb_put_end(pqcopy_buffer *buf);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_BINCOPY_H */