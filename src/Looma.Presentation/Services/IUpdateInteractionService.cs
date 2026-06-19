// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

namespace Looma.Presentation.Services;

public interface IUpdateInteractionService
{
    event EventHandler? UpdatePromptRequested;
    event EventHandler? CurrentReleaseNotesRequested;

    void RequestUpdatePrompt();
    void RequestCurrentReleaseNotes();
}
