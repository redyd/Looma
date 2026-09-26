// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Localization;

/// <summary>
/// Gives the lower layers access to the translation dictionary owned by the presentation layer.
/// </summary>
public interface ILocalizer
{
    string this[string key] { get; }

    string Format(string key, params object[] args);
}
