/**
 * @file pqparse.c
 * @brief parse statement strings
 * @date 2017-08-28
 * @author Thomas Krennwallner <krennwallner@ximes.com>
 * @copyright Copyright (c) 2015-2017, XIMES GmbH
 * @see https://www.postgresql.org/docs/current/static/libpq-exec.html
 */

#define DLL_EXPORT
#include "pqbinfmt_config.h"

#include "fe_utils/postgres_fe.h"
#include "libpq-fe.h"

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>

#include "fe_utils/psqlscan.h"

#include "pqparse.h"




typedef struct pqparse_state
{
	PsqlScanState sstate;
	PQExpBufferData scan_buf;
	const char * const *variables;
	char **statements;
	int unknown_variables;
	size_t alloc_statements;
	size_t index;
} pqparse_state;

/*
 * Simple error-printing function, might be needed by lexer
 */
static void
pqparse_error(const char *fmt,...)
{
	va_list		ap;

	fflush(stdout);
	va_start(ap, fmt);
	vfprintf(stderr, _(fmt), ap);
	va_end(ap);
}


/* Fetch value of a variable, as a free'able string; NULL if unknown */
/* This pointer can be NULL if no variable substitution is wanted */
static char*
pqparse_get_variable(const char *varname, PsqlScanQuoteType quote, void *passthrough)
{
	if (passthrough == NULL)
		return NULL;

	pqparse_state *pstate = passthrough;
	const char * const *variables = pstate->variables;
	const char * const *var;
	int i;
	char buf[64];

	for (i = 0, var = variables; var && *var != NULL; var++, i++)
	{
		if (strcasecmp(*var, varname) != 0) /* compare case insensitive, :VarName == :varname */
			continue;

		/* :variables[i] => $(i+1) */
		int n = snprintf(buf, 64, "$%i", i + 1);
		if (n < 0 || n >= 64)
		{
			pstate->unknown_variables++;
			return NULL;
		}

		buf[n] = '\0';

		switch (quote)
		{
		case PQUOTE_PLAIN:			/* just return the actual value */
		case PQUOTE_SQL_LITERAL:	/* :'{variable_char}+' add quotes to make a valid SQL literal */
		case PQUOTE_SQL_IDENT:		/* :"{variable_char}+" quote if needed to make a SQL identifier */
		case PQUOTE_SHELL_ARG:		/* quote if needed to be safe in a shell cmd */
			return pg_strdup(buf);
		}

		break;
	}
	
	pstate->unknown_variables++;
	return NULL;
}


/* callback functions for psqlscan */
static const PsqlScanCallbacks pqparse_callbacks =
{
	pqparse_get_variable,
	pqparse_error
};




#define ALLOC_BLOCK 128

/* Initialize pqparse_state with a null-terminated list of psql-style variable names.
 * variables must be a valid pointer throughout the parsing process.
 */
DECLSPEC pqparse_state *
pqparse_init(const char * const * variables)
{
	pqparse_state *pstate = malloc(sizeof(pqparse_state));
	if (pstate == NULL)
	{
		return NULL;
	}

	pstate->sstate = psql_scan_create(&pqparse_callbacks);
	initPQExpBuffer(&pstate->scan_buf);

	pstate->variables = variables;
	pstate->unknown_variables = 0;
	
	pstate->alloc_statements = ALLOC_BLOCK;
	pstate->statements = (char **) malloc(sizeof(char *) * ALLOC_BLOCK);
	pstate->index = 0;

	/* set context for pqparse_get_variable */
	psql_scan_set_passthrough(pstate->sstate, (void *)pstate);
	
	return pstate;
}


/* returns number currently parsed statements in pstate->statements */
DECLSPEC size_t
pqparse_num_statements(pqparse_state *pstate)
{
	return pstate ? pstate->index : 0;
}

/* returns number of unknown variables */
DECLSPEC int
pqparse_num_unknown_variables(pqparse_state *pstate)
{
	return pstate ? pstate->unknown_variables : -1;
}

/* returns currently parsed statements pstate->statements */
DECLSPEC const char * const *
pqparse_get_statements(pqparse_state *pstate)
{
	return pstate ? pstate->statements : NULL;
}


