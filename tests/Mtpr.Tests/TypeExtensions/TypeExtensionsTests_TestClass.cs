using System.ComponentModel;

namespace Mtpr.Tests.TypeExtensions;

public partial class TypeExtensionsTests
{
    public class TestClass
    {
        [Description("Some Description")] public string SomeProperty { get; set; } = string.Empty;

        [Description("Another Description")] public int AnotherProperty { get; set; }

        public int WithoutDescriptionProperty { get; set; }

        [Description("")] public int EmptyDescriptionProperty { get; set; }

        [Marker("first")]
        [Marker("second")]
        [Description("With Markers")]
        public int MultiAttributeProperty { get; set; }
    }
}