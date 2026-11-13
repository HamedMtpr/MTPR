using Mtpr.Core.Builders;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.BuilderTests;

public partial class BuilderTests : UnitTest
{
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

    [Fact]
    public void Build_Should_Return_Result_Produced_From_Source()
    {
        //Arrange
        var sut = new TestClassSummaryBuilder()
            .With(a => a.SomeProperty = "name")
            .With(a => a.AnotherProperty = 7);

        //Act
        var actual = sut.Build();

        //Assert
        actual.Should().BeOfType<Summary>();
        actual.Text.Should().Be("name:7");
    }

    [Fact]
    public void Build_Should_Apply_Actions_To_Fresh_Source_Each_Time()
    {
        //Arrange
        var sut = new TestClassSummaryBuilder()
            .With(a => a.AnotherProperty += 1);

        //Act
        var first = sut.Build();
        var second = sut.Build();

        //Assert
        first.Text.Should().Be(":1");
        second.Text.Should().Be(":1");
    }

    [Fact]
    public void With_Should_Throw_When_Action_Is_Null()
    {
        //Arrange
        var sut = new Builder<TestClass>();

        //Act
        var act = () => sut.With(null!);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_Without_Actions_Should_Return_Default_Source()
    {
        //Act
        var actual = new Builder<TestClass>()
            .Build();

        //Assert
        actual.Should().BeEquivalentTo(new TestClass());
    }
}
