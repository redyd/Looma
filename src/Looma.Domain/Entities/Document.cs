// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Entities;

public class Document
{
    public required Guid Id { get; init; }
    public required string Nickname { get; init; }
    public required string Type { get; init; }
    public required long SizeBytes { get; init; }
    public required string? StoragePath { get; init; }
}
