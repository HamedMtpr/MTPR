using System.Text.Json;
using Mtpr.Core.Repositories;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Repositories.FilterValueConverters;

public partial class FilterValueConverterTests : UnitTest
{
    [Theory]
    [MemberData(nameof(ValidConversions))]
    public void Convert_Should_Return_Value_Of_Target_Type(object value, Type type, object expected)
    {
        FilterValueConverter.Convert(value, type).Should().Be(expected);
    }

    [Fact]
    public void Convert_With_Null_On_Nullable_Should_Return_Null()
    {
        FilterValueConverter.Convert(null, typeof(int?)).Should().BeNull();
    }

    [Fact]
    public void Convert_With_Matching_Type_Should_Return_Same_Value()
    {
        FilterValueConverter.Convert("a", typeof(string)).Should().Be("a");
    }

    [Theory]
    [InlineData("abc", typeof(Color))]
    [InlineData("abc", typeof(DateOnly))]
    [InlineData(1.5, typeof(int))]
    public void Convert_With_Invalid_Value_Should_Throw(object value, Type type)
    {
        var act = () => FilterValueConverter.Convert(value, type);

        act.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Converter_Should_Skip_Unknown_Properties()
    {
        var json = """
                   {
                        "Unknown":
                        {
                            "a":[1,2]
                        },
                        "PropertyName":"Number",
                        "FilterOperation":"Equal",
                        "Value":1
                   }
                   """;

        var dto = JsonSerializer.Deserialize<FilterCriteriaDto>(json,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new CriteriaFilterConverter()
                }
            });

        dto!.PropertyName.Should().Be("Number");
    }

    [Fact]
    public void Converter_With_Object_Value_Should_Throw()
    {
        var json = """
                   {
                        "PropertyName":"Number",
                        "FilterOperation":"Equal",
                        "Value":
                        {
                            "a":1
                        }
                   }
                   """;

        var act = () => JsonSerializer.Deserialize<FilterCriteriaDto>(json,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new CriteriaFilterConverter()
                }
            });

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Converter_With_Incomplete_Block_Should_Throw()
    {
        var bytes = "{\"PropertyName\":\"Number\","u8.ToArray();
        var converter = new CriteriaFilterConverter();

        JsonException? error = null;
        try
        {
            var reader = new Utf8JsonReader(bytes, isFinalBlock: false, state: default);
            reader.Read();
            converter.Read(ref reader, typeof(FilterCriteriaDto), new JsonSerializerOptions());
        }
        catch (JsonException exception)
        {
            error = exception;
        }

        error.Should().NotBeNull();
    }
}
