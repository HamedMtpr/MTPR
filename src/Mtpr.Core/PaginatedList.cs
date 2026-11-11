namespace Mtpr.Core;

public class PaginatedList<T> : List<T>
{
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
    public int PageCount { get; private set; }

    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int PageSize
    {
        get => pageSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, "PageSize must be at least 1.");

            pageSize = value;
            PageCount = (int)Math.Ceiling(Count / (double)pageSize);
            CurrentPageNumber = 1;
        }
    }

    public bool HasPreviousPage => CurrentPageNumber > 1;
    public bool HasNextPage => CurrentPageNumber < PageCount;
    public IEnumerable<T> CurrentPage => 
        this.Skip((CurrentPageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
    public PaginatedList(IEnumerable<T> items, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        AddRange(items);
        PageSize = pageSize;
    }

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
