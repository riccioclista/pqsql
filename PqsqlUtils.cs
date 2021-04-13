using System;
using System.Runtime.CompilerServices;

namespace Pqsql
{
	public static class PqsqlUtils
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool ReadBool(ref IntPtr ptr)
		{
			var p = (sbyte*) ptr;
			var val = *p == 1;
			ptr += sizeof(sbyte);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe short ReadInt16(ref IntPtr ptr)
		{
			var p = (short*) ptr;
			var val = SwapBytes(*p);
			ptr += sizeof(short);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int ReadInt32(ref IntPtr ptr)
		{
			var p = (int*) ptr;
			var val = SwapBytes(*p);
			ptr += sizeof(int);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe uint ReadUInt32(ref IntPtr ptr)
		{
			var p = (uint*) ptr;
			var val = SwapBytes(*p);
			ptr += sizeof(uint);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long ReadInt64(ref IntPtr ptr)
		{
			var p = (long*) ptr;
			var val = SwapBytes(*p);
			ptr += sizeof(long);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong ReadUInt64(ref IntPtr ptr)
		{
			var p = (ulong*) ptr;
			var val = SwapBytes(*p);
			ptr += sizeof(ulong);
			return val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe float ReadFloat32(ref IntPtr ptr)
		{
			var p = (uint*) ptr;
			var val = SwapBytes(*p);
			var f = (float*) &val;
			ptr += sizeof(float);
			return *f;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double ReadFloat64(ref IntPtr ptr)
		{
			var p = (ulong*) ptr;
			var val = SwapBytes(*p);
			var d = (double*) &val;
			ptr += sizeof(double);
			return *d;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short SwapBytes(short x) => (short) SwapBytes((ushort) x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SwapBytes(int x) => (int) SwapBytes((uint) x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long SwapBytes(long x) => (long) SwapBytes((ulong) x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort SwapBytes(ushort x) =>	(ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SwapBytes(uint x)
		{
			// swap adjacent 16-bit blocks
			x = (x >> 16) | (x << 16);
			// swap adjacent 8-bit blocks
			return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong SwapBytes(ulong x)
		{
			// swap adjacent 32-bit blocks
			x = (x >> 32) | (x << 32);
			// swap adjacent 16-bit blocks
			x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
			// swap adjacent 8-bit blocks
			return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
		}
	}
}
