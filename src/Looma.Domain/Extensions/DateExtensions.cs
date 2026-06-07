// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Extensions;

public static class DateExtensions
{
    public static DateOnly? ToDateOnly(this DateTimeOffset? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value.DateTime);

    public static DateTimeOffset? ToDateTimeOffset(this DateOnly? value) =>
        value is null ? null : new DateTimeOffset(value.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}