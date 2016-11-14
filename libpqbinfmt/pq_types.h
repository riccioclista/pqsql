/*-------------------------------------------------------------------------
 *
 * pg_type.h
 *	  definition of the system "type" relation (pg_type)
 *	  along with the relation's initial contents.
 *
 *
 * Portions Copyright (c) 1996-2016, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/catalog/pg_type.h
 *
 * NOTES
 *	  the genbki.pl script reads this file and generates .bki
 *	  information from the DATA() statements.
 *
 *-------------------------------------------------------------------------
 */
/**
 * @file pq_types.h
 * @brief this is an excerpt from src/include/catalog/pg_type.h
 * @date 2015-10-01
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @see https://www.postgresql.org/docs/current/static/catalog-pg-type.html
 */

#ifndef __PQ_TYPES_H
#define __PQ_TYPES_H

/* ----------------
 *		initial contents of pg_type
 * ----------------
 */

/*
 * Keep the following ordered by OID so that later changes can be made more
 * easily.
 *
 * For types used in the system catalogs, make sure the values here match
 * TypInfo[] in bootstrap.c.
 */

/* OIDS 1 - 99 */
#define BOOLOID			16
#define BYTEAOID		17
#define CHAROID			18
#define NAMEOID			19
#define INT8OID			20
#define INT2OID			21
#define INT2VECTOROID	22
#define INT4OID			23
#define REGPROCOID		24
#define TEXTOID			25
#define OIDOID			26
#define TIDOID		27
#define XIDOID 28
#define CIDOID 29
#define OIDVECTOROID	30

/* hand-built rowtype entries for bootstrapped catalogs */
/* NB: OIDs assigned here must match the BKI_ROWTYPE_OID declarations */

/* OIDS 100 - 199 */
#define JSONOID 114
#define XMLOID 142
#define PGNODETREEOID	194

/* OIDS 200 - 299 */

/* OIDS 300 - 399 */

/* OIDS 400 - 499 */

/* OIDS 500 - 599 */

/* OIDS 600 - 699 */
#define POINTOID		600
#define LSEGOID			601
#define PATHOID			602
#define BOXOID			603
#define POLYGONOID		604
#define LINEOID			628

/* OIDS 700 - 799 */

#define FLOAT4OID 700
#define FLOAT8OID 701
#define ABSTIMEOID		702
#define RELTIMEOID		703
#define TINTERVALOID	704
#define UNKNOWNOID		705
#define CIRCLEOID		718
#define CASHOID 790

/* OIDS 800 - 899 */
#define MACADDROID 829
#define INETOID 869
#define CIDROID 650

/* OIDS 900 - 999 */

/* OIDS 1000 - 1099 */
#define INT2ARRAYOID		1005
#define INT4ARRAYOID		1007
#define TEXTARRAYOID		1009
#define OIDARRAYOID			1028
#define FLOAT4ARRAYOID 1021
#define ACLITEMOID		1033
#define CSTRINGARRAYOID		1263

#define BPCHAROID		1042
#define VARCHAROID		1043

#define DATEOID			1082
#define TIMEOID			1083

/* OIDS 1100 - 1199 */
#define TIMESTAMPOID	1114
#define TIMESTAMPTZOID	1184
#define INTERVALOID		1186

/* OIDS 1200 - 1299 */
#define TIMETZOID		1266

/* OIDS 1500 - 1599 */
#define BITOID	 1560
#define VARBITOID	  1562

/* OIDS 1600 - 1699 */

/* OIDS 1700 - 1799 */
#define NUMERICOID		1700
#define REFCURSOROID	1790

/* OIDS 2200 - 2299 */
#define REGPROCEDUREOID 2202
#define REGOPEROID		2203
#define REGOPERATOROID	2204
#define REGCLASSOID		2205
#define REGTYPEOID		2206
#define REGTYPEARRAYOID 2211

/*
 * pseudo-types
 *
 * types with typtype='p' represent various special cases in the type system.
 *
 * These cannot be used to define table columns, but are valid as function
 * argument and result types (if supported by the function's implementation
 * language).
 *
 * Note: cstring is a borderline case; it is still considered a pseudo-type,
 * but there is now support for it in records and arrays.  Perhaps we should
 * just treat it as a regular base type?
 */
