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

/* acl.c */

/* amutils.c */

/* bool.c */

/* char.c */

/* domains.c */

/* encode.c */

/* enum.c */

/* int.c */

/* name.c */

/* numutils.c */

/* float.c */

extern double get_float8_infinity(void);
extern double get_float8_nan(void);
extern int	is_infinite(double val);
extern double float8in_internal(char *num, char **endptr_p,
				  const char *type_name, const char *orig_string);

extern Datum float8in(char *num);

/* dbsize.c */

/* genfile.c */

/* misc.c */

/* oid.c */

/* orderedsetaggs.c */

/* pseudotypes.c */

/* regexp.c */

/* regproc.c */

/* rowtypes.c */

/* ruleutils.c */

/* tid.c */

/* varchar.c */

/* varlena.c */

/* version.c */

/* xid.c */

/* like.c */

/* oracle_compat.c */

/* inet_cidr_ntop.c */

/* inet_net_pton.c */

/* network.c */

/* mac.c */

/* numeric.c */

extern Datum numeric_out(Numeric num);
extern Datum numeric_recv(const char *buf, int32 typmod);

extern Datum numeric_send(PQExpBuffer buf, Numeric num);
extern Datum float8_numeric(double val);
extern Datum numeric_float8(Numeric num);

/* ri_triggers.c */

/* trigfuncs.c */

/* encoding support functions */

/* format_type.c */

/* quote.c */

/* guc.c */

/* pg_config.c */

/* pg_controldata.c */

/* rls.c */

/* lockfuncs.c */

/* txid.c */

/* uuid.c */

/* windowfuncs.c */

/* access/spgist/spgquadtreeproc.c */

/* access/spgist/spgkdtreeproc.c */

/* access/spgist/spgtextproc.c */

/* access/gin/ginarrayproc.c */

/* access/tablesample/bernoulli.c */

/* access/tablesample/system.c */

/* access/transam/twophase.c */

/* access/transam/multixact.c */

/* access/transam/committs.c */

/* catalogs/dependency.c */

/* catalog/objectaddress.c */

/* commands/constraint.c */

/* commands/event_trigger.c */

/* commands/extension.c */

/* commands/prepare.c */

/* utils/mmgr/portalmem.c */

#endif   /* BUILTINS_H */
