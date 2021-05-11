/*-------------------------------------------------------------------------
 *
 * datetime.c
 *	  Support functions for date/time types.
 *
 * Portions Copyright (c) 1996-2017, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/utils/adt/datetime.c
 *
 *-------------------------------------------------------------------------
 */

#define HAVE_INT64_TIMESTAMP
#include "pgadt/datetime.h"
#include "pgadt/timestamp.h"


/*
 * Calendar time to Julian date conversions.
 * Julian date is commonly used in astronomical applications,
 *	since it is numerically accurate and computationally simple.
 * The algorithms here will accurately convert between Julian day
 *	and calendar date for all non-negative Julian days
 *	(i.e. from Nov 24, -4713 on).
 *
 * Rewritten to eliminate overflow problems. This now allows the
 * routines to work correctly for all Julian day counts from
 * 0 to 2147483647	(Nov 24, -4713 to Jun 3, 5874898) assuming
 * a 32-bit integer. Longer types should also work to the limits
 * of their precision.
 *
 * Actually, date2j() will work sanely, in the sense of producing
 * valid negative Julian dates, significantly before Nov 24, -4713.
 * We rely on it to do so back to Nov 1, -4713; see IS_VALID_JULIAN()
 * and associated commentary in timestamp.h.
 */

int
date2j(int y, int m, int d)
{
	int			julian;
	int			century;

	if (m > 2)
	{
		m += 1;
		y += 4800;
	}
	else
	{
		m += 13;
		y += 4799;
	}

	century = y / 100;
	julian = y * 365 - 32167;
	julian += y / 4 - century + century / 4;
	julian += 7834 * m / 256 + d;

	return julian;
}								/* date2j() */

void
j2date(int jd, int *year, int *month, int *day)
{
	unsigned int julian;
	unsigned int quad;
	unsigned int extra;
	int			y;

	julian = jd;
	julian += 32044;
	quad = julian / 146097;
	extra = (julian - quad * 146097) * 4 + 3;
	julian += 60 + quad * 3 + extra / 146097;
	quad = julian / 1461;
	julian -= quad * 1461;
	y = julian * 4 / 1461;
	julian = ((y != 0) ? ((julian + 305) % 365) : ((julian + 306) % 366))
		+ 123;
	y += quad * 4;
	*year = y - 4800;
	quad = julian * 2141 / 65536;
	*day = julian - 7834 * quad / 256;
	*month = (quad + 10) % MONTHS_PER_YEAR + 1;

	return;
}								/* j2date() */
