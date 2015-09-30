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
		#region encode datatype to binary message

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_text(IntPtr ptr, string s);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_bool(IntPtr ptr, int b);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_int8(IntPtr ptr, long i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_int4(IntPtr ptr, int i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_int2(IntPtr ptr, short i);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_float4(IntPtr ptr, float f);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_float8(IntPtr ptr, double d);

		[DllImport("libpqbinfmt.dll")]
		public static extern void setmsg_numeric(IntPtr ptr, double d);

		#endregion


		#region decode datatype from binary message

		[DllImport("libpqbinfmt.dll")]
		public static extern string getmsg_text(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern int getmsg_bool(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern long getmsg_int8(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern int getmsg_int4(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern short getmsg_int2(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern float getmsg_float4(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern double getmsg_float8(IntPtr ptr);

		[DllImport("libpqbinfmt.dll")]
		public static extern double getmsg_numeric(IntPtr ptr);

		#endregion
	}
}
