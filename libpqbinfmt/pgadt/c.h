/*-------------------------------------------------------------------------
 *
 * c.h
 *	  Fundamental C definitions.  This is included by every .c file in
 *	  PostgreSQL (via either postgres.h or postgres_fe.h, as appropriate).
 *
 *	  Note that the definitions here are not intended to be exposed to clients
 *	  of the frontend interface libraries --- so we don't worry much about
 *	  polluting the namespace with lots of stuff...
 *
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/c.h
 *
 *-------------------------------------------------------------------------
 */
/*
 *----------------------------------------------------------------
 *	 TABLE OF CONTENTS
 *
 *		When adding stuff to this file, please try to put stuff
 *		into the relevant section, or add new sections as appropriate.
 *
 *	  section	description
 *	  -------	------------------------------------------------
 *		0)		pg_config.h and standard system headers
 *		1)		compiler characteristics
 *		2)		bool, true, false, TRUE, FALSE, NULL
 *		3)		standard system types
 *		4)		IsValid macros for system types
 *		5)		offsetof, lengthof, endof, alignment
 *		6)		assertions
 *		7)		widely useful macros
 *		8)		random stuff
 *		9)		system-specific hacks
 *
 * NOTE: since this file is included by both frontend and backend modules, it's
 * almost certainly wrong to put an "extern" declaration here.  typedefs and
 * macros are the kind of thing that might go here.
 *
 *----------------------------------------------------------------
 */
#ifndef C_H
#define C_H

#if !defined(WIN32)
#include <stddef.h>

/* excerpt from pg_config.h */
#define PG_PRINTF_ATTRIBUTE gnu_printf
#endif

#include <stdint.h>

/* pg_config.h */
/* Define to nothing if C supports flexible array members, and to 1 if it does
   not. That way, with a declaration like `struct s { int n; double
   d[FLEXIBLE_ARRAY_MEMBER]; };', the struct hack can be used with pre-C99
   compilers. When computing the size of such an object, don't use 'sizeof
   (struct s)' as it overestimates the size. Use 'offsetof (struct s, d)'
   instead. Don't use 'offsetof (struct s, d[0])', as this doesn't work with
   MSVC and with C++ compilers. */
#define FLEXIBLE_ARRAY_MEMBER
/* Define bytes to use libc memset(). */
#define MEMSET_LOOP_LIMIT 1024



/* Must be before gettext() games below */
#include <locale.h>

#ifdef ENABLE_NLS
#include <libintl.h>
#endif

/* ----------------------------------------------------------------
 *				Section 1: compiler characteristics
 *
 * type prefixes (const, signed, volatile, inline) are handled in pg_config.h.
 * ----------------------------------------------------------------
 */

/*
 * Attribute macros
 *
 * GCC: https://gcc.gnu.org/onlinedocs/gcc/Function-Attributes.html
 * GCC: https://gcc.gnu.org/onlinedocs/gcc/Type-Attributes.html
 * Sunpro: https://docs.oracle.com/cd/E18659_01/html/821-1384/gjzke.html
 * XLC: http://www-01.ibm.com/support/knowledgecenter/SSGH2K_11.1.0/com.ibm.xlc111.aix.doc/language_ref/function_attributes.html
 * XLC: http://www-01.ibm.com/support/knowledgecenter/SSGH2K_11.1.0/com.ibm.xlc111.aix.doc/language_ref/type_attrib.html
 */

/* only GCC supports the unused attribute */
#ifdef __GNUC__
#define pg_attribute_unused() __attribute__((unused))
#else
#define pg_attribute_unused()
#endif

/* GCC and XLC support format attributes */
#if defined(__GNUC__) || defined(__IBMC__)
#define pg_attribute_format_arg(a) __attribute__((format_arg(a)))
#define pg_attribute_printf(f,a) __attribute__((format(PG_PRINTF_ATTRIBUTE, f, a)))
#else
#define pg_attribute_format_arg(a)
#define pg_attribute_printf(f,a)
#endif