#define RECORDOID		2249
#define RECORDARRAYOID	2287
#define CSTRINGOID		2275
#define ANYOID			2276
#define ANYARRAYOID		2277
#define VOIDOID			2278
#define TRIGGEROID		2279
#define EVTTRIGGEROID		3838
#define LANGUAGE_HANDLEROID		2280
#define INTERNALOID		2281
#define OPAQUEOID		2282
#define ANYELEMENTOID	2283
#define ANYNONARRAYOID	2776
#define ANYENUMOID		3500
#define FDW_HANDLEROID	3115
#define INDEX_AM_HANDLEROID 325
#define TSM_HANDLEROID	3310
#define ANYRANGEOID		3831

/* uuid */
#define UUIDOID 2950

/* pg_lsn */
#define LSNOID			3220

/* text search */
#define TSVECTOROID		3614
#define GTSVECTOROID	3642
#define TSQUERYOID		3615
#define REGCONFIGOID	3734
#define REGDICTIONARYOID	3769

/* jsonb */
#define JSONBOID 3802

/* range types */
#define INT4RANGEOID		3904

/*
 * pseudo-types
 *
 * types with typtype='p' represent various special cases in the type system.
 *
 * These cannot be used to define table columns, but are valid as function
 * argument and result types (if supported by the function's implementation
 * language).
 *
 * Note: cstring is a borderline case; it is still considered a pseudo-type,
 * but there is now support for it in records and arrays.  Perhaps we should
 * just treat it as a regular base type?
 */
#define RECORDOID		2249
#define RECORDARRAYOID	2287
#define CSTRINGOID		2275
#define ANYOID			2276
#define ANYARRAYOID		2277
#define VOIDOID			2278
#define TRIGGEROID		2279
#define EVTTRIGGEROID		3838
#define LANGUAGE_HANDLEROID		2280
#define INTERNALOID		2281
#define OPAQUEOID		2282
#define ANYELEMENTOID	2283
#define ANYNONARRAYOID	2776
#define ANYENUMOID		3500
#define FDW_HANDLEROID	3115
#define ANYRANGEOID		3831


/*
 * macros
 */
#define  TYPTYPE_BASE		'b' /* base type (ordinary scalar type) */
#define  TYPTYPE_COMPOSITE	'c' /* composite (e.g., table's rowtype) */
#define  TYPTYPE_DOMAIN		'd' /* domain over another type */
#define  TYPTYPE_ENUM		'e' /* enumerated type */
#define  TYPTYPE_PSEUDO		'p' /* pseudo-type */
#define  TYPTYPE_RANGE		'r' /* range type */

#define  TYPCATEGORY_INVALID	'\0'	/* not an allowed category */
#define  TYPCATEGORY_ARRAY		'A'
#define  TYPCATEGORY_BOOLEAN	'B'
#define  TYPCATEGORY_COMPOSITE	'C'
#define  TYPCATEGORY_DATETIME	'D'
#define  TYPCATEGORY_ENUM		'E'
#define  TYPCATEGORY_GEOMETRIC	'G'
#define  TYPCATEGORY_NETWORK	'I'		/* think INET */
#define  TYPCATEGORY_NUMERIC	'N'
#define  TYPCATEGORY_PSEUDOTYPE 'P'
#define  TYPCATEGORY_RANGE		'R'
#define  TYPCATEGORY_STRING		'S'
#define  TYPCATEGORY_TIMESPAN	'T'
#define  TYPCATEGORY_USER		'U'
#define  TYPCATEGORY_BITSTRING	'V'		/* er ... "varbit"? */
#define  TYPCATEGORY_UNKNOWN	'X'

/* Is a type OID a polymorphic pseudotype?	(Beware of multiple evaluation) */
#define IsPolymorphicType(typid)  \
	((typid) == ANYELEMENTOID || \
	 (typid) == ANYARRAYOID || \
	 (typid) == ANYNONARRAYOID || \
	 (typid) == ANYENUMOID || \
	 (typid) == ANYRANGEOID)

#endif /* __PQ_TYPES_H */