using System.ComponentModel;
using Mtpr.Core;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.EnumExtensions;

public partial class EnumExtensionsTests : UnitTest
{
    #region  GetAttributePropertyValue<TAttribute>

    [Theory]
    [InlineData(TestEnum.First, "First Description")]
    [InlineData(TestEnum.Third, "Third Description")]
    public void GetAttributePropertyValue_With_Attribute_Should_Return_Property_Value(TestEnum value, string expected)
    {
        // Arrange

        // Act
        var actual = value.GetAttributePropertyValue<DescriptionAttribute>(
            nameof(DescriptionAttribute.Description));

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetAttributePropertyValue_Without_Attribute_Should_Return_Null()
    {
        // Arrange

        // Act
        var actual = TestEnum.Second
            .GetAttributePropertyValue<DescriptionAttribute>(
                nameof(DescriptionAttribute.Description));

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetAttributePropertyValue_For_Undefined_Value_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => ((TestEnum)99).GetAttributePropertyValue<DescriptionAttribute>(
            nameof(DescriptionAttribute.Description));

        // Assert
        actual
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void GetAttributePropertyValue_With_Unknown_Property_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => TestEnum.First
            .GetAttributePropertyValue<DescriptionAttribute>("Missing");

        // Assert
        actual
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("attributePropertyName");
    }

    [Fact]
    public void GetAttributePropertyValue_With_Null_Value_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () =>
            ((Enum)null!).GetAttributePropertyValue<DescriptionAttribute>("Description");

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void GetAttributePropertyValue_With_Null_PropertyName_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () =>
            TestEnum.First.GetAttributePropertyValue<DescriptionAttribute>(null!);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("attributePropertyName");
    }

    #endregion

    #region GetAttributePropertyValue<TAttribute, TValue>

    [Fact]
    public void GetAttributePropertyValue_Typed_With_Attribute_Should_Return_Typed_Value()
    {
        // Arrange

        // Act
        var actual = TestEnum.Third
            .GetAttributePropertyValue<DescriptionAttribute, string>(
                nameof(DescriptionAttribute.Description));

        // Assert
        actual.Should().Be("Third Description");
    }

    [Fact]
    public void GetAttributePropertyValue_Typed_Without_Attribute_Should_Return_Default()
    {
        // Arrange

        // Act
        var actual = TestEnum.Second
            .GetAttributePropertyValue<DescriptionAttribute, string>(nameof(
                DescriptionAttribute.Description));

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetAttributePropertyValue_Typed_With_Mismatched_Type_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () =>
            TestEnum.First.GetAttributePropertyValue<DescriptionAttribute, int>(
                nameof(DescriptionAttribute.Description));

        // Assert
        actual.Should().Throw<InvalidCastException>();
    }

    #endregion

    #region GetValue<TEnum, TAttribute>

    [Theory]
    [InlineData("First Description", TestEnum.First)]
    [InlineData("Third Description", TestEnum.Third)]
    public void GetValue_Should_Return_Matching_Member(string value, TestEnum expected)
    {
        // Arrange

        // Act
        var actual = value.GetValue<TestEnum, DescriptionAttribute>(
            nameof(DescriptionAttribute.Description));

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValue_With_Member_Name_Without_Attribute_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => 
            "Second".GetValue<TestEnum, DescriptionAttribute>(
                nameof(DescriptionAttribute.Description));

        // Assert
        actual
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void GetValue_With_Non_Matching_Value_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () =>
            42.GetValue<TestEnum, DescriptionAttribute>(
                nameof(DescriptionAttribute.Description));

        // Assert
        actual
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void GetValue_With_Unknown_Property_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => 
            "First Description".GetValue<TestEnum, DescriptionAttribute>("Missing");

        // Assert
        actual
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName("attributePropertyName");
    }

    [Fact]
    public void GetValue_With_Null_Value_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => ((object)null!).GetValue<TestEnum, DescriptionAttribute>(nameof(DescriptionAttribute.Description));

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void GetValue_With_Null_PropertyName_Should_Throw()
    {
        // Arrange

        // Act
        var actual = () => "First Description".GetValue<TestEnum, DescriptionAttribute>(null!);

        // Assert
        actual
            .Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("attributePropertyName");
    }

    #endregion
}
