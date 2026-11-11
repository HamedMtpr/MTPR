using Mtpr.Core;

namespace Mtpr.Tests.Enumerations;

public partial class EnumerationTests
{
    private sealed class SomeEnumeration : Enumeration
    {
        public SomeEnumeration(string? value, string displayName) : base(value, displayName) { }
        public SomeEnumeration(string displayName) : base(displayName) { }
    }
}