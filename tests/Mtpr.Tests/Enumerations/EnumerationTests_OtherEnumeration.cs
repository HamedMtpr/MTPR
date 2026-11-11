using Mtpr.Core;

namespace Mtpr.Tests.Enumerations;

public partial class EnumerationTests
{
    private sealed class OtherEnumeration : Enumeration
    {
        public OtherEnumeration(string? value, string displayName) : base(value, displayName) { }
    }
}