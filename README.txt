Compiling Pqsql
===============

libpqbinfmt
-----------

Assumes that headers and lib files are installed in C:\pgsql

Just unpack postgresql-9.4.5-1-windows-x64-binaries.zip from
http://www.enterprisedb.com/postgresql-945-binaries-win64?ls=Crossover&type=Crossover
into C:\pgsql



Dependencies of Pqsql
=====================

libpqbinfmt
-----------

When libpqbinfmt is compiled with Visual Studio 2010, we need to install
Microsoft Visual C++ 2010 SP1 Redistributable Package (x64) from
https://www.microsoft.com/en-us/download/details.aspx?id=13523

The libpq DLLs depend on Visual C++ Redistributable Packages for Visual Studio 2013 from
https://www.microsoft.com/en-us/download/details.aspx?id=40784

libpqbinfmt us based on x64 libpq from PostgreSQL 9.4.5:
http://www.postgresql.org/docs/current/static/libpq.html

libpq DLLs can be retrieved from postgresql-9.4.5-1-windows-x64-binaries.zip:
http://www.enterprisedb.com/postgresql-945-binaries-win64?ls=Crossover&type=Crossover

libpq 9.4.5 is linked with
- OpenSSL 1.0.1p (libeay32.dll and ssleay32.dll)
- libintl 0.18.1 (libintl-8.dll)

postgresql-9.4.5-1-windows-x64-binaries.zip contains the necessary DLLs
for libpqbinfmt:
- pgsql\bin\libpq.dll
- pgsql\bin\libeay32.dll
- pgsql\bin\ssleay32.dll
- pgsql\bin\libintl-8.dll


pgBouncer
---------

https://pgbouncer.github.io/
pgbouncer-1.6.1-win-x64.zip: http://winpg.jp/~saito/pgbouncer/try_64bit/

Installation: 

- copy content of pgbouncer-1.6.1-win-x64.zip to C:\Program Files\pgBouncer
- setup pgbouncer.ini and userlist.txt in  C:\Program Files\pgBouncer
- register pgBouncer as Windows service:
  $ pgbouncer -regservice config.ini



TODO
====

- see README Section "Unsupported datatypes" of libpqbinfmt

- Textual I/O format not supported: some datatypes (aclitem, ...) do not support binary I/O, cf. also
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

- PqsqlConnection.GetSchema() and friends
- PqsqlConnection.EnlistTransaction()
- PqsqlConnection.StateChange