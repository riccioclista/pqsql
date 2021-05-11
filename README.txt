!!! Visual Studio users: If the Project libpqbinfmt.proj cannot be loaded, you first need to build it with cmake.
The easiest way is by running the "debug reconfigure libpqbinfmt" task in VS Code.


Compiling Pqsql
===============


Build:
 - get vscode
    * C# extension
    * CMake extension (optional)
    * .NET Core Test Explorer (optional)
 - get .net core 2.1
 - get cmake
 - get deps: task "setup dependencies" (this may take a while)
 - configure libpqbinfmt: task "debug reconfigure libpqbinfmt"
 - build: task "debug build pqsql"

Run tests:
 - make sure to use at least vstest version 16.7.2, because we need environment variable support in .runsettings
 - build tests: task "build pqsql tests"
 - configure tests: task "configure test runsettings"
 - add current user to docker group (linux only)
 - start postgres docker test instance: task "start docker pgsql 9.6" (may take a little longer on first run)
 - run tests: task "run tests"






libpqbinfmt
-----------

Assumes that headers and lib files are installed using the "setup dependencies" task


Build
-----

 - configure: task "debug reconfigure libpqbinfmt"
 - build: task "debug build libpqbinfmt"


Dependencies of Pqsql
=====================

libpqbinfmt
-----------

When libpqbinfmt is compiled with Visual Studio 2015, we need to install
Microsoft Visual C++ 2015 Redistributable Update 3 (x64) from
https://www.microsoft.com/en-us/download/details.aspx?id=53587

The libpq DLLs depend on Visual C++ Redistributable Packages for Visual Studio 2013 from
https://www.microsoft.com/en-us/download/details.aspx?id=40784

libpqbinfmt is based on x64 libpq from PostgreSQL 10.16:
http://www.postgresql.org/docs/current/static/libpq.html

libpq DLLs can be retrieved from postgresql-10.16 by running the "setup dependencies" task

libpq 10.16 is linked with
- OpenSSL 1.1.1k (libssl-1_1-x64.dll and libcrypto-1_1-x64.dll)
- libintl 0.19.6 (libintl-8.dll)
- libiconv 1.14 (libiconv-2.dll)

postgresql 10.16 package contains the necessary DLLs
for libpqbinfmt:
- pgsql\bin\libpq.dll
- pgsql\bin\libssl-1_1-x64.dll
- pgsql\bin\libcrypto-1_1-x64.dll
- pgsql\bin\libintl-8.dll
- pgsql\bin\libiconv-2.dll



Tests
=====

You can run the tests in the Debug configuration using the task "run tests"   



TODO
====

Target Framework
----------------
current target framework netstandard2.0


Obsolete
--------
- mark PqsqlState.DEADLOCK_DETECTED as [Obsolete]


Error codes
-----------
check current list of error codes for PqsqlState:
https://www.postgresql.org/docs/current/static/errcodes-appendix.html


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