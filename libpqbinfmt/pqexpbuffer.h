#ifndef __PQEXPBUFFER_H
#define __PQEXPBUFFER_H

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

#ifdef  __cplusplus
}
#endif

#endif /* __PQEXPBUFFER_H */