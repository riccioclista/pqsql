using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pqsql
{
	public static class PqsqlUTF8Statement
	{

		// return static UTF8-encoded statement including trailing 0 byte
		public static byte[] CreateUTF8Statement(string s)
		{
			byte[] b = Encoding.UTF8.GetBytes(s); // not null terminated
			int blen = b.Length;
			Array.Resize(ref b, blen + 1);
			b[blen] = 0; // null-terminate s
			return b;
		}

		// return static UTF8-encoded statement including trailing 0 byte
		public static byte[] CreateUTF8Statement(StringBuilder sb)
		{
			return CreateUTF8Statement(sb.ToString());
		}

		// converts null-terminated sbyte* to string 
		public static string CreateStringFromUTF8(IntPtr sp)
		{
			int pos = 0;
			int buflen = 64; // must be power of two
			byte[] buf = new byte[buflen];

			unsafe
			{
				byte* s = (byte*) sp.ToPointer();

				while (*s != 0x0)
				{
					if (pos >= buflen)
					{
						buflen <<= 1; // exponential growth strategy
						Array.Resize(ref buf, buflen);
					}

					buf[pos++] = *s++;
				}
			}

			return Encoding.UTF8.GetString(buf, 0, pos);
		}

	}
}