using Mtpr.TestUtilities;
using Mtpr.Core;

namespace Mtpr.Tests;

public class PaginatedListTests : UnitTest
{
    private static PaginatedList<int> createList(int count, int pageSize) =>
        new(Enumerable.Range(1, count), pageSize);

    #region Constructor and paging math

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void Constructor_Should_Compute_PageCount(int count, int pageSize, int expectedPageCount)
    {
        createList(count, pageSize)
            .PageCount
            .Should()
            .Be(expectedPageCount);
    }

    [Fact]
    public void Constructor_Should_Start_On_First_Page()
    {
        var sut = createList(25, 10);

        sut.CurrentPageNumber.Should().Be(1);
        sut.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_With_Empty_Items_Should_Have_Page_One_And_No_Pages()
    {
        var sut = createList(0, 10);

        sut.CurrentPageNumber.Should().Be(1);
        sut.PageCount.Should().Be(0);
        sut.CurrentPage.Should().BeEmpty();
        sut.HasNextPage.Should().BeFalse();
        sut.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void Constructor_With_Null_Items_Should_Throw()
    {
        Action action = () => new PaginatedList<int>(null!, 10);

        action.Should().
            Throw<ArgumentNullException>()
            .WithParameterName("items");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_With_Invalid_PageSize_Should_Throw(int pageSize)
    {
        Action action = () => createList(5, pageSize);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    #endregion

    #region CurrentPage

    [Fact]
    public void CurrentPage_Should_Return_Items_Of_Current_Page()
    {
        var sut = createList(25, 10);

        sut.CurrentPage.Should().Equal(Enumerable.Range(1, 10));

        sut.MoveLast();

        sut.CurrentPage
            .Should()
            .Equal(21, 22, 23, 24, 25);
    }

    [Fact]
    public void CurrentPage_Should_Be_A_Snapshot()
    {
        var sut = createList(5, 10);

        var page = sut.CurrentPage;
        sut.Add(6);

        page.Should().HaveCount(5);
    }

    #endregion

    #region Navigation

    [Fact]
    public void MoveNext_Should_Advance_And_Stop_At_Last_Page()
    {
        var sut = createList(25, 10);

        sut.MoveNext();
        sut.CurrentPageNumber.Should().Be(2);

        sut.MoveNext();
        sut.CurrentPageNumber.Should().Be(3);
        sut.HasNextPage.Should().BeFalse();

        sut.MoveNext();
        sut.CurrentPageNumber.Should().Be(3);
    }

    [Fact]
    public void MovePrevious_Should_Go_Back_And_Stop_At_First_Page()
    {
        var sut = createList(25, 10);
        sut.MoveLast();

        sut.MovePrevious();
        sut.CurrentPageNumber.Should().Be(2);
        sut.HasPreviousPage.Should().BeTrue();

        sut.MovePrevious();
        sut.CurrentPageNumber.Should().Be(1);
        sut.HasPreviousPage.Should().BeFalse();

        sut.MovePrevious();
        sut.CurrentPageNumber.Should().Be(1);
    }

    [Fact]
    public void MoveFirst_Should_Go_To_First_Page()
    {
        var sut = createList(25, 10);
        sut.MoveLast();

        sut.MoveFirst();

        sut.CurrentPageNumber.Should().Be(1);
    }

    [Fact]
    public void MoveLast_Should_Go_To_Last_Page()
    {
        var sut = createList(25, 10);

        sut.MoveLast();

        sut.CurrentPageNumber.Should().Be(3);
        sut.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void MoveLast_With_Empty_List_Should_Stay_On_Page_One()
    {
        var sut = createList(0, 10);

        sut.MoveLast();

        sut.CurrentPageNumber.Should().Be(1);
    }

    #endregion

    #region CurrentPageNumber setter

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(99, 3)]
    public void CurrentPageNumber_Set_Should_Clamp_To_Valid_Range(int value, int expected)
    {
        var sut = createList(25, 10);

        sut.CurrentPageNumber = value;

        sut.CurrentPageNumber.Should().Be(expected);
    }

    [Fact]
    public void CurrentPageNumber_Set_With_No_Pages_Should_Be_One()
    {
        var sut = createList(0, 10);

        sut.CurrentPageNumber = 5;

        sut.CurrentPageNumber.Should().Be(1);
    }

    #endregion

    #region PageSize

    [Fact]
    public void PageSize_Set_Should_Recalculate_And_Reset_To_First_Page()
    {
        var sut = createList(25, 10);
        sut.MoveLast();

        sut.PageSize = 5;

        sut.PageCount.Should().Be(5);
        sut.CurrentPageNumber.Should().Be(1);
        sut.CurrentPage.Should().Equal(1, 2, 3, 4, 5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void PageSize_Set_Invalid_Should_Throw_And_Keep_Previous_Value(int value)
    {
        var sut = createList(25, 10);

        Action action = () => sut.PageSize = value;

        action.Should().Throw<ArgumentOutOfRangeException>();
        sut.PageSize.Should().Be(10);
        sut.PageCount.Should().Be(3);
    }

    #endregion
}
