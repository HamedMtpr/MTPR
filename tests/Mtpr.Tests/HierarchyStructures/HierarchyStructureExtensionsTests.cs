using Mtpr.Core;
using Mtpr.TestUtilities;


namespace Mtpr.Tests.HierarchyStructures;

public partial class HierarchyStructureExtensionsTests : UnitTest
{
    [Fact]
    public void GetTraversedHierarchyNodes_WithNextFunctionAndPredicate_Should_Return_Expected()
    {
        // Arrange
        var node3 = new TestNode { Value = 3 };
        var node2 = new TestNode { Value = 2, Next = node3 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        var expected = new[] { node1, node2, node3 };

        // Act
        var actual = node1.GetTraversedHierarchyNodes(
            node => node?.Next,
            node => node is not null);

        // Assert
        actual.Should().ContainInOrder(expected);
        actual.Should().HaveCount(3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNextFunction_Should_Return_Expected()
    {
        // Arrange
        var node3 = new TestNode { Value = 3 };
        var node2 = new TestNode { Value = 2, Next = node3 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        var expected = new[] { node1, node2, node3 };

        // Act
        var actual = node1.GetTraversedHierarchyNodes(node => node?.Next);

        // Assert
        actual.Should().ContainInOrder(expected);
        actual.Should().HaveCount(3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_Should_Return_Expected_References()
    {
        // Arrange
        var node2 = new TestNode { Value = 2 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        // Act
        var actual = node1.GetTraversedHierarchyNodes(node => node?.Next)
            .ToArray();

        // Assert
        actual[0].Should().BeSameAs(node1);
        actual[1].Should().BeSameAs(node2);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicate_Should_Stop_When_Predicate_Returns_False()
    {
        // Arrange
        var node3 = new TestNode { Value = 3 };
        var node2 = new TestNode { Value = 2, Next = node3 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        // Act
        var actual = node1.GetTraversedHierarchyNodes(
            node => node?.Next,
            node => node?.Value is int value && value < 3);

        // Assert
        actual.Should().ContainInOrder(node1, node2);
        actual.Should().NotContain(node3);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithPredicateInitiallyFalse_Should_Return_Empty_Expected()
    {
        // Arrange
        var node = new TestNode { Value = 1 };

        // Act
        var actual = node.GetTraversedHierarchyNodes(
            _ => null,
            _ => false);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithSingleNode_Should_Return_Expected_SingleNode()
    {
        // Arrange
        var expectedSingleNode = new TestNode { Value = 1 };

        // Act
        var actual = expectedSingleNode.GetTraversedHierarchyNodes(_ => null);

        // Assert
        actual.Should().ContainSingle()
            .Which.Should().BeSameAs(expectedSingleNode);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullNext_Should_Stop_Traversal()
    {
        // Arrange
        var node2 = new TestNode { Value = 2 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        // Act
        var actual = node1.GetTraversedHierarchyNodes(node => node?.Next);

        // Assert
        actual.Should().ContainInOrder(node1, node2);
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullNextFunction_Should_Throw_Exception()
    {
        // Arrange
        var node = new TestNode { Value = 1 };

        // Act
        var action = () => node
            .GetTraversedHierarchyNodes((Func<TestNode?, TestNode?>)null!)
            .ToArray();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nextItem");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullNextFunctionAndPredicate_Should_Throw_Exception()
    {
        // Arrange
        var node = new TestNode { Value = 1 };

        // Act
        var action = () => node
            .GetTraversedHierarchyNodes(
                null!,
                _ => true)
            .ToArray();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("nextItem");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_WithNullPredicate_Should_Exception()
    {
        // Arrange
        var node = new TestNode { Value = 1 };

        // Act
        var action = () => node
            .GetTraversedHierarchyNodes(
                _ => null,
                null!)
            .ToArray();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("isContinue");
    }

    [Fact]
    public void GetTraversedHierarchyNodes_Should_Be_Lazy()
    {
        // Arrange
        var node2 = new TestNode { Value = 2 };
        var node1 = new TestNode { Value = 1, Next = node2 };

        var nextCallCount = 0;

        // Act
        var result = node1.GetTraversedHierarchyNodes(node =>
        {
            nextCallCount++;
            return node?.Next;
        });

        // Assert
        nextCallCount.Should().Be(0);

        result.ToArray();

        nextCallCount.Should().Be(2);
    }
}