using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Text;

using PqsqlBinaryFormat = Pqsql.UnsafeNativeMethods.PqsqlBinaryFormat;

namespace Pqsql
{
	internal static class PqsqlUTF8Statement
	{
		internal static unsafe void SetText(IntPtr p, string text)
		{
			var t = StringToCoTaskMemUTF8(text);

			try
			{
				PqsqlBinaryFormat.pqbf_set_text(p, (sbyte*) t.ToPointer());
			}
			finally
			{
				ZeroFreeCoTaskMemUTF8(t);
			}
		}

		internal static unsafe void AddText(IntPtr pb, string text, uint oid)
		{
			var t = StringToCoTaskMemUTF8(text);
			
			try
			{
				PqsqlBinaryFormat.pqbf_add_text(pb, (sbyte*) t.ToPointer(), oid);
			}
			finally
			{
				ZeroFreeCoTaskMemUTF8(t);
			}
		}

		// return static UTF8-encoded statement including trailing 0 byte
		internal static byte[] CreateUTF8Statement(string s)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(s != null);
#else
			if (s == null)
				throw new ArgumentNullException(nameof(s));
#endif

			byte[] b = Encoding.UTF8.GetBytes(s); // not null terminated
			int blen = b.Length;
			Array.Resize(ref b, blen + 1);
			b[blen] = 0; // null-terminate s
			return b;
		}

		// return static UTF8-encoded statement including trailing 0 byte
		internal static byte[] CreateUTF8Statement(StringBuilder sb)
		{
#if CODECONTRACTS
			Contract.Requires<ArgumentNullException>(sb != null);
#else
			if (sb == null)
				throw new ArgumentNullException(nameof(sb));
#endif

			return CreateUTF8Statement(sb.ToString());
		}

		// converts null-terminated byte* to string 
		internal static unsafe string CreateStringFromUTF8(IntPtr p)
		{
			if (p == IntPtr.Zero)
				return null;

#if !WIN32
			return PtrToStringUTF8(p);
#else	
			int dummy = 0;
			IntPtr utp = PqsqlBinaryFormat.pqbf_get_unicode_text(p, &dummy);

			return Marshal.PtrToStringUni(utp);
#endif
		}

		// from https://github.com/dotnet/runtime: /src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshal.cs
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe IntPtr StringToCoTaskMemUTF8(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = Encoding.UTF8.GetMaxByteCount(s.Length);

            IntPtr pMem = Marshal.AllocCoTaskMem(nb + 1);

            int nbWritten;
            byte* pbMem = (byte*)pMem;

            fixed (char* firstChar = s)
            {
                nbWritten = Encoding.UTF8.GetBytes(firstChar, s.Length, pbMem, nb);
            }

            pbMem[nbWritten] = 0;

            return pMem;
        }

		// from https://github.com/dotnet/runtime: /src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshal.cs
		private static unsafe void ZeroFreeCoTaskMemUTF8(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }

			
			// zero memory
			for (var b = (byte*) s; *b != (byte) '\0'; b++)
			{
				*b = 0;
			}
			
            Marshal.FreeCoTaskMem(s);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe string PtrToStringUTF8(IntPtr ptr)
        {
            if (IsNullOrWin32Atom(ptr))
            {
                return null;
            }

			// strlen
			var start = (byte*) ptr;
			var b = start;
			for (; *b != (byte) '\0'; b++)
			{
			}

			var len = (int) (b - start);
			return PtrToStringUTF8(ptr, len);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe string PtrToStringUTF8(IntPtr ptr, int len)
        {
			if (len < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(len));
			}

            if (IsNullOrWin32Atom(ptr))
            {
                return null;
            }

			return Encoding.UTF8.GetString((byte*) ptr, len);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsNullOrWin32Atom(IntPtr ptr)
        {
            const long HIWORDMASK = unchecked((long)0xffffffffffff0000L);

            long lPtr = (long)ptr;
            return 0 == (lPtr & HIWORDMASK);
        }
	}
}