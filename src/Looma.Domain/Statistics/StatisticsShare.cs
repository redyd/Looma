// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Statistics;

/// <summary>Part d'un total, triée par valeur décroissante dans les rapports.</summary>
public sealed record StatisticsShare<TKey>(TKey Key, double Value);