/* GCC, Sunpro and XLC support aligned, packed and noreturn */
#if defined(__GNUC__) || defined(__SUNPRO_C) || defined(__IBMC__)
#define pg_attribute_aligned(a) __attribute__((aligned(a)))
#define pg_attribute_noreturn() __attribute__((noreturn))
#define pg_attribute_packed() __attribute__((packed))
#define HAVE_PG_ATTRIBUTE_NORETURN 1
#else
/*
 * NB: aligned and packed are not given default definitions because they
 * affect code functionality; they *must* be implemented by the compiler
 * if they are to be used.
 */
#define pg_attribute_noreturn()
#endif

/*
 * Mark a point as unreachable in a portable fashion.  This should preferably
 * be something that the compiler understands, to aid code generation.
 * In assert-enabled builds, we prefer abort() for debugging reasons.
 */
#if defined(HAVE__BUILTIN_UNREACHABLE) && !defined(USE_ASSERT_CHECKING)
#define pg_unreachable() __builtin_unreachable()
#elif defined(_MSC_VER) && !defined(USE_ASSERT_CHECKING)
#define pg_unreachable() __assume(0)
#else
#define pg_unreachable() abort()
#endif


/* ----------------------------------------------------------------
 *				Section 2:	bool, true, false, TRUE, FALSE, NULL
 * ----------------------------------------------------------------
 */

/*
 * bool
 *		Boolean value, either true or false.
 *
 * XXX for C++ compilers, we assume the compiler has a compatible
 * built-in definition of bool.
 */

#ifndef TRUE
#define TRUE	1
#endif

#ifndef FALSE
#define FALSE	0
#endif


/* ----------------------------------------------------------------
 *				Section 3:	standard system types
 * ----------------------------------------------------------------
 */

/*
 * Pointer
 *		Variable holding address of any memory resident object.
 *
 *		XXX Pointer arithmetic is done with this, so it can't be void *
 *		under "true" ANSI compilers.
 */
typedef char *Pointer;

/*
 * intN
 *		Signed integer, EXACTLY N BITS IN SIZE,
 *		used for numerical computations and the
 *		frontend/backend protocol.
 */
typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;

/*
 * uintN
 *		Unsigned integer, EXACTLY N BITS IN SIZE,
 *		used for numerical computations and the
 *		frontend/backend protocol.
 */
typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;

/*
 * 64-bit integers
 */
/* We have working support for "long long int", use that */
typedef int64_t int64;
typedef uint64_t uint64;

#define INT64CONST(x)  (x##LL)
#define UINT64CONST(x) (x##ULL)

/* snprintf format strings to use for 64-bit integers */
#define INT64_FORMAT "%" INT64_MODIFIER "d"
#define UINT64_FORMAT "%" INT64_MODIFIER "u"

/*
 * 128-bit signed and unsigned integers
 *		There currently is only limited support for such types.
 *		E.g. 128bit literals and snprintf are not supported; but math is.
 *		Also, because we exclude such types when choosing MAXIMUM_ALIGNOF,
 *		it must be possible to coerce the compiler to allocate them on no
 *		more than MAXALIGN boundaries.
 */
#if defined(PG_INT128_TYPE)
#define HAVE_INT128 1
typedef PG_INT128_TYPE int128;
typedef unsigned PG_INT128_TYPE uint128;
#endif


/*
 * Size
 *		Size of any memory resident object, as returned by sizeof.
 */
typedef size_t Size;

/*
 * Common Postgres datatype names (as used in the catalogs)
 */
typedef float float4;
typedef double float8;


/*
 * Array indexing support
 */
#define MAXDIM 6

