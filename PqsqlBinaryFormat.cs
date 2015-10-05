using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;

namespace Pqsql
{
	//
	// Routines for formatting and parsing frontend/backend binary messages
	//
	public unsafe class PqsqlBinaryFormat
	{
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


		#region encode datatype to binary message

		[DllImport("libpqbinfmt.dll")]
		public static unsafe extern void pqbf_set_null(IntPtr pb, uint oid);

		[DllImport("libpqbinfmt.dll")]
		public static unsafe extern int pqbf_set_unicode_text(IntPtr pb, char* t);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_free_unicode_text(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_bool(IntPtr pb, int b);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int8(IntPtr pb, long i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int4(IntPtr pb, int i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_int2(IntPtr pb, short i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_float4(IntPtr pb, float f);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_float8(IntPtr pb, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void pqbf_set_numeric(IntPtr pb, double d);

		#endregion


		#region decode datatype from binary message

		[DllImport("libpqbinfmt.dll")]
		public static unsafe extern IntPtr pqbf_get_text(IntPtr p, int* len);

		[DllImport("libpqbinfmt.dll")]
		public static unsafe extern IntPtr pqbf_get_unicode_text(IntPtr p, int* len);

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
		public static extern double pqbf_get_numeric(IntPtr p);

		#endregion
	}
}
