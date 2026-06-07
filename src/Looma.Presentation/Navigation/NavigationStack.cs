// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.Navigation;

internal class NavigationStack<T>
{
    private readonly Stack<T> _back = new();
    private T? _current;

    public T? Current => _current;
    public bool CanGoBack => _back.Count > 0;

    public void Push(T page)
    {
        if (_current is not null)
            _back.Push(_current);
        _current = page;
    }

    public T? Pop()
    {
        if (!CanGoBack) return default;
        _current = _back.Pop();
        return _current;
    }

    public void Clear()
    {
        _back.Clear();
        _current = default;
    }
}