/* ----------------
 *		Variable-length datatypes all share the 'struct varlena' header.
 *
 * NOTE: for TOASTable types, this is an oversimplification, since the value
 * may be compressed or moved out-of-line.  However datatype-specific routines
 * are mostly content to deal with de-TOASTed values only, and of course
 * client-side routines should never see a TOASTed value.  But even in a
 * de-TOASTed value, beware of touching vl_len_ directly, as its
 * representation is no longer convenient.  It's recommended that code always
 * use macros VARDATA_ANY, VARSIZE_ANY, VARSIZE_ANY_EXHDR, VARDATA, VARSIZE,
 * and SET_VARSIZE instead of relying on direct mentions of the struct fields.
 * See postgres.h for details of the TOASTed form.
 * ----------------
 */

#define VARHDRSZ		((int32) sizeof(int32))


/* ----------------------------------------------------------------
 *				Section 4:	IsValid macros for system types
 * ----------------------------------------------------------------
 */


/* ----------------------------------------------------------------
 *				Section 5:	offsetof, lengthof, endof, alignment
 * ----------------------------------------------------------------
 */


/* ----------------------------------------------------------------
 *				Section 6:	assertions
 * ----------------------------------------------------------------
 */

/*
 * USE_ASSERT_CHECKING, if defined, turns on all the assertions.
 * - plai  9/5/90
 *
 * It should _NOT_ be defined in releases or in benchmark copies
 */

/*
 * Assert() can be used in both frontend and backend code. In frontend code it
 * just calls the standard assert, if it's available. If use of assertions is
 * not configured, it does nothing.
 */
#include <assert.h>
#define Assert(p) assert(p)


/* ----------------------------------------------------------------
 *				Section 7:	widely useful macros
 * ----------------------------------------------------------------
 */
/*
 * Max
 *		Return the maximum of two numbers.
 */
#define Max(x, y)		((x) > (y) ? (x) : (y))

/*
 * Min
 *		Return the minimum of two numbers.
 */
#define Min(x, y)		((x) < (y) ? (x) : (y))

/*
 * Abs
 *		Return the absolute value of the argument.
 */
#define Abs(x)			((x) >= 0 ? (x) : -(x))

/*
 * StrNCpy
 *	Like standard library function strncpy(), except that result string
 *	is guaranteed to be null-terminated --- that is, at most N-1 bytes
 *	of the source string will be kept.
 *	Also, the macro returns no result (too hard to do that without
 *	evaluating the arguments multiple times, which seems worse).
 *
 *	BTW: when you need to copy a non-null-terminated string (like a text
 *	datum) and add a null, do not do it with StrNCpy(..., len+1).  That
 *	might seem to work, but it fetches one byte more than there is in the
 *	text object.  One fine day you'll have a SIGSEGV because there isn't
 *	another byte before the end of memory.  Don't laugh, we've had real
 *	live bug reports from real live users over exactly this mistake.
 *	Do it honestly with "memcpy(dst,src,len); dst[len] = '\0';", instead.
 */
#define StrNCpy(dst,src,len) \
	do \
	{ \
		char * _dst = (dst); \
		Size _len = (len); \
\
		if (_len > 0) \
		{ \
			strncpy(_dst, (src), _len); \
			_dst[_len-1] = '\0'; \
		} \
	} while (0)


/* Get a bit mask of the bits set in non-long aligned addresses */
#define LONG_ALIGN_MASK (sizeof(long) - 1)

/*
 * MemSet
 *	Exactly the same as standard library function memset(), but considerably
 *	faster for zeroing small word-aligned structures (such as parsetree nodes).
 *	This has to be a macro because the main point is to avoid function-call
 *	overhead.   However, we have also found that the loop is faster than
 *	native libc memset() on some platforms, even those with assembler
 *	memset() functions.  More research needs to be done, perhaps with
 *	MEMSET_LOOP_LIMIT tests in configure.
 */
