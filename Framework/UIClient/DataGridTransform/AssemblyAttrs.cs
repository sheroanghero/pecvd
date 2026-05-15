//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// This file specifies various assembly level attributes.
//
//---------------------------------------------------------------------------

using System;
using System.Resources;
using System.Security;
using System.Windows;
using System.Windows.Markup;

// Needed to turn on checking of security critical call chains
//[assembly:SecurityCritical]

// Needed to enable xbap scenarios
[assembly:AllowPartiallyTrustedCallers]

[assembly:CLSCompliant(false)]
[assembly:NeutralResourcesLanguage("en-US")]

[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.Microsoft.Windows.Controls")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.Microsoft.Windows.Controls.Primitives")]

[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.Classes")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.Base")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.Converter")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.UserControls")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/wpf/2008/toolkit", "ExtendedGrid.ExtendedGridControl")]



