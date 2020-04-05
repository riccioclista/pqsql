using System;
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
			var t = Marshal.StringToCoTaskMemUTF8(text);

			try
			{
				PqsqlBinaryFormat.pqbf_set_text(p, (sbyte*) t.ToPointer());
			}
			finally
			{
				Marshal.ZeroFreeCoTaskMemUTF8(t);
			}
		}

		internal static unsafe void AddText(IntPtr pb, string text, uint oid)
		{
			var t = Marshal.StringToCoTaskMemUTF8(text);
			
			try
			{
				PqsqlBinaryFormat.pqbf_add_text(pb, (sbyte*) t.ToPointer(), oid);
			}
			finally
			{
				Marshal.ZeroFreeCoTaskMemUTF8(t);
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
			return Marshal.PtrToStringUTF8(p);
#else	
			int dummy = 0;
			IntPtr utp = PqsqlBinaryFormat.pqbf_get_unicode_text(p, &dummy);

			return Marshal.PtrToStringUni(utp);
#endif
		}

	}
}