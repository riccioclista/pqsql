using System;
using System.Data.Common;

namespace Pqsql
{
	public sealed class PqsqlException : DbException
	{
		// hint string retrieved from query results
		private string mHint = string.Empty;


		public PqsqlException(string message)
			: base(message)
		{ }

		public PqsqlException(string message, Exception innerException)
			: base(message, innerException)
		{ }

		public PqsqlException(string message, IntPtr result)
			: base(message, CreateErrorCode(result))
		{
			mHint = CreateHint(result);
		}

		private unsafe string CreateHint(IntPtr result)
		{
			sbyte* hint = PqsqlWrapper.PQresultErrorField(result, PqsqlDiag.PG_DIAG_MESSAGE_HINT);
			return new string(hint);
		}

		// postgres error codes are stored as alphanumeric string of
		// 5 characters. we convert them to an integer of maximum 30 bits;
		// each character is stored within 6 bits of ErrorCode
		private static unsafe int CreateErrorCode(IntPtr result)
		{
			sbyte* sqlState = PqsqlWrapper.PQresultErrorField(result, PqsqlDiag.PG_DIAG_SQLSTATE);

			int code = 0; // error code '00000' means successful_completion

			if (sqlState != null)
			{
				for (int j = 0, c = *sqlState; j < 5 && c != 0; j++, c = *(++sqlState))
				{
					int i = 0;
					if (c >= 48 && c <= 57) // '0' ... '9' =>  0 ...  9
						i = c - 48;
					else if (c >= 65 && c <= 90) // 'A' ... 'Z' => 10 ... 35
						i = c - 55;

					// store each character in 6 bits from code
					code |= (i << j*6);
				}
			}

			return code;
		}

		// get SQLSTATE string from ErrorCode
		public string SqlState
		{
			get
			{
				char[] err = new char[5];
				int code = ErrorCode;
				for (int j = 0; j < 5; j++)
				{
					int i = (code >> j*6) & 0x3F;
					i = i < 65 ? i + 48 : i + 55;
					err[j] = (char) i;
				}
				return new string(err);
			}
		}

		// retrieved from PG_DIAG_MESSAGE_HINT result error field
		public string Hint
		{
			get { return mHint; }
		}

	}
}
