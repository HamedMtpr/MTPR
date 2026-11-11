namespace Mtpr.Core;

public class Builder<T> where T : class, new()
{
    private readonly List<Action<T>> actions = [];

    public Builder<T> With(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        actions.Add(action);
        return this;
    }

    public T Build()
    {
        var objectToBuild = new T();

        foreach (var action in actions)
            action(objectToBuild);

        return objectToBuild;
    }
}
