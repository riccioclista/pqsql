using System;
using System.Runtime.InteropServices;

namespace Pqsql
{
	//
	// Routines for formatting and parsing frontend/backend binary messages
	//
	public sealed unsafe class PqsqlBinaryFormat
	{
		#region unix timestamp 0 in ticks
		
		// DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
		public const long UnixEpochTicks = 621355968000000000;
		
		#endregion

		#region PQExpBuffer

		[DllImport("libpqbinfmt.dll")]
		public static extern long pqbf_get_buflen(IntPtr s);

		[DllImport("libpqbinfmt.dll")]
		public static extern sbyte* pqbf_get_bufval(IntPtr s);

		#endregion

		#region interface to pqparam_buffer

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_create();
		[DllImport("libpqbinfmt.dll")]
		public static extern void pqpb_free(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern void pqpb_reset(IntPtr pb);


		[DllImport("libpqbinfmt.dll")]
		public static extern int pqpb_get_num(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_get_types(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_get_vals(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_get_lens(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_get_frms(IntPtr pb);
		[DllImport("libpqbinfmt.dll")]
		public static extern uint pqpb_get_type(IntPtr pb, int i);
		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqpb_get_val(IntPtr pb, int i);
		[DllImport("libpqbinfmt.dll")]
		public static extern int pqpb_get_len(IntPtr pb, int i);

		#endregion


		#region encode datatype as binary parameter

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_null(IntPtr pb, uint oid);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqbf_add_unicode_text(IntPtr pb, char* t);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_bool(IntPtr pb, int b);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_bytea(IntPtr pb, sbyte* buf, ulong len);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_int8(IntPtr pb, long i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_int4(IntPtr pb, int i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_oid(IntPtr pb, uint i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_int2(IntPtr pb, short i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_float4(IntPtr pb, float f);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_float8(IntPtr pb, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_numeric(IntPtr pb, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_interval(IntPtr pbb, long offset, int day, int month);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_add_timestamp(IntPtr pbb, long sec, int usec);

		#endregion


		#region encode datatype to binary PQExpBuffer

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_null(IntPtr s, uint oid);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqbf_set_unicode_text(IntPtr s, char* t);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_bool(IntPtr s, int b);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_bytea(IntPtr s, sbyte* buf, ulong len);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int8(IntPtr s, long i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int4(IntPtr s, int i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_oid(IntPtr s, uint i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int2(IntPtr s, short i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_float4(IntPtr s, float f);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_float8(IntPtr s, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_numeric(IntPtr s, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_interval(IntPtr s, long offset, int day, int month);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_timestamp(IntPtr s, long sec, int usec);

		#endregion


		#region decode datatype from binary message

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqbf_get_text(IntPtr p, int* len);

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqbf_get_unicode_text(IntPtr p, int* len);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_free_unicode_text(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern byte pqbf_get_byte(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqbf_get_bool(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern long pqbf_get_int8(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqbf_get_int4(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern short pqbf_get_int2(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern float pqbf_get_float4(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern double pqbf_get_float8(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern double pqbf_get_numeric(IntPtr p, int typmod);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_get_bytea(IntPtr p, sbyte* buf, ulong len);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_get_interval(IntPtr p, long* offset, int* day, int* month);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_get_timestamp(IntPtr p, long* sec, int* usec);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqbf_get_date(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern long pqbf_get_time(IntPtr p);

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqbf_get_array(IntPtr p, int* ndim, int* flags, uint* oid, ref IntPtr dim, ref IntPtr lbound);

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqbf_get_array_value(IntPtr p, int* itemlen);


		#endregion


		#region interface to pqcopy_buffer

		[DllImport("libpqbinfmt.dll")]
		public static extern IntPtr pqcb_create(IntPtr conn, int num_cols);
		[DllImport("libpqbinfmt.dll")]
		public static extern void pqcb_free(IntPtr pc);
		[DllImport("libpqbinfmt.dll")]
		public static extern void pqcb_reset(IntPtr pc, int num_cols);
		
		[DllImport("libpqbinfmt.dll")]
		public static extern int pqcb_put_col(IntPtr pc, sbyte* val, uint len);

		[DllImport("libpqbinfmt.dll")]
		public static extern int pqcb_put_end(IntPtr pc);

		#endregion
	}
}
