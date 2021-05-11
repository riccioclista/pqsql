/**
 * @file float.c
 * @brief encode/decode float binary format to native datatype for PQgetvalue() and pqparam_buffer
 * @date 2015-09-30
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 * @note postgresql source src/backend/utils/adt/float.c
 */
/*-------------------------------------------------------------------------
 *
 * float.c
 *	  Functions for the built-in floating-point types.
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/utils/adt/float.c
 *
 *-------------------------------------------------------------------------
 */

#include <ctype.h>
#include <float.h>
#include <math.h>
#include <limits.h>

#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

#define DLL_EXPORT
#include "pqbinfmt_config.h"

#include "pgadt/c.h"
#include "pgadt/fmgr.h"
#include "pgadt/builtins.h"
#include "pgadt/fmgrprotos.h"
#include "pgadt/postgres.h"

#if defined(WIN32)
/*
 * Visual C++ sometimes misses isinf
 */
#if !defined(isinf)
int
isinf(double x)
{
        int                     fpclass = _fpclass(x);

        if (fpclass == _FPCLASS_PINF)
                return 1;
        if (fpclass == _FPCLASS_NINF)
                return -1;
        return 0;
}
#endif

#define pg_strncasecmp(s1,s2,c) _strnicmp(s1,s2,c)
#endif


#ifndef M_PI
/* from my RH5.2 gcc math.h file - thomas 2000-04-03 */
#define M_PI 3.14159265358979323846
#endif

/* Radians per degree, a.k.a. PI / 180 */
#define RADIANS_PER_DEGREE 0.0174532925199432957692

/* Visual C++ etc lacks NAN, and won't accept 0.0/0.0.  NAN definition from
 * http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vclang/html/vclrfNotNumberNANItems.asp
 */
#if defined(WIN32) && !defined(NAN)
static const uint32 nan[2] = {0xffffffff, 0x7fffffff};

#define NAN (*(const double *) nan)
#endif

/* not sure what the following should be, but better to make it over-sufficient */
#define MAXFLOATWIDTH	64
#define MAXDOUBLEWIDTH	128

/*
 * Routines to provide reasonably platform-independent handling of
 * infinity and NaN.  We assume that isinf() and isnan() are available
 * and work per spec.  (On some platforms, we have to supply our own;
 * see src/port.)  However, generating an Infinity or NaN in the first
 * place is less well standardized; pre-C99 systems tend not to have C99's
 * INFINITY and NAN macros.  We centralize our workarounds for this here.
 */

double
get_float8_infinity(void)
{
#ifdef INFINITY
	/* C99 standard way */
	return (double) INFINITY;
#else

	/*
	 * On some platforms, HUGE_VAL is an infinity, elsewhere it's just the
	 * largest normal double.  We assume forcing an overflow will get us a
	 * true infinity.
	 */
	return (double) (HUGE_VAL * HUGE_VAL);
#endif
}

double
get_float8_nan(void)
{
	/* (double) NAN doesn't work on some NetBSD/MIPS releases */
#if defined(NAN) && !(defined(__NetBSD__) && defined(__mips__))
	/* C99 standard way */
	return (double) NAN;
#else
	/* Assume we can get a NAN via zero divide */
	return (double) (0.0 / 0.0);
#endif
}

/*
 * Returns -1 if 'val' represents negative infinity, 1 if 'val'
 * represents (positive) infinity, and 0 otherwise. On some platforms,
 * this is equivalent to the isinf() macro, but not everywhere: C99
 * does not specify that isinf() needs to distinguish between positive
 * and negative infinity.
 */
int
is_infinite(double val)
{
	int			inf = isinf(val);

	if (inf == 0)
		return 0;
	else if (val > 0)
		return 1;
	else
		return -1;
}


/* ========== USER I/O ROUTINES ========== */


/*
 *		float8in		- converts "num" to float8
 */
Datum
float8in(char *num)
{
#if 0
	char	   *num = PG_GETARG_CSTRING(0);
#endif

	PG_RETURN_FLOAT8(float8in_internal(num, NULL, "double precision", num));
}

