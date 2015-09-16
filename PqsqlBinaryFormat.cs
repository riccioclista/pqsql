using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;

namespace Pqsql
{
	[StructLayout(LayoutKind.Explicit)]
	internal struct swap_float4_int4_union
	{
		[FieldOffset(0)]
		public uint as_unsigned;

		[FieldOffset(0)]
		public float as_single_precision;
	}


	[StructLayout(LayoutKind.Explicit)]
	internal struct swap_float8_int8_union
	{
		[FieldOffset(0)]
		public ulong as_unsigned;

		[FieldOffset(0)]
		public double as_double_precision;
	}


	[StructLayout(LayoutKind.Sequential)]
	public struct numeric_meta
	{
		public ushort ndigits; // digits are base 10000
		public ushort weight;  //
		public ushort sign;    // + / - / NaN
		public ushort dscale;  // digits after .
	}

	//#define NBASE       10000
 //   #define HALF_NBASE  5000
  //  #define DEC_DIGITS  4           /* decimal digits per NBASE digit */
 //   #define MUL_GUARD_DIGITS    2   /* these are measured in NBASE digits */
 //   #define DIV_GUARD_DIGITS    4
	//   #define NUMERIC_POS         0x0000
   //#define NUMERIC_NEG         0x4000
   //#define NUMERIC_SHORT       0x8000
   //#define NUMERIC_NAN         0xC000

	//
	// Routines for formatting and parsing frontend/backend binary messages
	//
	public unsafe class PqsqlBinaryFormat
	{
		#region encode datatype to binary message

		// see pq_sendtext(StringInfo buf, const char *str, int slen) from src/backend/libpq/pqformat.c
		public static void SendText(ref IntPtr val, string s)
		{
			Marshal.StructureToPtr(s, val, true);
		}


		// see pq_sendint(StringInfo buf, int i, int b) from src/backend/libpq/pqformat.c
		public static void SendInt(ref IntPtr val, int i, int b)
		{
       switch (b)
       {
				 case 1:
					 val = (IntPtr)(byte)i;
					 break;
				 case 2:
					 val = (IntPtr)(ushort)IPAddress.HostToNetworkOrder((ushort)i);
					 break;
				 case 4:
					 val = (IntPtr)(uint)IPAddress.HostToNetworkOrder((uint)i);
					 break;
				 default:
					 throw new InvalidCastException("unsupported integer size " + b.ToString());
			 }
		}


		// see pq_sendint64(StringInfo buf, int64 i) from src/backend/libpq/pqformat.c
		public static void SendInt64(ref IntPtr val, long i)
		{
			uint h32;
			uint l32;

			/* High order half first, since we're doing MSB-first */
			h32 = (uint)IPAddress.HostToNetworkOrder((uint)(i >> 32));

			/* Now the low order half */
			l32 = (uint)IPAddress.HostToNetworkOrder((uint)i);

			i = l32;
			i <<= 32;
			i |= h32;

			val = (IntPtr)i;
		}


		// see void pq_sendfloat4(StringInfo msg, float4 f) from src/backend/libpq/pqformat.c
		public static void SendFloat4(ref IntPtr val, float f)
		{
			swap_float4_int4_union onion = new swap_float4_int4_union();
			onion.as_single_precision = f;
			val = (IntPtr)IPAddress.HostToNetworkOrder(onion.as_unsigned);
		}


		// see void pq_sendfloat8(StringInfo msg, float8 f) from src/backend/libpq/pqformat.c
		public static void SendFloat8(ref IntPtr val, double d)
		{
			swap_float8_int8_union onion = new swap_float8_int8_union();
			onion.as_double_precision = d;
			SendInt64(ref val, (long)onion.as_unsigned);
		}


		// see  numeric_send() from src/backend/utils/adt/numeric.c
		public static void SendNumeric(ref IntPtr val, decimal d)
		{
			// requires 8 + ndigits bytes of memory
			numeric_meta meta = new numeric_meta();
			
			int[] b = decimal.GetBits(d); //

			meta.ndigits = 1;
			meta.weight = 2;

			meta.sign = 0;  // 0...+
			if (d < 0.0m)
			{
				meta.sign = (ushort)IPAddress.HostToNetworkOrder(0x4000); // 0x4000...-
			}

			meta.dscale = 3;
		}


