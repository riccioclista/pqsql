using System;
using System.ComponentModel;
using System.Data.Common;

namespace Pqsql
{

	// The currently recognized parameter key words are:
	//
	//host
	//    Name of host to connect to. If this begins with a slash, it specifies Unix-domain communication rather than TCP/IP communication; the value is the name of the directory in which the socket file is stored. The default behavior when host is not specified is to connect to a Unix-domain socket in /tmp (or whatever socket directory was specified when PostgreSQL was built). On machines without Unix-domain sockets, the default is to connect to localhost.
	//
	//hostaddr
	//    Numeric IP address of host to connect to. This should be in the standard IPv4 address format, e.g., 172.28.40.9. If your machine supports IPv6, you can also use those addresses. TCP/IP communication is always used when a nonempty string is specified for this parameter.
	//    Using hostaddr instead of host allows the application to avoid a host name look-up, which might be important in applications with time constraints. However, a host name is required for GSSAPI or SSPI authentication methods, as well as for verify-full SSL certificate verification. The following rules are used:
	//        If host is specified without hostaddr, a host name lookup occurs.
	//        If hostaddr is specified without host, the value for hostaddr gives the server network address. The connection attempt will fail if the authentication method requires a host name.
	//        If both host and hostaddr are specified, the value for hostaddr gives the server network address. The value for host is ignored unless the authentication method requires it, in which case it will be used as the host name.
	//    Note that authentication is likely to fail if host is not the name of the server at network address hostaddr. Also, note that host rather than hostaddr is used to identify the connection in ~/.pgpass (see Section 31.15).
	//    Without either a host name or host address, libpq will connect using a local Unix-domain socket; or on machines without Unix-domain sockets, it will attempt to connect to localhost.
	//
	//port
	//    Port number to connect to at the server host, or socket file name extension for Unix-domain connections.
	//
	//dbname
	//    The database name. Defaults to be the same as the user name. In certain contexts, the value is checked for extended formats; see Section 31.1.1 for more details on those.
	//
	//user
	//    PostgreSQL user name to connect as. Defaults to be the same as the operating system name of the user running the application.
	//
	//password
	//    Password to be used if the server demands password authentication.
	//
	//connect_timeout
	//    Maximum wait for connection, in seconds (write as a decimal integer string). Zero or not specified means wait indefinitely. It is not recommended to use a timeout of less than 2 seconds.
	//
	//client_encoding
	//    This sets the client_encoding configuration parameter for this connection. In addition to the values accepted by the corresponding server option, you can use auto to determine the right encoding from the current locale in the client (LC_CTYPE environment variable on Unix systems).
	//
	//options
	//    Adds command-line options to send to the server at run-time. For example, setting this to -c geqo=off sets the session's value of the geqo parameter to off. For a detailed discussion of the available options, consult Chapter 18.
	//
	//application_name
	//    Specifies a value for the application_name configuration parameter.
	//
	//fallback_application_name
	//    Specifies a fallback value for the application_name configuration parameter. This value will be used if no value has been given for application_name via a connection parameter or the PGAPPNAME environment variable. Specifying a fallback name is useful in generic utility programs that wish to set a default application name but allow it to be overridden by the user.
	//
	//keepalives
	//    Controls whether client-side TCP keepalives are used. The default value is 1, meaning on, but you can change this to 0, meaning off, if keepalives are not wanted. This parameter is ignored for connections made via a Unix-domain socket.
	//
	//keepalives_idle
	//    Controls the number of seconds of inactivity after which TCP should send a keepalive message to the server. A value of zero uses the system default. This parameter is ignored for connections made via a Unix-domain socket, or if keepalives are disabled. It is only supported on systems where the TCP_KEEPIDLE or TCP_KEEPALIVE socket option is available, and on Windows; on other systems, it has no effect.
	//
	//keepalives_interval
	//    Controls the number of seconds after which a TCP keepalive message that is not acknowledged by the server should be retransmitted. A value of zero uses the system default. This parameter is ignored for connections made via a Unix-domain socket, or if keepalives are disabled. It is only supported on systems where the TCP_KEEPINTVL socket option is available, and on Windows; on other systems, it has no effect.
	//
	//keepalives_count
	//    Controls the number of TCP keepalives that can be lost before the client's connection to the server is considered dead. A value of zero uses the system default. This parameter is ignored for connections made via a Unix-domain socket, or if keepalives are disabled. It is only supported on systems where the TCP_KEEPCNT socket option is available; on other systems, it has no effect.
	//
	//tty
	//    Ignored (formerly, this specified where to send server debug output).
	//
	//sslmode
	//    This option determines whether or with what priority a secure SSL TCP/IP connection will be negotiated with the server. There are six modes:
	//    disable
	//        only try a non-SSL connection
	//    allow
	//        first try a non-SSL connection; if that fails, try an SSL connection
	//    prefer (default)
	//        first try an SSL connection; if that fails, try a non-SSL connection
	//    require
	//        only try an SSL connection. If a root CA file is present, verify the certificate in the same way as if verify-ca was specified
	//    verify-ca
	//        only try an SSL connection, and verify that the server certificate is issued by a trusted certificate authority (CA)
	//    verify-full
	//        only try an SSL connection, verify that the server certificate is issued by a trusted CA and that the server host name matches that in the certificate
	//    See Section 31.18 for a detailed description of how these options work.
	//    sslmode is ignored for Unix domain socket communication. If PostgreSQL is compiled without SSL support, using options require, verify-ca, or verify-full will cause an error, while options allow and prefer will be accepted but libpq will not actually attempt an SSL connection.
	//
	//requiressl
	//    This option is deprecated in favor of the sslmode setting.
	//    If set to 1, an SSL connection to the server is required (this is equivalent to sslmode require). libpq will then refuse to connect if the server does not accept an SSL connection. If set to 0 (default), libpq will negotiate the connection type with the server (equivalent to sslmode prefer). This option is only available if PostgreSQL is compiled with SSL support.
	//
	//sslcompression
	//    If set to 1 (default), data sent over SSL connections will be compressed (this requires OpenSSL version 0.9.8 or later). If set to 0, compression will be disabled (this requires OpenSSL 1.0.0 or later). This parameter is ignored if a connection without SSL is made, or if the version of OpenSSL used does not support it.
	//    Compression uses CPU time, but can improve throughput if the network is the bottleneck. Disabling compression can improve response time and throughput if CPU performance is the limiting factor.
	//
	//sslcert
	//    This parameter specifies the file name of the client SSL certificate, replacing the default ~/.postgresql/postgresql.crt. This parameter is ignored if an SSL connection is not made.
	//
	//sslkey
	//    This parameter specifies the location for the secret key used for the client certificate. It can either specify a file name that will be used instead of the default ~/.postgresql/postgresql.key, or it can specify a key obtained from an external "engine" (engines are OpenSSL loadable modules). An external engine specification should consist of a colon-separated engine name and an engine-specific key identifier. This parameter is ignored if an SSL connection is not made.
	//
	//sslrootcert
	//    This parameter specifies the name of a file containing SSL certificate authority (CA) certificate(s). If the file exists, the server's certificate will be verified to be signed by one of these authorities. The default is ~/.postgresql/root.crt.
	//
	//sslcrl
	//    This parameter specifies the file name of the SSL certificate revocation list (CRL). Certificates listed in this file, if it exists, will be rejected while attempting to authenticate the server's certificate. The default is ~/.postgresql/root.crl.
	//
	//requirepeer
	//    This parameter specifies the operating-system user name of the server, for example requirepeer=postgres. When making a Unix-domain socket connection, if this parameter is set, the client checks at the beginning of the connection that the server process is running under the specified user name; if it is not, the connection is aborted with an error. This parameter can be used to provide server authentication similar to that available with SSL certificates on TCP/IP connections. (Note that if the Unix-domain socket is in /tmp or another publicly writable location, any user could start a server listening there. Use this parameter to ensure that you are connected to a server run by a trusted user.) This option is only supported on platforms for which the peer authentication method is implemented; see Section 19.3.6.
	//
	//krbsrvname
	//    Kerberos service name to use when authenticating with GSSAPI. This must match the service name specified in the server configuration for Kerberos authentication to succeed. (See also Section 19.3.3.)
	//
	//gsslib
	//    GSS library to use for GSSAPI authentication. Only used on Windows. Set to gssapi to force libpq to use the GSSAPI library for authentication instead of the default SSPI.
	//
	//service
	//    Service name to use for additional parameters. It specifies a service name in pg_service.conf that holds additional connection parameters. This allows applications to specify only a service name so connection parameters can be centrally maintained. See Section 31.16.
	//
	public sealed class PqsqlConnectionStringBuilder : DbConnectionStringBuilder
	{
		public const string host = "host";
		public const string hostaddr = "hostaddr";
		public const string port = "port";
		public const string user = "user";
		public const string password = "password";
		public const string dbname = "dbname";
		public const string connect_timeout = "connect_timeout";
		public const string client_encoding = "client_encoding";
		public const string options = "options";
		public const string application_name = "application_name";

