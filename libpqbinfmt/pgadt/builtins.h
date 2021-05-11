/*-------------------------------------------------------------------------
 *
 * builtins.h
 *	  Declarations for operations on built-in types.
 *
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
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

/* bool.c */

/* domains.c */

/* encode.c */

/* int.c */

/* name.c */

/* numutils.c */

/* float.c */

extern double get_float8_infinity(void);
extern double get_float8_nan(void);
extern int	is_infinite(double val);
extern double float8in_internal(char *num, char **endptr_p,
				  const char *type_name, const char *orig_string);

/* oid.c */

/* regexp.c */

/* ruleutils.c */

/* varchar.c */

/* varlena.c */

/* xid.c */

/* inet_cidr_ntop.c */

/* inet_net_pton.c */

/* network.c */

/* numeric.c */

/* format_type.c */

/* quote.c */

#endif   /* BUILTINS_H */
