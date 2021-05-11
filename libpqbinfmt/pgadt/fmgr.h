/*-------------------------------------------------------------------------
 *
 * fmgr.h
 *	  Definitions for the Postgres function manager and function-call
 *	  interface.
 *
 * This file must be included by all Postgres modules that either define
 * or call fmgr-callable functions.
 *
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/fmgr.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef FMGR_H
#define FMGR_H

/* Macros for returning results of standard types */

#define PG_RETURN_DATUM(x)	 return (x)
#define PG_RETURN_POINTER(x) return PointerGetDatum(x)
#define PG_RETURN_CSTRING(x) return CStringGetDatum(x)
/* these macros hide the pass-by-reference-ness of the datatype: */
#define PG_RETURN_FLOAT8(x)  return Float8GetDatum(x)
/* RETURN macros for other pass-by-ref types will typically look like this: */
#define PG_RETURN_BYTEA_P(x)   PG_RETURN_POINTER(x)

#endif							/* FMGR_H */
