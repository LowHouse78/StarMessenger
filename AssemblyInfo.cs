using System.Reflection;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("1348da65-ac4b-4562-bd6f-38d31c20ada3")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("0.3.1.0")]
[assembly: AssemblyFileVersion("0.3.1.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("StarMessenger")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("A notification plugin for N.I.N.A. which provides the user with information about the current imaging session.")]

// Your name
[assembly: AssemblyCompany("Sascha Lohaus")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("StarMessenger")]
[assembly: AssemblyCopyright("Copyright ©  2024")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.3001")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/rennmaus-coder/ninaAPI")]


// The following attributes are optional for the official manifest meta data

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "messenger, information, pushover, ntfy, notification, offline")]

//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"This plugin provides the user with information from the N.I.N.A imaging session via Pushover, Ntfy or Email.
                            It is possible to configure which information the StarMessage contains. Furthermore, the user can use 'Message by Condition' to determine 
                            the condition on the basis of which the message is to be sent. 
                            If you have question or feedback, let me know in Discord: wizzardKvothe|Sascha or write an email to: wizzardkvothe78@gmail.com")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bitbucket.org/wizzardkvothe/starmessenger/raw/689922c72ca6f5c9763b495e1d93ee8f92511499/NINA.StarMessenger/starmessenger.png")]


// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]