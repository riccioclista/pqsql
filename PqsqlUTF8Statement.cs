using System;
#if CODECONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Text;

namespace Pqsql
{
	internal static class PqsqlUTF8Statement
	{

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
		internal static unsafe string CreateStringFromUTF8(byte *p)
		{
			if (p == null)
				return null;

			int pos = 0;
			int buflen = 64; // must be power of two
			byte[] buf = new byte[buflen];

			while (*p != 0x0)
			{
				if (pos >= buflen)
				{
					buflen <<= 1; // exponential growth strategy
					Array.Resize(ref buf, buflen);
#if CODECONTRACTS
					Contract.Assume(pos < buf.Length);
#endif
				}

				buf[pos++] = *p++;
			}

			return Encoding.UTF8.GetString(buf, 0, pos);
		}

	}
}