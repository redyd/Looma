// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Integrity;

namespace Looma.Domain.IServices;

public interface IDataIntegrityService
{
    /// <summary>Checks the database, configuration, themes and document files. Never modifies anything.</summary>
    Task<ResultT<IntegrityReport>> CheckAsync();

    /// <summary>Moves stored files that no document references into a quarantine folder (never deletes them).</summary>
    Task<ResultT<int>> QuarantineOrphanFilesAsync();

    /// <summary>Removes the database entries of documents whose file is missing.</summary>
    Task<ResultT<int>> RemoveMissingDocumentEntriesAsync();
}
