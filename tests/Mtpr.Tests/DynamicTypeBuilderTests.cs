using System.Text.Json;
using Mtpr.Core.Builders;
using Mtpr.Core.Builders.DynamicTypeBuilders;
using Mtpr.TestUtilities;

namespace Mtpr.Tests;

public class DynamicTypeBuilderTests : UnitTest
{
    [Fact]
    public void Should_Inherit_From_Builder()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Person");

        //Assert
        sut.Should().BeAssignableTo<Builder<DynamicTypeDefinition, Type>>();
    }

    [Fact]
    public void Build_Should_Return_Emitted_Type()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Person").WithProperty<int>("Age");

        //Act
        var actual = sut.Build();

        //Assert
        actual.Name.Should().Be("Person");
        actual.GetProperties().Should().ContainSingle();
    }

    [Fact]
    public void Build_Should_Be_Callable_Through_Base_Builder()
    {
        //Arrange
        Builder<DynamicTypeDefinition, Type> sut = new DynamicTypeBuilder("Person").WithProperty<int>("Age");

        //Act
        var actual = sut.Build();

        //Assert
        actual.Should().NotBeNull();
    }

    [Fact]
    public void Build_Should_Create_Instantiable_Type_With_Properties()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Person")
            .WithProperty<string>("Name")
            .WithProperty<int>("Age");

        //Act
        var type = sut.Build();
        var obj = Activator.CreateInstance(type)!;
        type.GetProperty("Name")!.SetValue(obj, "Ali");
        type.GetProperty("Age")!.SetValue(obj, 30);

        //Assert
        type.Name.Should().Be("Person");
        type.GetProperty("Name")!.GetValue(obj).Should().Be("Ali");
        type.GetProperty("Age")!.GetValue(obj).Should().Be(30);
        type.GetProperty("Age")!.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void WithProperty_Should_Resolve_Type_Names()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Order")
            .WithProperty("Total", "decimal")
            .WithProperty("CreatedAt", "DateTime")
            .WithProperty("Qty", "int?");

        //Act
        var type = sut.Build();

        //Assert
        type.GetProperty("Total")!.PropertyType.Should().Be<decimal>();
        type.GetProperty("CreatedAt")!.PropertyType.Should().Be<DateTime>();
        type.GetProperty("Qty")!.PropertyType.Should().Be<int?>();
    }

    [Fact]
    public void WithProperties_Should_Add_All_Names()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Names")
            .WithProperties<string>(["A", "B", "C"]);

        //Act
        var type = sut.Build();

        //Assert
        type.GetProperties().Should().HaveCount(3);
    }

    [Fact]
    public void Build_Should_Return_Distinct_Types_On_Each_Call()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Same")
            .WithProperty<int>("X");

        //Act
        var first = sut.Build();
        var second = sut.Build();

        //Assert
        second.Should().NotBeSameAs(first);
        second.Assembly.Should().NotBeSameAs(first.Assembly);
        first.Assembly.GetName().Name
            .Should()
            .NotBe(typeof(DynamicTypeBuilder).Assembly.GetName().Name);
    }

    [Fact]
    public void WithProperty_After_Build_Should_Affect_Only_Next_Build()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Late")
            .WithProperty<int>("X");

        var first = sut.Build();

        //Act
        sut.WithProperty<int>("Y");
        var second = sut.Build();

        //Assert
        first.GetProperties().Should().HaveCount(1);
        second.GetProperties().Should().HaveCount(2);
    }

    [Fact]
    public void WithProperty_Should_Throw_On_Duplicate()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Dup")
            .WithProperty<int>("X");

        //Act
        var act = () => sut.WithProperty<int>("X");

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1abc")]
    [InlineData("has space")]
    [InlineData("a-b")]
    public void WithProperty_Should_Throw_On_Invalid_Name(string name)
    {
        //Arrange
        var sut = new DynamicTypeBuilder("Valid");

        //Act
        var act = () => sut.WithProperty<int>(name);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad name")]
    [InlineData("9Class")]
    public void Constructor_Should_Throw_On_Invalid_ClassName(string name)
    {
        //Act
        var act = () => new DynamicTypeBuilder(name);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithProperty_Should_Throw_On_Unknown_Type_Name()
    {
        //Arrange
        var sut = new DynamicTypeBuilder("T");

        //Act
        var act = () => sut.WithProperty("X", "foobar");

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Type_Should_Work_With_Json_Serialization()
    {
        //Arrange
        var type = new DynamicTypeBuilder("Dto")
            .WithProperty<string>("Name").Build();

        //Act
        var atual = JsonSerializer.Deserialize("{\"Name\":\"Zed\"}", type)!;

        //Assert
        type.GetProperty("Name")!.GetValue(atual)
            .Should().Be("Zed");
    }
}
