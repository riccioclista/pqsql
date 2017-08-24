/*-------------------------------------------------------------------------
 *
 * builtins.h
 *	  Declarations for operations on built-in types.
 *
 *
 * Portions Copyright (c) 1996-2016, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/utils/builtins.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef BUILTINS_H
#define BUILTINS_H

#include "pgadt/fmgr.h"
#include "pgadt/postgres.h"
#include "pgadt/numeric.h"
#include "pgadt/pqexpbuffer.h"

/*
 *		Defined in adt/
 */

/* float.c */

extern double get_float8_infinity(void);
extern double get_float8_nan(void);
extern int	is_infinite(double val);
extern double float8in_internal(char *num, char **endptr_p,
				  const char *type_name, const char *orig_string);

extern Datum float8in(char *num);

/* numeric.c */
extern Datum numeric_out(Numeric num);
extern Datum numeric_recv(const char *buf, int32 typmod);
extern Datum numeric_send(PQExpBuffer buf, Numeric num);
extern Datum numeric_float8(Numeric num);
extern Datum float8_numeric(double val);

#endif   /* BUILTINS_H */
