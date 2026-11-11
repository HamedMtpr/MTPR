using System.ComponentModel;

namespace Mtpr.Tests.TypeExtensions;

public partial class TypeExtensionsTests
{
    public class BaseClass
    {
        [Description("Base Description")] public virtual int Value { get; set; }
    }
}