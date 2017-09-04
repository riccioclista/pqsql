Compiling Pqsql
===============

libpqbinfmt
-----------

Assumes that headers and lib files are installed in C:\pgsql

Just unpack postgresql-9.6.5-1-windows-x64-binaries.zip from
https://get.enterprisedb.com/postgresql/postgresql-9.6.5-1-windows-x64-binaries.zip
into C:\pgsql


Build
-----

Setup environment variables:
	"C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\amd64\vcvars64.bat"

Release:

	MSBuild.exe Pqsql-VS2015.sln /p:Configuration=Release /p:Platform=x64

Debug:

	MSBuild.exe Pqsql-VS2015.sln /p:Configuration=Debug /p:Platform=x64


Dependencies of Pqsql
=====================

libpqbinfmt
-----------

When libpqbinfmt is compiled with Visual Studio 2015, we need to install
Microsoft Visual C++ 2015 Redistributable Update 3 (x64) from
https://www.microsoft.com/en-us/download/details.aspx?id=53587

The libpq DLLs depend on Visual C++ Redistributable Packages for Visual Studio 2013 from
https://www.microsoft.com/en-us/download/details.aspx?id=40784

libpqbinfmt is based on x64 libpq from PostgreSQL 9.6.5:
http://www.postgresql.org/docs/current/static/libpq.html

libpq DLLs can be retrieved from postgresql-9.6.5-1-windows-x64-binaries.zip:
https://get.enterprisedb.com/postgresql/postgresql-9.6.5-1-windows-x64-binaries.zip

libpq 9.6.5 is linked with
- OpenSSL 1.0.2l (libeay32.dll and ssleay32.dll)
- libintl 0.19.6 (libintl-8.dll)
- libiconv 1.14.0.0 (libiconv-2.dll)

postgresql-9.6.5-1-windows-x64-binaries.zip contains the necessary DLLs
for libpqbinfmt:
- pgsql\bin\libpq.dll
- pgsql\bin\libeay32.dll
- pgsql\bin\ssleay32.dll
- pgsql\bin\libintl-8.dll
- pgsql\bin\libiconv-2.dll



Code Contracts for .NET (only for Debug configuration)
------------------------------------------------------

https://marketplace.visualstudio.com/items?itemName=RiSEResearchinSoftwareEngineering.CodeContractsforNET



pgBouncer (optional)
--------------------

https://pgbouncer.github.io/
pgbouncer-1.6.1-win-x64.zip: http://winpg.jp/~saito/pgbouncer/try_64bit/

Installation: 

- copy content of pgbouncer-1.6.1-win-x64.zip to C:\Program Files\pgBouncer
- setup pgbouncer.ini and userlist.txt in  C:\Program Files\pgBouncer
- register pgBouncer as Windows service:
  $ pgbouncer -regservice config.ini




Tests
=====

You can run the tests in the Debug configuration:

   "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\amd64\vcvars64.bat"

   MSBuild.exe Pqsql-VS2015.sln /p:Configuration=Debug /p:Platform=x64

   % setup libpq environment variables
   % E.g.
   set PGSERVICE=servicename
   set PGPASSWORD=secure

   vstest.console.exe bin\x64\Debug\PqsqlTests.dll /Settings:Pqsql.runsettings /logger:trx



TODO
====

Fix deprecated methods
----------------------
http://go.microsoft.com/fwlink/?linkid=14202
https://msdn.microsoft.com/en-us/library/hh419161.aspx#data


Connection pooling
------------------

- add connection pooling settings to PqsqlConnectionStringBuilder


Performance counters
--------------------
https://msdn.microsoft.com/en-us/library/ms254503(v=vs.110).aspx
https://www.devart.com/dotconnect/oracle/docs/PerformanceCounters.html


Implement CommandBehavior.SequentialAccess
------------------------------------------
https://msdn.microsoft.com/en-us/library/87z0hy49(v=vs.110).aspx


Data types
----------

- see README Section "Unsupported datatypes" of libpqbinfmt

- add support for user-defined data types in PqsqlParameter and PqsqlParameterCollection

- add support for further typcategory user-types in PqsqlDataReader (currently, only S (String types) is supported)

- add support for enum types https://www.postgresql.org/docs/current/static/datatype-enum.html

- add support for composite types https://www.postgresql.org/docs/current/static/rowtypes.html

- add support for range types https://www.postgresql.org/docs/current/static/rangetypes.html

- Textual I/O format not supported: some data types (aclitem, ...) do not support binary I/O, cf. also
  http://www.postgresql.org/message-id/flat/201102222230.41081.rsmogura@softperience.eu

	corresponding error is
	ERROR:  42883: no binary output function available for type aclitem

  select oid,typname,typarray,typinput,typoutput,typreceive,typsend from pg_type where typreceive=0 or typsend=0;
  ┌──────┬──────────────────┬──────────┬─────────────────────┬──────────────────────┬────────────┬─────────┐
  │ oid  │     typname      │ typarray │      typinput       │      typoutput       │ typreceive │ typsend │
  ├──────┼──────────────────┼──────────┼─────────────────────┼──────────────────────┼────────────┼─────────┤
  │  210 │ smgr             │        0 │ smgrin              │ smgrout              │ -          │ -       │
  │ 1033 │ aclitem          │     1034 │ aclitemin           │ aclitemout           │ -          │ -       │
  │ 3642 │ gtsvector        │     3644 │ gtsvectorin         │ gtsvectorout         │ -          │ -       │
  │ 2276 │ any              │        0 │ any_in              │ any_out              │ -          │ -       │
  │ 2279 │ trigger          │        0 │ trigger_in          │ trigger_out          │ -          │ -       │
  │ 3838 │ event_trigger    │        0 │ event_trigger_in    │ event_trigger_out    │ -          │ -       │
  │ 2280 │ language_handler │        0 │ language_handler_in │ language_handler_out │ -          │ -       │
  │ 2281 │ internal         │        0 │ internal_in         │ internal_out         │ -          │ -       │
  │ 2282 │ opaque           │        0 │ opaque_in           │ opaque_out           │ -          │ -       │
  │ 2283 │ anyelement       │        0 │ anyelement_in       │ anyelement_out       │ -          │ -       │
  │ 2776 │ anynonarray      │        0 │ anynonarray_in      │ anynonarray_out      │ -          │ -       │
  │ 3500 │ anyenum          │        0 │ anyenum_in          │ anyenum_out          │ -          │ -       │
  │ 3115 │ fdw_handler      │        0 │ fdw_handler_in      │ fdw_handler_out      │ -          │ -       │
  │ 3831 │ anyrange         │        0 │ anyrange_in         │ anyrange_out         │ -          │ -       │
  └──────┴──────────────────┴──────────┴─────────────────────┴──────────────────────┴────────────┴─────────┘

Asynchronous Notification
-------------------------
- add support for PqsqlConnection.Notify event https://www.postgresql.org/docs/current/static/libpq-notify.html
- add support for PqsqlConnection.Notice event https://www.postgresql.org/docs/current/static/libpq-notice-processing.html

GetSchema
---------
- PqsqlConnection.GetSchema() and friends

Two-phase commit
----------------
- PqsqlConnection.EnlistTransaction() (see https://www.postgresql.org/docs/current/static/sql-prepare-transaction.html)

PREPARE
-------
- PqsqlCommand.Prepare() (see PQsendPrepare, PQsendQueryPrepared, PQsendDescribePrepared)