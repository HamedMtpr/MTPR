using Mtpr.Core.Builders;

namespace Mtpr.Tests.BuilderTests;

public partial class BuilderTests
{
    private class TestClassSummaryBuilder : Builder<TestClass, Summary>
    {
        protected override Summary Build(TestClass source) =>
            new() { Text = $"{source.SomeProperty}:{source.AnotherProperty}" };
    }
}