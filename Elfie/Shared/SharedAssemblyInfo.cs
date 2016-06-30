// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion(Microsoft.CodeAnalysis.Elfie.VersionConstants.AssemblyVersion)]
[assembly: AssemblyFileVersion(Microsoft.CodeAnalysis.Elfie.VersionConstants.FileVersion)]
[assembly: AssemblyInformationalVersion(Microsoft.CodeAnalysis.Elfie.VersionConstants.AssemblyVersion)]

[assembly: AssemblyProduct("Extensible Lightweight Fast Indexing of Entities (ELFIE)")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Microsoft 2016")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

#if DELAY_SIGNED
[assembly: InternalsVisibleTo("Elfie.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
#else
[assembly: InternalsVisibleTo("Elfie.Test")]
#endif
