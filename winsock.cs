using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SocketError = System.Net.Sockets.SocketError; // reuse SocketError

namespace WinSock2
{

	// Enums from Microsofts website (#defined in C++)
	public enum AddressFamily : int
	{
		Unknown = 0,
		InterNetworkv4 = 2,
		Ipx = 4,
		AppleTalk = 17,
		NetBios = 17,
		InterNetworkv6 = 23,
		Irda = 26,
		BlueTooth = 32
	}

	public enum SocketType : int
	{
		Unknown = 0,
		Stream = 1,
		DGram = 2,
		Raw = 3,
		Rdm = 4,
		SeqPacket = 5
	}

	public enum ProtocolType : int
	{
		BlueTooth = 3,
		Tcp = 6,
		Udp = 17,
		ReliableMulticast = 113
	}

	// Equivilent to C++s "SOCKET"
	public unsafe struct SOCKET
	{
		private void* handle;
		private SOCKET(int _handle)
		{
			handle = (void*)_handle;
		}
		public static bool operator ==(SOCKET s, int i)
		{
			return ((int)s.handle == i);
		}
		public static bool operator !=(SOCKET s, int i)
		{
			return ((int)s.handle != i);
		}
		public static implicit operator SOCKET(int i)
		{
			return new SOCKET(i);
		}
		public static implicit operator uint(SOCKET s)
		{
			return (uint)s.handle;
		}
		public override bool Equals(object obj)
		{
			return (obj is SOCKET) ? (((SOCKET)obj).handle == this.handle) : base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return (int)handle;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct fd_set
	{
		public uint fd_count;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
		public IntPtr[] fd_array;

		private const int Size = 1;

		public static fd_set Null
		{
			get
			{
				return new fd_set()
				{
					fd_array = null,
					fd_count = 0
				};
			}
		}

		public static fd_set Create(IntPtr socket)
		{
			var handle = new fd_set()
			{
				fd_count = Size,
				fd_array = new IntPtr[Size] { socket }
			};
			return handle;
		}
	}

	// C# equivilent to C++'s sockaddr_in / SOCKADDR_IN
	[StructLayout(LayoutKind.Sequential, Size = 16)]
	public struct sockaddr_in
	{
		public const int Size = 16;

		public short sin_family;
		public ushort sin_port;
		public struct in_addr
		{
			public uint S_addr;
			public struct _S_un_b
			{
				public byte s_b1, s_b2, s_b3, s_b4;
			}
			public _S_un_b S_un_b;
			public struct _S_un_w
			{
				public ushort s_w1, s_w2;
			}
			public _S_un_w S_un_w;
		}
		public in_addr sin_addr;
	}

	// WSAData structure, used in WSAStarutp
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public unsafe struct WSAData
	{
		public ushort Version;
		public ushort HighVersion;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
		public string Description;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
		public string SystemStatus;
		public ushort MaxSockets;
		public ushort MaxUdpDg;
		sbyte* lpVendorInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct timeval
	{
		/// <summary>
		/// Time interval, in seconds.
		/// </summary>
		public int tv_sec;

		/// <summary>
		/// Time interval, in microseconds.
		/// </summary>
		public int tv_usec;
	};

	// Interface to ws2_32.dll
	public unsafe partial class winsock
	{
		public const int SOCKET_ERROR = -1;
		public const int INVALID_SOCKET = ~0;

		[DllImport("Ws2_32.dll")]
		public static extern int WSAStartup(ushort Version, out WSAData Data);
		[DllImport("Ws2_32.dll")]
		public static extern SocketError WSAGetLastError();
		[DllImport("Ws2_32.dll")]
		public static extern SOCKET socket(AddressFamily af, SocketType type, ProtocolType protocol);
		[DllImport("Ws2_32.dll")]
		public static extern int send(SOCKET s, byte* buf, int len, int flags);
		[DllImport("Ws2_32.dll")]
		public static extern int recv(SOCKET s, byte* buf, int len, int flags);
		[DllImport("Ws2_32.dll")]
		public static extern SOCKET accept(SOCKET s, void* addr, int addrsize);
		[DllImport("Ws2_32.dll")]
		public static extern int listen(SOCKET s, int backlog);
		[DllImport("Ws2_32.dll", CharSet = CharSet.Ansi)]
		public static extern uint inet_addr(string cp);
		[DllImport("Ws2_32.dll")]
		public static extern ushort htons(ushort hostshort);
		[DllImport("Ws2_32.dll")]
		public static extern int connect(SOCKET s, sockaddr_in* addr, int addrsize);
		[DllImport("Ws2_32.dll")]
		public static extern int closesocket(SOCKET s);
		[DllImport("Ws2_32.dll")]
		public static extern int getpeername(SOCKET s, sockaddr_in* addr, int* addrsize);
		[DllImport("Ws2_32.dll")]
		public static extern int bind(SOCKET s, sockaddr_in* addr, int addrsize);
		[DllImport("Ws2_32.dll")]
		public static extern int select(int ndfs, ref fd_set readfds, ref fd_set writefds, ref fd_set exceptfds, timeval* timeout);
		[DllImport("Ws2_32.dll")]
		public static extern sbyte* inet_ntoa(sockaddr_in.in_addr _in);
	}


}
