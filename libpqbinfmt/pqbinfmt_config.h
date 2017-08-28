/**
 * @file pqbinfmt_config.h
 * @brief utility macros
 * @date 2015-09-30
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 */

#ifndef __PQ_BINFMT_CONFIG_H
#define __PQ_BINFMT_CONFIG_H

#if defined _WIN32
	#if defined DLL_EXPORT
		#define DECLSPEC __declspec(dllexport)
	#else
		#define DECLSPEC __declspec(dllimport)
	#endif /* DLL_EXPORT */
#else
	#ifndef DECLSPEC
		#define DECLSPEC
	#endif /* DECLSPEC */
#endif /* _WIN32 */

#ifdef _WIN32
	/* BYTESWAP for network byte order */
	#define BYTESWAP2(x) _byteswap_ushort(x)
	#define BYTESWAP4(x) _byteswap_ulong(x)
	#define BYTESWAP8(x) _byteswap_uint64(x)

	#define strcasecmp(s1,s2) _stricmp(s1, s2)
#endif /* _WIN32 */

#ifndef __has_builtin /* Optional of course */
	#define __has_builtin(x) 0 /* Compatibility with non-clang compilers */
#endif

#if (defined(__clang__) && __has_builtin(__builtin_bswap32) && __has_builtin(__builtin_bswap64)) \
		|| (defined(__GNUC__ ) && (__GNUC__ > 4 || (__GNUC__ == 4 && __GNUC_MINOR__ >= 3)))
	#define BYTESWAP2(x) __builtin_bswap16(x)
	#define BYTESWAP4(x) __builtin_bswap32(x)
	#define BYTESWAP8(x) __builtin_bswap64(x)
#endif /* GCC or CLANG */


/* guards */

#define BAILIFNULL(p) do { if (p == NULL) return; } while(0)
#define BAILWITHVALUEIFNULL(p,val) do { if (p == NULL) return val; } while(0)

#endif /* __PQ_BINFMT_CONFIG_H */