namespace Mtpr.Tests.Repositories.FilterValueConverters;

public partial class FilterValueConverterTests
{
    private enum Color
    {
        Red = 1,
        Blue = 2
    }

    public static TheoryData<object, Type, object> ValidConversions => new()
    {
        { "Blue", typeof(Color), Color.Blue },
        { 1.0, typeof(Color), Color.Red },
        {
            "2024-01-02T03:04:05+00:00", typeof(DateTimeOffset),
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)
        },
        { "2024-01-02", typeof(DateOnly), new DateOnly(2024, 1, 2) },
        { "03:04:05", typeof(TimeOnly), new TimeOnly(3, 4, 5) },
        { "01:02:03", typeof(TimeSpan), new TimeSpan(1, 2, 3) },
        { 2.0, typeof(int), 2 },
        { "5", typeof(long?), 5L },
    };
}