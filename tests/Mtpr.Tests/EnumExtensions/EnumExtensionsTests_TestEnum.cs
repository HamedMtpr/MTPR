using System.ComponentModel;

namespace Mtpr.Tests.EnumExtensions;

public enum TestEnum
{
    [Description("First Description")]
    First,

    Second,

    [Description("Third Description")]
    Third
}
