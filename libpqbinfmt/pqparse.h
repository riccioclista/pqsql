/**
 * @file pqparse.h
 * @brief parse statement strings
 * @date 2017-08-28
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 */

#ifndef __PQ_PARSE_H
#define __PQ_PARSE_H

#include "pqbinfmt_config.h"

#ifdef  __cplusplus
extern "C" {
#endif

typedef struct pqparse_state pqparse_state;

extern DECLSPEC pqparse_state *pqparse_init(const char * const *variables);

extern DECLSPEC size_t pqparse_num_statements(pqparse_state *pstate);

extern DECLSPEC const char * const *pqparse_get_statements(pqparse_state *pstate);

extern DECLSPEC int pqparse_num_unknown_variables(pqparse_state *pstate);

extern DECLSPEC int pqparse_add_statements(pqparse_state *pstate, const char *buffer);

extern DECLSPEC void pqparse_destroy(pqparse_state *pstate);

#ifdef  __cplusplus
}
#endif

#endif /* __PQ_PARSE_H */