		// see  numeric_send() from src/backend/utils/adt/numeric.c
		public static void SendNumeric(ref IntPtr val, double d)
		{
			// requires 8 + ndigits bytes of memory
			numeric_meta meta = new numeric_meta();

			meta.ndigits = 1;
			meta.weight = 2;

			meta.sign = 0;  // 0...+
			if (double.IsNaN(d))
			{
				meta.sign = (ushort)IPAddress.HostToNetworkOrder(0xC000); // 0xC000...NaN
			}
			else if (d < 0)
			{
				meta.sign = (ushort)IPAddress.HostToNetworkOrder(0x4000); // 0x4000...-
			}
			
			meta.dscale = 3;
		}

		#endregion


		#region decode datatype from binary message

		// see char* pq_getmsgstring(StringInfo msg) from pqformat.c
		public static string GetMsgString(IntPtr val)
		{
			return Marshal.PtrToStringAnsi(val);
		}


		// see int pq_getmsgint(StringInfo msg) from pqformat.c 	
		public static int GetMsgInt(IntPtr val, int b)
		{
			uint res;
			void* v = (void*)val;

			switch (b)
			{
				case 1:
					res = *(byte*)v;
					break;
				case 2:
					res = (uint)IPAddress.NetworkToHostOrder(*(ushort*)v);
					break;
				case 4:
					res = (uint)IPAddress.NetworkToHostOrder(*(uint*)v);
					break;
				default:
					throw new InvalidCastException("unsupported integer size " + b.ToString());
			}

			return (int)res;
		}


		// see int pq_getmsgint64(StringInfo msg) from pqformat.c 	
		public static long GetMsgInt64(IntPtr val)
		{
			long res;
			uint h32;
			uint l32;
			void* hv = (void*)val;
			void* lv = (void*)(val + 4);

			h32 = (uint)IPAddress.NetworkToHostOrder(*(uint*)hv);
			l32 = (uint)IPAddress.NetworkToHostOrder(*(uint*)lv);

			res = h32;
			res <<= 32;
			res |= l32;

			return res;
		}


		// see float4 pq_getmsgfloat4(StringInfo msg) from pqformat.c 	
		public static float GetMsgFloat4(IntPtr val)
		{
			swap_float4_int4_union onion = new swap_float4_int4_union();
			onion.as_unsigned = (uint)IPAddress.NetworkToHostOrder(*((int*)((void*)val)));
			return onion.as_single_precision;
		}


		// see float8 pq_getmsgfloat8(StringInfo msg) from pqformat.c 	
		public static double GetMsgFloat8(IntPtr val)
		{
			swap_float8_int8_union onion = new swap_float8_int8_union();
			onion.as_unsigned = (ulong)IPAddress.NetworkToHostOrder(*((long*)((void*)val)));
			return onion.as_double_precision;
		}


		// see  numeric_recv() from src/backend/utils/adt/numeric.c
		public static decimal GetNumeric(IntPtr val)
		{
			decimal d = 0;
			ushort ndigits;
			ushort weight;
			ushort sign;
			ushort dscale;
			numeric_meta* meta = (numeric_meta*)val;
			ushort* digits = (ushort*)((void*)(val + sizeof(numeric_meta)));
			uint digit = 0;

			ndigits = (ushort)IPAddress.NetworkToHostOrder(meta->ndigits);
			weight = (ushort)IPAddress.NetworkToHostOrder(meta->weight);
			dscale = (ushort)IPAddress.NetworkToHostOrder(meta->dscale);

			sign = (ushort)IPAddress.NetworkToHostOrder(meta->ndigits);  // 0...+
			if (sign == 0x4000) // 0x4000...-
			{
				// todo do somethign
			}
			else if (sign == 0xC000) // 0xC000...NaN
			{
				throw new NotFiniteNumberException("NaN is not supported with decimal type");
			}

			for (int i = 0; i < ndigits; i++)
			{
				// todo run through digits buffer
				digit = (uint)IPAddress.NetworkToHostOrder(*(ushort*)digits);
				digits += sizeof(ushort);
			}

			return d;
		}


		#endregion
	}
}
