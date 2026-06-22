// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.Navigation;

internal class NavigationStack<T>
{
    private readonly List<T> _back = [];
    private T? _current;

    public T? Current => _current;
    public bool CanGoBack => _back.Count > 0;

    public IReadOnlyList<T> Push(T page)
    {
        if (_current is not null)
            _back.Add(_current);
        _current = page;
        return TrimHistory();
    }

    public T? Pop(out T? removedCurrent)
    {
        removedCurrent = default;
        if (!CanGoBack) return default;
        removedCurrent = _current;
        var lastIndex = _back.Count - 1;
        _current = _back[lastIndex];
        _back.RemoveAt(lastIndex);
        return _current;
    }

    public IReadOnlyList<T> Clear()
    {
        var removed = new List<T>(_back);
        if (_current is not null)
            removed.Add(_current);

        _back.Clear();
        _current = default;
        return removed;
    }

    private IReadOnlyList<T> TrimHistory()
    {
        var removed = new List<T>();
        var keepLastTransient = true;
        for (var i = _back.Count - 1; i >= 0; i--)
        {
            if (ShouldKeepInHistory(_back[i]))
                continue;

            if (keepLastTransient)
            {
                keepLastTransient = false;
                continue;
            }

            removed.Add(_back[i]);
            _back.RemoveAt(i);
        }

        return removed;
    }

    private static bool ShouldKeepInHistory(T page) =>
        page is PageViewModelBase { KeepAliveInNavigationHistory: true };
}
