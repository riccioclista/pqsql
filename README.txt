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

based on x64 libpq from PostgreSQL 9.4.5:
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