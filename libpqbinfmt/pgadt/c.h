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
 * Portions Copyright (c) 1996-2016, PostgreSQL Global Development Group
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
 *		1)		hacks to cope with non-ANSI C compilers
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
#define TRUE    1
#endif

#ifndef FALSE
#define FALSE   0
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
typedef int64_t int64;
typedef uint64_t uint64;

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


#define VARHDRSZ                ((int32) sizeof(int32))

/*
 * Array indexing support
 */
#define MAXDIM 6


/* ----------------------------------------------------------------
 *				Section 6:	assertions
 * ----------------------------------------------------------------
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

/* Get a bit mask of the bits set in non-long aligned addresses */
#define LONG_ALIGN_MASK (sizeof(long) - 1)

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



#endif /* C_H */