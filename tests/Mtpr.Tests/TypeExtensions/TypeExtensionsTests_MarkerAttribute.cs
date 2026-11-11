namespace Mtpr.Tests.TypeExtensions;

public partial class TypeExtensionsTests
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class MarkerAttribute(string value) : Attribute
    {
        public string Value { get; } = value;
    }
}