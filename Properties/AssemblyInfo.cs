using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Pqsql")]
[assembly: AssemblyDescription("PostgreSQL .NET data provider using libpq")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("XIMES GmbH")]
[assembly: AssemblyProduct("Pqsql")]
[assembly: AssemblyCopyright("Copyright © XIMES GmbH 2017")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f7e3eba2-1a87-406b-b636-9d8234409f0b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("9.6.5.1")]
[assembly: AssemblyFileVersion("9.6.5.1")]

#if (DEBUG || TEST)

[assembly: InternalsVisibleTo("PqsqlTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001005fb4801a1c2dbf1f00f7d0b0b4b43355b54f0c0947a7b4c40dd8bb9688d4dc694533988d3acb92ce19e54d1791c56a36e8b9b34410ff834e36843ad8ce7af9ad1d45a1e5c63a9c3ec8bbdca2b06d87b24ad124313dbbfb8fd84c2badedba8475960654153556b0090204d745a11fee8656fce36ee763741edb7234aec323339d")]

#endif