/* destroy parser and parsed statements pstate->statements */
DECLSPEC void
pqparse_destroy(pqparse_state *pstate)
{
	if (pstate == NULL)
		return;

	if (pstate->sstate)
	{
		psql_scan_finish(pstate->sstate);
		psql_scan_destroy(pstate->sstate);
		pstate->sstate = NULL;
	}

	termPQExpBuffer(&pstate->scan_buf);

	if (pstate->statements)
	{
		for (char **stm = pstate->statements; stm && *stm != NULL; stm++)
		{
			free(*stm);
		}
		free(pstate->statements);
		pstate->statements = NULL;
	}

	pstate->alloc_statements = 0;
	pstate->index = 0;
	pstate->variables = NULL;
	pstate->unknown_variables = 0;
}


/* parse a list of statements stored in buffer and add them to pstate->statements.
 * returns 0 when parsing buffer is complete, 1 if parsing requires more input, and -1
 * if buffer contains an invalid list of query statements.
 */
DECLSPEC int
pqparse_add_statements(pqparse_state *pstate, const char *buffer)
{
	const char *b;
	size_t len;
	PsqlScanResult sr;
	promptStatus_t prompt;
	int was_incomplete;
	size_t old_index;

	if (pstate == NULL || pstate->sstate == NULL || pstate->statements == NULL || pstate->unknown_variables)
		return -1;

	if (pstate->scan_buf.len > 0) /* previous scan was incomplete, restart */
	{
		appendPQExpBufferStr(&pstate->scan_buf, buffer);

		b = pg_strdup(pstate->scan_buf.data);
		len = pstate->scan_buf.len;
		was_incomplete = 1;

		/* output buffer is processed */
		resetPQExpBuffer(&pstate->scan_buf);
	}
	else
	{
		b = buffer;
		len = strlen(buffer);
		was_incomplete = 0;
	}

	/* we force encoding = 6 (UTF-8) and stdstrings = true */
	psql_scan_setup(pstate->sstate, b, len, 6, true);
	
	/* save current statement index */
	old_index = pstate->index;

	do
	{
		/* parse the next statement */
		prompt = PROMPT_READY;
		sr = psql_scan(pstate->sstate, &pstate->scan_buf, &prompt);		

		/* could not substitute variables, or reached PSCAN_EOL || PSCAN_INCOMPLETE || PSCAN_BACKSLASH */
		if (sr != PSCAN_SEMICOLON || pstate->unknown_variables)
			break;

		/* collect the next SQL statement */
		pstate->statements[pstate->index] = pg_strdup(pstate->scan_buf.data);
		pstate->index++;
		
		/* allocate the next block in our statement array */
		if (pstate->index >= pstate->alloc_statements)
		{
			pstate->alloc_statements += ALLOC_BLOCK;
			char **tmp = (char **) realloc(pstate->statements, sizeof(char *) * pstate->alloc_statements);
			if (tmp == NULL)
			{
				return -1;
			}
			pstate->statements = tmp;
		}

		/* output buffer is processed */
		resetPQExpBuffer(&pstate->scan_buf);

	} while (1);

	/* we are finished with parsing b */
	psql_scan_finish(pstate->sstate);

	/* output buffer is processed */
	resetPQExpBuffer(&pstate->scan_buf);

	/* keep pstate->scan_buf intact for the next round */
	if ((sr == PSCAN_INCOMPLETE && prompt != PROMPT_READY) || (sr == PSCAN_EOL && prompt == PROMPT_CONTINUE))
	{
		/* rescue b, we might have replaced incomplete variable names that get expanded in the next round */
		appendPQExpBufferStr(&pstate->scan_buf, b);

		if (was_incomplete) /* did we copy b before? */
		{
			free((void *) b);
		}

		/* remove everything we have parsed so far, try to fix it in the next round */
		for (size_t i = old_index; i < pstate->index; i++)
		{
			if (pstate->statements[i])
			{
				free(pstate->statements[i]);
				pstate->statements[i] = NULL;
			}
		}

		/* reset index and unknown variables, the next round might work */
		pstate->index = old_index;
		pstate->unknown_variables = 0;

		/* indicates that we can retry to call pqparse_add_statements with a larger input */
		return 1;
	}

	if (was_incomplete) /* did we copy b? */
	{
		free((void *)b);
	}

	pstate->statements[pstate->index] = NULL;	

	/* 0: (PSCAN_EOL && !PROMPT_CONTINUE) || (PSCAN_INCOMPLETE && PROMPT_READY)
	 * -1: either PSCAN_BACKSLASH or variable mapping was missing, bail with error
	 */
	return sr != PSCAN_BACKSLASH && pstate->unknown_variables == 0 ? 0 : -1;
}