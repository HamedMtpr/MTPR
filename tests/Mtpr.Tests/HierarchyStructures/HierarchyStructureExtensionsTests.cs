using Mtpr.Core;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.HierarchyStructures;

public partial class HierarchyStructureExtensionsTests : UnitTest
{
    private static (TestNode n1, TestNode n2, TestNode n3) createChain()
    {
        var n3 = new TestNode { Value = 3 };
        var n2 = new TestNode { Value = 2, Next = n3 };
        var n1 = new TestNode { Value = 1, Next = n2 };
        return (n1, n2, n3);
    }

    #region With predicate

    [Fact]
    public void GetTraversedHierarchyNodes_WithNextFunctionAndPredicate_Should_Return_Expected()
    {
        var (n1, n2, n3) = createChain();

        var actual = n1.GetTraversedHierarchyNodes(
            node => node?.Next, node => node is not null);

        actual.Should().Equal(n1, n2, n3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicate_Should_Stop_When_Predicate_Returns_False()
    {
        var (n1, n2, _) = createChain();

        var actual = n1.GetTraversedHierarchyNodes(
            node => node?.Next,
            node => node?.Value is int value && value < 3);

        actual.Should().Equal(n1, n2);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicateInitiallyFalse_Should_Return_Empty()
    {
        var node = new TestNode { Value = 1 };

        node.GetTraversedHierarchyNodes(_ => null, _ => false).Should().BeEmpty();
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicate_And_NullSource_Should_Pass_Null_To_Predicate()
    {
        TestNode? source = null;
        var seen = new List<TestNode?>();

        var actual = source.GetTraversedHierarchyNodes(
            node => node?.Next,
            node => { seen.Add(node); return false; });

        actual.Should().BeEmpty();
        seen.Should().ContainSingle().Which.Should().BeNull();
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullNextFunctionAndPredicate_Should_Throw_Eagerly()
    {
        var node = new TestNode { Value = 1 };

        var action = () => 
            node.GetTraversedHierarchyNodes(null!, _ => true);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nextItem");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullPredicate_Should_Throw_Eagerly()
    {
        var node = new TestNode { Value = 1 };

        var action = () => 
            node.GetTraversedHierarchyNodes(_ => null, null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("isContinue");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicate_Should_Be_Lazy()
    {
        var (n1, _, _) = createChain();
        var nextCalls = 0;
        var predicateCalls = 0;

        var result = n1.GetTraversedHierarchyNodes(
            node => { nextCalls++; return node?.Next; },
            node => { predicateCalls++; return node is not null; });

        nextCalls.Should().Be(0);
        predicateCalls.Should().Be(0);

        result.ToArray();

        nextCalls.Should().Be(3);
        predicateCalls.Should().Be(4);
    }

    #endregion

    #region Null-terminated

    [Fact]
    public void GetTraversedHierarchyNodes_WithNextFunction_Should_Return_Expected_In_Order()
    {
        var (n1, n2, n3) = createChain();

        var actual = n1.GetTraversedHierarchyNodes(node => node?.Next);

        actual.Should().Equal(n1, n2, n3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithSingleNode_Should_Return_Only_Source()
    {
        var node = new TestNode { Value = 1 };

        node.GetTraversedHierarchyNodes(_ => null)
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeSameAs(node);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullSource_Should_Return_Empty()
    {
        TestNode? source = null;

        source!.GetTraversedHierarchyNodes(n => n?.Next)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullNextFunction_Should_Throw_Eagerly()
    {
        var node = new TestNode { Value = 1 };

        var action = () => 
            node.GetTraversedHierarchyNodes((Func<TestNode?, TestNode?>)null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("nextItem");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNextFunction_Should_Be_Lazy()
    {
        var (n1, _, _) = createChain();
        var nextCalls = 0;

        var result = n1.GetTraversedHierarchyNodes(
            node => { nextCalls++; return node?.Next; });

        nextCalls.Should().Be(0);

        result.ToArray();

        nextCalls.Should().Be(3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_Should_Not_Evaluate_Beyond_What_Is_Consumed()
    {
        var (n1, n2, _) = createChain();
        var nextCalls = 0;

        var actual = n1.GetTraversedHierarchyNodes(
                node => { nextCalls++; return node?.Next; })
            .Take(2)
            .ToArray();

        actual.Should().Equal(n1, n2);
        nextCalls.Should().Be(1);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithCycle_Should_Be_Bounded_By_Consumer()
    {
        var node = new TestNode { Value = 1 };
        node.Next = node;

        node.GetTraversedHierarchyNodes(n => n?.Next)
            .Take(5)
            .Should()
            .HaveCount(5);
    }

    #endregion
}
