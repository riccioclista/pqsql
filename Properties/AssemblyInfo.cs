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
[assembly: AssemblyCopyright("Copyright © XIMES GmbH 2018")]
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
[assembly: AssemblyVersion("9.6.6.0")]
[assembly: AssemblyFileVersion("9.6.6.0")]

#if (DEBUG || TEST)

[assembly: InternalsVisibleTo("PqsqlTests")]

#endif