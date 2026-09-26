// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections;
using System.Globalization;
using System.Resources;
using Looma.Domain.Core;
using Looma.Domain.Localization;
using Looma.Presentation.Services;

namespace Looma.Presentation.Tests.Services;

public sealed class TranslationCoverageTests
{
    private static readonly ResourceManager Resources =
        new("Looma.Presentation.Resources.Translations", typeof(TranslationService).Assembly);

    public static TheoryData<string> Languages => new(TranslationService.SupportedLanguage);

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_language_defines_every_key_with_a_value(string language)
    {
        var neutral = ReadKeys(CultureInfo.InvariantCulture);
        var localized = ReadKeys(new CultureInfo(language));

        localized.Keys.Should().BeEquivalentTo(neutral.Keys, $"the {language} dictionary must match the neutral one");
        localized.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key)
            .Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Format_placeholders_match_the_neutral_dictionary(string language)
    {
        var neutral = ReadKeys(CultureInfo.InvariantCulture);
        var localized = ReadKeys(new CultureInfo(language));

        foreach (var (key, value) in neutral)
        {
            Placeholders(localized[key]).Should().BeEquivalentTo(Placeholders(value), $"{key} in {language}");
        }
    }

    [Fact]
    public void Domain_messages_are_translated_in_the_active_language()
    {
        var translation = TranslationService.Current;
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var (defaultCulture, defaultUiCulture) = (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);
        try
        {
            translation.SetCulture("fr");
            Localizer.Current.Should().BeSameAs(translation);
            Result.NotFound(Localizer.Format("Errors_WoolNotFound", 3)).Error.Should().Be("La laine 3 est introuvable.");

            translation.SetCulture("en");
            Localizer.Format("Errors_WoolNotFound", 3).Should().Be("Yarn 3 cannot be found.");

            translation.SetCulture("de");
            Localizer.Get("Errors_QuantityMustBePositive").Should().Be("Die Menge muss größer als null sein.");
        }
        finally
        {
            // SetCulture also changes number formatting: restore exactly what the other tests expect.
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = defaultUiCulture;
        }
    }

    private static Dictionary<string, string> ReadKeys(CultureInfo culture)
    {
        var set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return set.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? string.Empty);
    }

    private static string[] Placeholders(string value) =>
        System.Text.RegularExpressions.Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
}
