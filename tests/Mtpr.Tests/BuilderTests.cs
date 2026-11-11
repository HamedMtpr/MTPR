using FluentAssertions;
using Mtpr.Core;
using Mtpr.TestUtilities;

namespace Mtpr.Tests;

public class BuilderTests : UnitTest
{
    private class TestClass
    {
        public string? SomeProperty { get; set; }
        public int AnotherProperty { get; set; }
    }

    [Fact]
    public void Build_Should_Return_Expected()
    {
        //Arrange
        string someProperty = "Some Property";
        int anotherProperty = 1;

        var sut = new Builder<TestClass>()
            .With(a => a.SomeProperty = someProperty)
            .With(a => a.AnotherProperty = anotherProperty);

        var expected = new TestClass()
        {
            SomeProperty = someProperty,
            AnotherProperty = anotherProperty,
        };

        //Act
        var actual = sut.Build();

        //Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Build_Should_Return_Immutable_Expected()
    {
        //Arrange
        string someProperty = "Some Property";
        int anotherProperty = 1;

        var sut = new Builder<TestClass>()
            .With(a => a.SomeProperty = someProperty)
            .With(a => a.AnotherProperty = anotherProperty);

        var expected = sut.Build();

        //Act
        var actual = sut.Build();

        //Assert
        actual.Should().NotBeSameAs(expected);
    }
}