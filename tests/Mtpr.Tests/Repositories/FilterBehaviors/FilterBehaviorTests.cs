using System.Linq.Expressions;
using System.Text.Json;
using Mtpr.Core.Repositories;
using Mtpr.Core.Repositories.FilterOperations;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.Repositories.FilterBehaviors;

public partial class FilterBehaviorTests : UnitTest
{
    [Fact]
    public void Equal_With_Double_On_Int_Should_Match()
    {
        run(nameof(Item.Number),
            new EqualFilterOperation(), 2.0)
            .Should()
            .ContainSingle()
            .Which.Number
            .Should()
            .Be(2);
    }

    [Fact]
    public void Equal_With_String_On_Guid_Should_Match()
    {
        run(nameof(Item.Id),
            new EqualFilterOperation(),
            firstId.ToString())
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void GreaterThan_With_String_On_DateTime_Should_Match()
    {
        run(nameof(Item.Date),
            new GreaterThanFilterOperation(), "2024-06-01T00:00:00Z").
            Should()
            .ContainSingle()
            .Which
            .Number
            .Should()
            .Be(2);
    }

    [Fact]
    public void Equal_With_Bool_Should_Match()
    {
        run(nameof(Item.Flag),
            new EqualFilterOperation(),
            true)
            .Should()
            .ContainSingle()
            .Which
            .Number
            .Should()
            .Be(1);
    }

    [Fact]
    public void Equal_With_Null_On_Nullable_Should_Match()
    {
        run(nameof(Item.Optional),
            new EqualFilterOperation(),
            null).
            Should()
            .ContainSingle()
            .Which
            .Number.
            Should()
            .Be(1);
    }

    [Fact]
    public void Equal_With_Double_On_Nullable_Int_Should_Match()
    {
        run(nameof(Item.Optional),
            new EqualFilterOperation(), 5.0).Should().ContainSingle().Which.Number.Should().Be(2);
    }

    [Fact]
    public void In_With_Doubles_On_Int_Should_Match()
    {
        run(nameof(Item.Number),
            new InFilterOperation(),
            new List<object?> { 1.0, 2.0 })
            .Should()
            .HaveCount(2);
    }

    [Fact]
    public void In_With_NonCollection_Should_Throw()
    {
        var actual = () => run(nameof(Item.Number),
            new InFilterOperation(),
            1);
        actual.Should()
            .Throw<InvalidFilterException>();
    }

    [Fact]
    public void Contains_On_NonString_Should_Throw()
    {
        var actual = () => run(nameof(Item.Number), 
            new ContainsFilterOperation(),
            "1");

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Contains_With_Null_Value_Should_Throw()
    {
        var actual = () => new ContainsFilterOperation()
            .GetPredicate(Expression.Constant("a"), null);

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void GreaterThan_On_Bool_Should_Throw()
    {
        var actual = () => run(nameof(Item.Flag),
            new GreaterThanFilterOperation(),
            true);

        actual.Should().Throw<InvalidFilterException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(1.5)]
    public void Invalid_Value_Should_Throw(object value)
    {
        var actual = () => run(nameof(Item.Number), 
            new EqualFilterOperation(), 
            value);

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Null_On_NonNullable_Should_Throw()
    {
        var actual = () => run(nameof(Item.Number),
            new EqualFilterOperation(), 
            null);

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Unknown_Property_Should_Throw()
    {
        var actual = () => run("Missing", 
            new EqualFilterOperation(),
            1);

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Property_Not_In_Whitelist_Should_Throw()
    {
        var actual = () => run(nameof(Item.Number),
            new EqualFilterOperation(), 1, 
            new HashSet<string>
            {
                nameof(Item.Flag)
            });
        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Missing_Operation_Or_Property_Should_Throw()
    {
        var source = items.AsQueryable();

        var noOp = () => source.ApplyFilters(
            [
                new FilterCriteria { PropertyName = "Number" }
            ])
            .ToList();
        var noProp = () => source.ApplyFilters(
            [
                new FilterCriteria
                {
                    FilterOperation = new EqualFilterOperation()
                }
            ]).ToList();

        noOp.Should().Throw<InvalidFilterException>();
        noProp.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void ToFilterCriteria_Without_Operation_Should_Throw()
    {
        var actual = () => new FilterCriteriaDto()
            .ToFilterCriteria(
                Substitute.For<IFilterOperationHandler>());

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Handler_Should_Resolve_By_Name_And_Throw_When_Unknown()
    {
        var sut = new FilterOperationHandler(
            [
                        new EqualFilterOperation(), 
                        new InFilterOperation()
                      ]);

        sut.Handle("equalfilteroperation")
            .Should()
            .BeOfType<EqualFilterOperation>();

        var actual = () => sut.Handle("Nope");

        actual.Should().Throw<InvalidFilterException>();
    }

    [Fact]
    public void Converter_Should_RoundTrip_All_Value_Kinds()
    {
        var options = new JsonSerializerOptions { Converters = { new CriteriaFilterConverter() } };
        const string json = """
                            [
                            {
                                "PropertyName":"A",
                                "FilterOperation":"X",
                                "Value":[1,"s",true,null]},
                            {
                                "PropertyName":"B",
                                "FilterOperation":"X",
                                "Value":false
                            },
                            {
                                "PropertyName":"C",
                                "FilterOperation":"X",
                                "Value":null,
                                "Extra":{"a":1}
                            }]
                            """;

        var dtos = JsonSerializer.Deserialize<List<FilterCriteriaDto>>(json, options)!;

        dtos[0].Value.Should().BeEquivalentTo(
            new List<object?> { 1.0, "s", true, null });

        dtos[1].Value.Should().Be(false);
        dtos[2].Value.Should().BeNull();

        var again = JsonSerializer.Deserialize<List<FilterCriteriaDto>>(
            JsonSerializer.Serialize(dtos, options), options)!;

        again[0].Value.Should().BeEquivalentTo(dtos[0].Value);
        again[1].Value.Should().Be(false);
    }

    [Fact]
    public void Converter_With_Truncated_Json_Should_Throw()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new CriteriaFilterConverter()
            }
        };

        var actual = () => 
            JsonSerializer.Deserialize<FilterCriteriaDto>("""{"PropertyName":"A" """, options);

        actual.Should().Throw<JsonException>();
    }

    [Fact]
    public void Converter_Should_Keep_Date_String_As_String()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new CriteriaFilterConverter()
            }
        };

        var actual = JsonSerializer.Deserialize<FilterCriteriaDto>(
            """{"Value":"2024-01-01T00:00:00Z"}""",
            options)!;

        actual.Value.Should().BeOfType<string>();
    }
}
