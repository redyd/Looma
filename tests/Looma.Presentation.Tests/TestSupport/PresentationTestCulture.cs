// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Globalization;
using System.Runtime.CompilerServices;
using Looma.Presentation.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Looma.Presentation.Tests.TestSupport;

internal static class PresentationTestCulture
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var culture = new CultureInfo("en-US");
        var uiCulture = new CultureInfo("fr");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        TranslationService.Current.SetCulture("fr");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }
}
