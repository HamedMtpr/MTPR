namespace Mtpr.Core;

/// <summary>
/// Provides extension methods for traversing linked hierarchical structures.
/// </summary>
public static class HierarchyStructureExtensions
{
    /// <summary>
    /// Traverses a hierarchy starting from the specified source node.
    /// </summary>
    /// <typeparam name="TSource">The type of hierarchy node.</typeparam>
    /// <param name="source">The starting node.</param>
    /// <param name="nextItem">A function that returns the next node from the current node.</param>
    /// <param name="isContinue">
    /// A predicate that determines whether the current node should be included
    /// and traversal should continue.
    /// </param>
    /// <returns>A lazily evaluated sequence containing the traversed nodes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown immediately (not on enumeration) when <paramref name="nextItem"/>
    /// or <paramref name="isContinue"/> is null.
    /// </exception>
    /// <remarks>A cyclic structure yields an infinite sequence unless <paramref name="isContinue"/> stops it.</remarks>
    public static IEnumerable<TSource?> GetTraversedHierarchyNodes<TSource>(
        this TSource source,
        Func<TSource?, TSource?> nextItem,
        Predicate<TSource?> isContinue)
    {
        ArgumentNullException.ThrowIfNull(nextItem);
        ArgumentNullException.ThrowIfNull(isContinue);

        return Traverse(source, nextItem, isContinue);

        static IEnumerable<TSource?> Traverse(
            TSource? start, Func<TSource?, TSource?> next, Predicate<TSource?> @continue)
        {
            for (var current = start; @continue(current); current = next(current))
                yield return current;
        }
    }

    /// <summary>
    /// Traverses a null-terminated hierarchy starting from the specified source node.
    /// </summary>
    /// <typeparam name="TSource">The reference type of hierarchy node.</typeparam>
    /// <param name="source">The starting node.</param>
    /// <param name="nextItem">A function that returns the next node, or null when traversal should stop.</param>
    /// <returns>A lazily evaluated sequence containing the traversed nodes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown immediately (not on enumeration) when <paramref name="nextItem"/> is null.
    /// </exception>
    /// <remarks>A cyclic structure yields an infinite sequence.</remarks>
    public static IEnumerable<TSource> GetTraversedHierarchyNodes<TSource>(
        this TSource source,
        Func<TSource?, TSource?> nextItem)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(nextItem);

        return Traverse(source, nextItem);

        static IEnumerable<TSource> Traverse(TSource? start, Func<TSource?, TSource?> next)
        {
            for (var current = start; current is not null; current = next(current))
                yield return current;
        }
    }
}