/*
 * float8in_internal - guts of float8in()
 *
 * This is exposed for use by functions that want a reasonably
 * platform-independent way of inputting doubles.  The behavior is
 * essentially like strtod + ereport on error, but note the following
 * differences:
 * 1. Both leading and trailing whitespace are skipped.
 * 2. If endptr_p is NULL, we throw error if there's trailing junk.
 * Otherwise, it's up to the caller to complain about trailing junk.
 * 3. In event of a syntax error, the report mentions the given type_name
 * and prints orig_string as the input; this is meant to support use of
 * this function with types such as "box" and "point", where what we are
 * parsing here is just a substring of orig_string.
 *
 * "num" could validly be declared "const char *", but that results in an
 * unreasonable amount of extra casting both here and in callers, so we don't.
 */
double
float8in_internal(char *num, char **endptr_p,
				  const char *type_name, const char *orig_string)
{
	double		val;
	char	   *endptr;

	/* skip leading whitespace */
	while (*num != '\0' && isspace((unsigned char) *num))
		num++;

	/*
	 * Check for an empty-string input to begin with, to avoid the vagaries of
	 * strtod() on different platforms.
	 */
	if (*num == '\0')
#if 1
		return 0;
#else
		ereport(ERROR,
				(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
				 errmsg("invalid input syntax for type %s: \"%s\"",
						type_name, orig_string)));
#endif

	errno = 0;
	val = strtod(num, &endptr);

	/* did we not see anything that looks like a double? */
	if (endptr == num || errno != 0)
	{
		int			save_errno = errno;

		/*
		 * C99 requires that strtod() accept NaN, [+-]Infinity, and [+-]Inf,
		 * but not all platforms support all of these (and some accept them
		 * but set ERANGE anyway...)  Therefore, we check for these inputs
		 * ourselves if strtod() fails.
		 *
		 * Note: C99 also requires hexadecimal input as well as some extended
		 * forms of NaN, but we consider these forms unportable and don't try
		 * to support them.  You can use 'em if your strtod() takes 'em.
		 */
		if (pg_strncasecmp(num, "NaN", 3) == 0)
		{
			val = get_float8_nan();
			endptr = num + 3;
		}
		else if (pg_strncasecmp(num, "Infinity", 8) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 8;
		}
		else if (pg_strncasecmp(num, "+Infinity", 9) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 9;
		}
		else if (pg_strncasecmp(num, "-Infinity", 9) == 0)
		{
			val = -get_float8_infinity();
			endptr = num + 9;
		}
		else if (pg_strncasecmp(num, "inf", 3) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 3;
		}
		else if (pg_strncasecmp(num, "+inf", 4) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 4;
		}
		else if (pg_strncasecmp(num, "-inf", 4) == 0)
		{
			val = -get_float8_infinity();
			endptr = num + 4;
		}
		else if (save_errno == ERANGE)
		{
			/*
			 * Some platforms return ERANGE for denormalized numbers (those
			 * that are not zero, but are too close to zero to have full
			 * precision).  We'd prefer not to throw error for that, so try to
			 * detect whether it's a "real" out-of-range condition by checking
			 * to see if the result is zero or huge.
			 *
			 * On error, we intentionally complain about double precision not
			 * the given type name, and we print only the part of the string
			 * that is the current number.
			 */
			if (val == 0.0 || val >= HUGE_VAL || val <= -HUGE_VAL)
			{
#if 1
				return 0;
#else
				char	   *errnumber = pstrdup(num);

				errnumber[endptr - num] = '\0';
				ereport(ERROR,
						(errcode(ERRCODE_NUMERIC_VALUE_OUT_OF_RANGE),
						 errmsg("\"%s\" is out of range for type double precision",
								errnumber)));
#endif
			}
		}
		else
#if 1
			return 0;
#else
			ereport(ERROR,
					(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
					 errmsg("invalid input syntax for type %s: \"%s\"",
							type_name, orig_string)));
#endif
	}
#ifdef HAVE_BUGGY_SOLARIS_STRTOD
	else
	{
		/*
		 * Many versions of Solaris have a bug wherein strtod sets endptr to
		 * point one byte beyond the end of the string when given "inf" or
		 * "infinity".
		 */
		if (endptr != num && endptr[-1] == '\0')
			endptr--;
	}
#endif							/* HAVE_BUGGY_SOLARIS_STRTOD */

	/* skip trailing whitespace */
	while (*endptr != '\0' && isspace((unsigned char) *endptr))
		endptr++;

	/* report stopping point if wanted, else complain if not end of string */
	if (endptr_p)
		*endptr_p = endptr;
	else if (*endptr != '\0')
#if 1
		return 0;
#else
		ereport(ERROR,
				(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
				 errmsg("invalid input syntax for type %s: \"%s\"",
						type_name, orig_string)));
#endif

	return val;
}