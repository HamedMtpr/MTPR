using System.Collections;

namespace Mtpr.Core;

public class PaginatedList<T> : IReadOnlyList<T>
{
    private readonly List<T> items;
    private int pageSize;

    public int CurrentPageNumber
    {
        get;
        set
        {
            if (PageCount == 0)
            {
                field = 1;
                return;
            }

            field = Math.Clamp(value, 1, PageCount);
        }
    }
    public int Count => items.Count;
    public int PageCount => (int)Math.Ceiling(Count / (double)pageSize);

    public T this[int index] => items[index];

    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int PageSize
    {
        get => pageSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "PageSize must be at least 1.");

            pageSize = value;
            CurrentPageNumber = 1;
        }
    }

    public bool HasPreviousPage => CurrentPageNumber > 1;
    public bool HasNextPage => CurrentPageNumber < PageCount;
    public IEnumerable<T> CurrentPage => 
        items
            .Skip((CurrentPageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
    public PaginatedList(IEnumerable<T> source, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        items = [.. source];
        PageSize = pageSize;
    }

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void MoveFirst() => CurrentPageNumber = 1;

    public void MovePrevious()
    {
        if (HasPreviousPage)
            CurrentPageNumber -= 1;
    }

    public void MoveNext()
    {
        if (HasNextPage)
            CurrentPageNumber += 1;
    }

    public void MoveLast() => CurrentPageNumber = PageCount;
}