#define MemSet(start, val, len) \
	do \
	{ \
		/* must be void* because we don't know if it is integer aligned yet */ \
		void   *_vstart = (void *) (start); \
		int		_val = (val); \
		Size	_len = (len); \
\
		if ((((uintptr_t) _vstart) & LONG_ALIGN_MASK) == 0 && \
			(_len & LONG_ALIGN_MASK) == 0 && \
			_val == 0 && \
			_len <= MEMSET_LOOP_LIMIT && \
			/* \
			 *	If MEMSET_LOOP_LIMIT == 0, optimizer should find \
			 *	the whole "if" false at compile time. \
			 */ \
			MEMSET_LOOP_LIMIT != 0) \
		{ \
			long *_start = (long *) _vstart; \
			long *_stop = (long *) ((char *) _start + _len); \
			while (_start < _stop) \
				*_start++ = 0; \
		} \
		else \
			memset(_vstart, _val, _len); \
	} while (0)

/*
 * MemSetAligned is the same as MemSet except it omits the test to see if
 * "start" is word-aligned.  This is okay to use if the caller knows a-priori
 * that the pointer is suitably aligned (typically, because he just got it
 * from palloc(), which always delivers a max-aligned pointer).
 */
#define MemSetAligned(start, val, len) \
	do \
	{ \
		long   *_start = (long *) (start); \
		int		_val = (val); \
		Size	_len = (len); \
\
		if ((_len & LONG_ALIGN_MASK) == 0 && \
			_val == 0 && \
			_len <= MEMSET_LOOP_LIMIT && \
			MEMSET_LOOP_LIMIT != 0) \
		{ \
			long *_stop = (long *) ((char *) _start + _len); \
			while (_start < _stop) \
				*_start++ = 0; \
		} \
		else \
			memset(_start, _val, _len); \
	} while (0)


/*
 * MemSetTest/MemSetLoop are a variant version that allow all the tests in
 * MemSet to be done at compile time in cases where "val" and "len" are
 * constants *and* we know the "start" pointer must be word-aligned.
 * If MemSetTest succeeds, then it is okay to use MemSetLoop, otherwise use
 * MemSetAligned.  Beware of multiple evaluations of the arguments when using
 * this approach.
 */
#define MemSetTest(val, len) \
	( ((len) & LONG_ALIGN_MASK) == 0 && \
	(len) <= MEMSET_LOOP_LIMIT && \
	MEMSET_LOOP_LIMIT != 0 && \
	(val) == 0 )

#define MemSetLoop(start, val, len) \
	do \
	{ \
		long * _start = (long *) (start); \
		long * _stop = (long *) ((char *) _start + (Size) (len)); \
	\
		while (_start < _stop) \
			*_start++ = 0; \
	} while (0)


/* ----------------------------------------------------------------
 *				Section 8:	random stuff
 * ----------------------------------------------------------------
 */

/*
 * gettext support
 */

#ifndef ENABLE_NLS
/* stuff we'd otherwise get from <libintl.h> */
#define gettext(x) (x)
#define dgettext(d,x) (x)
#define ngettext(s,p,n) ((n) == 1 ? (s) : (p))
#define dngettext(d,s,p,n) ((n) == 1 ? (s) : (p))
#endif

#define _(x) gettext(x)

/*
 *	Use this to mark string constants as needing translation at some later
 *	time, rather than immediately.  This is useful for cases where you need
 *	access to the original string and translated string, and for cases where
 *	immediate translation is not possible, like when initializing global
 *	variables.
 *		http://www.gnu.org/software/autoconf/manual/gettext/Special-cases.html
 */
#define gettext_noop(x) (x)


/* ----------------------------------------------------------------
 *				Section 9: system-specific hacks
 *
 *		This should be limited to things that absolutely have to be
 *		included in every source file.  The port-specific header file
 *		is usually a better place for this sort of thing.
 * ----------------------------------------------------------------
 *
/* /port compatibility functions */
/* excerpt from port.h */
extern int	pg_strncasecmp(const char *s1, const char *s2, size_t n);

#endif							/* C_H */
