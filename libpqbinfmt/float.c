/*-------------------------------------------------------------------------
 *
 * float.c
 *	  Functions for the built-in floating-point types.
 *
 * Portions Copyright (c) 1996-2015, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/utils/adt/float.c
 *
 *-------------------------------------------------------------------------
 */
/**
 * @file float.c
 * @brief encode/decode float binary format to native datatype for PQgetvalue() and pqparam_buffer
 * @date 2015-09-30 
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 * @note postgresql source src/backend/utils/adt/float.c
 */

#include <ctype.h>
#include <float.h>
#include <math.h>
#include <limits.h>

#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

#ifndef M_PI
/* from my RH5.2 gcc math.h file - thomas 2000-04-03 */
#define M_PI 3.14159265358979323846
#endif

/* Visual C++ etc lacks NAN, and won't accept 0.0/0.0.  NAN definition from
 * http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vclang/html/vclrfNotNumberNANItems.asp
 */
//#if defined(WIN32) && !defined(NAN)
static const uint32_t nan[2] = {0xffffffff, 0x7fffffff};

#define NAN (*(const double *) nan)
//#endif

/* not sure what the following should be, but better to make it over-sufficient */
#define MAXFLOATWIDTH	64
#define MAXDOUBLEWIDTH	128

/*
 * check to see if a float4/8 val has underflowed or overflowed
 */
#define CHECKFLOATVAL(val, inf_is_valid, zero_is_valid)			\
do {															\
	if (isinf(val) && !(inf_is_valid))							\
		val = 0; \
	if ((val) == 0.0 && !(zero_is_valid))						\
		val = 0; \
} while(0)


/* ========== USER I/O ROUTINES ========== */



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

#define STRNCASECMP(s1,s2,c) _strnicmp(s1,s2,c) 
/*
 *		float8in		- converts "num" to float8
 */
double
float8in(char* num)
{
	//char	   *orig_num;
	double		val;
	char	   *endptr;

	/*
	 * endptr points to the first character _after_ the sequence we recognized
	 * as a valid floating point number. orig_num points to the original input
	 * string.
	 */
	//orig_num = num;

	/* skip leading whitespace */
	while (*num != '\0' && isspace((unsigned char) *num))
		num++;

	/*
	 * Check for an empty-string input to begin with, to avoid the vagaries of
	 * strtod() on different platforms.
	 */
	if (*num == '\0')
		return 0;
///		ereport(ERROR,
///				(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
///			 errmsg("invalid input syntax for type double precision: \"%s\"",
///					orig_num)));

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
		if (STRNCASECMP(num, "NaN", 3) == 0)
		{
			val = get_float8_nan();
			endptr = num + 3;
		}
		else if (STRNCASECMP(num, "Infinity", 8) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 8;
		}
		else if (STRNCASECMP(num, "+Infinity", 9) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 9;
		}
		else if (STRNCASECMP(num, "-Infinity", 9) == 0)
		{
			val = -get_float8_infinity();
			endptr = num + 9;
		}
		else if (STRNCASECMP(num, "inf", 3) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 3;
		}
		else if (STRNCASECMP(num, "+inf", 4) == 0)
		{
			val = get_float8_infinity();
			endptr = num + 4;
		}
		else if (STRNCASECMP(num, "-inf", 4) == 0)
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
			 */
			if (val == 0.0 || val >= HUGE_VAL || val <= -HUGE_VAL)
				return 0;
///				ereport(ERROR,
///						(errcode(ERRCODE_NUMERIC_VALUE_OUT_OF_RANGE),
///				   errmsg("\"%s\" is out of range for type double precision",
///						  orig_num)));
		}
		else
			return 0;
///			ereport(ERROR,
///					(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
///			 errmsg("invalid input syntax for type double precision: \"%s\"",
///					orig_num)));
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
#endif   /* HAVE_BUGGY_SOLARIS_STRTOD */

	/* skip trailing whitespace */
	while (*endptr != '\0' && isspace((unsigned char) *endptr))
		endptr++;

	/* if there is any junk left at the end of the string, bail out */
	if (*endptr != '\0')
		return 0;
///		ereport(ERROR,
///				(errcode(ERRCODE_INVALID_TEXT_REPRESENTATION),
///			 errmsg("invalid input syntax for type double precision: \"%s\"",
///					orig_num)));

	CHECKFLOATVAL(val, 1, 1);

	return val;
}