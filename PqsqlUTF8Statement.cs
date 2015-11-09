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
			int len = 0;
			while (Marshal.ReadByte(sp, len) != 0x0) len++; // strlen()
			byte[] b = new byte[len];
			Marshal.Copy(sp, b, 0, len);
			return Encoding.UTF8.GetString(b);
		}

	}
}