		// .NET connection string aliases will be replaced with their libpq equivalents
		static readonly string[] hostAlias = { "server", "data source", "datasource", "address", "addr", "network address" };
		static readonly string[] dbnameAlias = { "database", "initial catalog" };
		static readonly string[] connect_timeoutAlias = { "connect timeout", "timeout" };
		static readonly string[] userAlias = { "user id", "uid", "username", "user name" };
		static readonly string[] passwordAlias = { "pwd" };

		public PqsqlConnectionStringBuilder()
		{
		}


		delegate void RemoveAliasAddKey(string alias, string key);

		//
		// Summary:
		//     Gets or sets the connection string associated with the System.Data.Common.DbConnectionStringBuilder.
		//
		// Returns:
		//     The current connection string, created from the key/value pairs that are
		//     contained within the System.Data.Common.DbConnectionStringBuilder. The default
		//     value is an empty string.
		//
		// Exceptions:
		//   System.ArgumentException:
		//     An invalid connection string argument has been supplied.
		[RefreshProperties(RefreshProperties.All)]
		public new string ConnectionString
		{
			get { return base.ConnectionString; }

			set
			{
				if (string.IsNullOrEmpty(value) || base.ConnectionString.Equals(value))
					return;

				base.ConnectionString = value;

				//
				// now clean up connection string and use libpq keywords
				//

				// host aliases
				RemoveAliasAddKey remAddHostPort = delegate(string a, string k)
				{
					object o;
					if (TryGetValue(a, out o))
					{
						Remove(a);

						string dataSource = (string) o;
						int i = dataSource.IndexOf(','); // Data Source=IP,PORT

						if (i == -1)
						{
							Add(k, o);
						}
						else
						{
							Add(host, dataSource.Substring(0, i));
							Add(port, dataSource.Substring(i + 1));
						}
					}
				};
				Array.ForEach(hostAlias, a => remAddHostPort(a, host));

				// dbname, user, password, connect_timeout
				RemoveAliasAddKey remAdd = delegate(string a, string k)
				{
					object o;
					if (TryGetValue(a, out o))
					{
						Remove(a);
						Add(k, o);
					}
				};
				Array.ForEach(dbnameAlias, a => remAdd(a, dbname));
				Array.ForEach(userAlias, a => remAdd(a, user));
				Array.ForEach(passwordAlias, a => remAdd(a, password));
				Array.ForEach(connect_timeoutAlias, a => remAdd(a, connect_timeout));

				//
				// always set default timeout of at least 2 seconds
				//

				object timeout;
				if (TryGetValue(connect_timeout, out timeout))
				{
					int it = Convert.ToInt32(timeout);

					if (it >= 2)
						return;

					Remove(connect_timeout);
				}

				Add(connect_timeout, "2");
			}
		}


		// E.g.
		// host=localhost; port=5432; user=postgres; password=P4$$word; dbname=postgres; connect_timeout=10
		public PqsqlConnectionStringBuilder(string s)
		{
			ConnectionString = s;
		}

		public bool Equals(PqsqlConnectionStringBuilder o)
		{
			if (ReferenceEquals(null, o))
				return false;
			if (ReferenceEquals(this, o))
				return true;
			return o.ConnectionString == ConnectionString;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(PqsqlConnectionStringBuilder))
				return false;
			return Equals((PqsqlConnectionStringBuilder) o);
		}

		public override int GetHashCode()
		{
			return base.ConnectionString.GetHashCode();
		}
	}
}
