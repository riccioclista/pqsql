/*-------------------------------------------------------------------------
 *
 * fmgrprotos.h
 *    Prototypes for built-in functions.
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * NOTES
 *	******************************
 *	*** DO NOT EDIT THIS FILE! ***
 *	******************************
 *
 *	It has been GENERATED by Gen_fmgrtab.pl
 *	from ../../../src/include/catalog/pg_proc.h
 *
 *-------------------------------------------------------------------------
 */

#ifndef FMGRPROTOS_H
#define FMGRPROTOS_H

#include "pgadt/fmgr.h"
#include "pgadt/postgres.h"
#include "pgadt/numeric.h"
#include "pgadt/pqexpbuffer.h"

extern Datum float8in(char *num);

extern Datum numeric_out(Numeric num);
extern Datum numeric_recv(const char *buf, int32 typmod);
extern Datum numeric_send(PQExpBuffer buf, Numeric num);
extern Datum float8_numeric(double val);
extern Datum numeric_float8(Numeric num);

#endif /* FMGRPROTOS_H */
