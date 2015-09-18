using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Pqsql
{
	class PqsqlTextFormat
	{
		#region encode datatype to text message

		public static void SendText(ref IntPtr val, string s)
		{
			Marshal.Copy(s.ToArray<char>(), 0, val, s.Length);
		}

		public static void SendType<T>(ref IntPtr val, T t)
		{
			string s = t.ToString();
			Marshal.Copy(s.ToArray<char>(), 0, val, s.Length);
		}

		#endregion


		#region decode datatype from text message

		public static string GetMsgString(IntPtr val)
		{
			return Marshal.PtrToStringAnsi(val);
		}

		public static int GetMsgInt(IntPtr val, int b)
		{
			string s = Marshal.PtrToStringAnsi(val);

			switch (b)
			{
				case 4:
					int res_i;
					int.TryParse(s, out res_i);
					return res_i;
				case 2:
					short res_s;
					short.TryParse(s, out res_s);
					return res_s;
				case 1:
					byte res_b;
					byte.TryParse(s, out res_b);
					return res_b;
			}

			throw new InvalidCastException("unsupported integer size " + b.ToString());
		}

		public static long GetMsgInt64(IntPtr val)
		{
			string s = Marshal.PtrToStringAnsi(val);
			long res;

			if (long.TryParse(s, out res))
				return res;

			throw new InvalidCastException("GetMsgInt64");
		}

		public static float GetMsgFloat4(IntPtr val)
		{
			string s = Marshal.PtrToStringAnsi(val);
			float res;

			if (float.TryParse(s, out res))
				return res;

			throw new InvalidCastException("GetMsgFloat4");
		}

		public static double GetMsgFloat8(IntPtr val)
		{
			string s = Marshal.PtrToStringAnsi(val);
			double res;

			if (double.TryParse(s, out res))
				return res;

			throw new InvalidCastException("GetMsgFloat8");
		}

		public static decimal GetNumeric(IntPtr val)
		{
			string s = Marshal.PtrToStringAnsi(val);
			decimal res;

			if (decimal.TryParse(s, out res))
				return res;

			throw new InvalidCastException("GetNumeric");
		}

		public static DateTime GetDateTime(IntPtr val)
		{
			string s = Marshal.PtrToStringAnsi(val);
			DateTime res;

			if (DateTime.TryParse(s, out res))
				return res;

			throw new InvalidCastException("GetDate");
		}

		#endregion
	}
}
