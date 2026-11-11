using System.ComponentModel;
using Mtpr.Core;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.TypeExtensions;

public partial class TypeExtensionsTests : UnitTest
{
    #region GetAttribute (generic, on Type)

    [Fact]
    public void GetAttribute_Should_Return_Attribute_Instance()
    {
        var actual = typeof(TestClass)
            .GetAttribute<DescriptionAttribute>(nameof(TestClass.SomeProperty));

        actual.Should().NotBeNull();
        actual!.Description.Should().Be("Some Description");
    }

    [Fact]
    public void GetAttribute_With_Custom_Attribute_Should_Return_Its_Value()
    {
        var actual = typeof(TestClass)
            .GetAttribute<MarkerAttribute>(nameof(TestClass.MultiAttributeProperty));

        ((object)actual!).Should().NotBeNull();
        ((string)actual!.Value).Should().BeOneOf("first", "second");
    }

    [Fact]
    public void GetAttribute_For_Missing_Attribute_Should_Return_Null()
    {
        typeof(TestClass)
            .GetAttribute<DescriptionAttribute>(nameof(TestClass.WithoutDescriptionProperty))
            .Should()
            .BeNull();
    }

    [Fact]
    public void GetAttribute_For_Missing_Property_Should_Return_Null()
    {
        typeof(TestClass)
            .GetAttribute<DescriptionAttribute>("InvalidProperty")
            .Should()
            .BeNull();
    }

    [Fact]
    public void GetAttribute_With_Null_Type_Should_Throw()
    {
        Type type = null!;

        Action action = () =>
            type.GetAttribute<DescriptionAttribute>("SomeProperty");

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("type");
    }

    [Fact]
    public void GetAttribute_With_Null_PropertyName_Should_Throw()
    {
        Action action = () => typeof(TestClass)
            .GetAttribute<DescriptionAttribute>(null!);

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("propertyName");
    }

    [Fact]
    public void GetAttribute_For_Overridden_Property_Should_Not_Inherit()
    {
        typeof(DerivedClass)
            .GetAttribute<DescriptionAttribute>(nameof(DerivedClass.Value))
            .Should()
            .BeNull();
    }

    [Fact]
    public void GetAttribute_For_Base_Property_Should_Return_Attribute()
    {
        typeof(BaseClass).GetAttribute<DescriptionAttribute>(nameof(BaseClass.Value))
            .Should().NotBeNull();
    }

    #endregion

    #region GetAttributes (all occurrences)

    [Fact]
    public void GetAttributes_Should_Return_All_Occurrences()
    {
        var actual = 
            typeof(TestClass)
                .GetAttributes<MarkerAttribute>(
                    nameof(TestClass.MultiAttributeProperty));

        actual.Select(a => a.Value)
            .Should()
            .BeEquivalentTo("first", "second");
    }

    [Fact]
    public void GetAttributes_Should_Be_Empty_When_Attribute_Missing()
    {
        typeof(TestClass)
            .GetAttributes<MarkerAttribute>(nameof(TestClass.SomeProperty))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void GetAttributes_Should_Be_Empty_When_Property_Missing()
    {
        typeof(TestClass)
            .GetAttributes<MarkerAttribute>("InvalidProperty")
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void GetAttributes_Should_Not_Be_Null()
    {
        typeof(TestClass).
            GetAttributes<MarkerAttribute>("InvalidProperty")
            .Should().
            NotBeNull();
    }

    [Fact]
    public void GetAttributes_With_Null_Type_Should_Throw()
    {
        Type type = null!;

        Action action = () => type
            .GetAttributes<DescriptionAttribute>("SomeProperty");

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("type");
    }

    [Fact]
    public void GetAttributes_With_Null_PropertyName_Should_Throw()
    {
        Action action = () => 
            typeof(TestClass).GetAttributes<DescriptionAttribute>(null!);

        action.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("propertyName");
    }

    #endregion
}
