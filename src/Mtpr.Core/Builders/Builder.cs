namespace Mtpr.Core.Builders;

/// <summary>
/// Builder whose result is the configured object itself.
/// </summary>
public class Builder<T> : Builder<T, T> where T : class, new()
{
    protected override T Build(T source) => source;
}

/// <summary>
/// Generic builder: collects actions that configure a <typeparamref name="TSource"/>
/// instance, then converts the configured instance into a <typeparamref name="TResult"/>.
/// </summary>
public abstract class Builder<TSource, TResult> where TSource : class, new()
{
    private readonly List<Action<TSource>> actions = [];

    public Builder<TSource, TResult> With(Action<TSource> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        actions.Add(action);
        return this;
    }

    public TResult Build()
    {
        var source = new TSource();

        foreach (var action in actions)
            action(source);

        return Build(source);
    }

    protected abstract TResult Build(TSource source);
}
