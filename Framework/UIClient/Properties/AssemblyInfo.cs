using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Caliburn.Micro")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Caliburn.Micro")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("31a1fa85-31fe-4bf6-920f-e179be4b1d71")]

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
[assembly: AssemblyVersion("1.0.*")]
//[assembly: AssemblyFileVersion("1.0.0.0")]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.SourceAssembly, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsDefinition("http://OpenSEMI.Ctrlib.com/presentation", "OpenSEMI.Ctrlib.Controls")]
[assembly: XmlnsDefinition("http://OpenSEMI.Ctrlib.com/presentation", "OpenSEMI.Ctrlib.Types")]
[assembly: XmlnsDefinition("http://OpenSEMI.Ctrlib.com/presentation", "OpenSEMI.Ctrlib.Window")]

[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]

[assembly: XmlnsDefinition("http://www.caliburn.org", "Caliburn.Micro")]
[assembly: XmlnsPrefix("http://www.caliburn.org", "cal")]

#if !SILVERLIGHT
//[assembly: CLSCompliant(true)]
#endif