using System.Linq.Expressions;

namespace Mtpr.Core.Builders;

public static class PredicateBuilder
{
    private sealed class ParameterReplacer(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }

    public static Expression<Func<T, bool>> True<T>() => _ => true;

    public static Expression<Func<T, bool>> False<T>() => _ => false;

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return combine(expr1, expr2, Expression.OrElse);
    }

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return combine(expr1, expr2, Expression.AndAlso);
    }

    private static Expression<Func<T, bool>> combine<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        ArgumentNullException.ThrowIfNull(expr1);
        ArgumentNullException.ThrowIfNull(expr2);

        var parameter = expr1.Parameters[0];
        var rightBody = new ParameterReplacer(expr2.Parameters[0], parameter)
            .Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(
            merge(expr1.Body, rightBody),
            parameter);
    }
}
