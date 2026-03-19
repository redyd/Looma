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