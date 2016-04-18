// AssemblyInfo.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

[assembly: AssemblyTitle("Com.Latipium.Defaults.Physics")]
[assembly: AssemblyDescription("The default physics implementation for Latipium")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Latipium")]
[assembly: AssemblyProduct("Com.Latipium.Defaults.Physics")]
[assembly: AssemblyCopyright("Zach Deibert")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.0.*")]

[assembly: AllowPartiallyTrustedCallers]
[assembly: CLSCompliant(true)]
