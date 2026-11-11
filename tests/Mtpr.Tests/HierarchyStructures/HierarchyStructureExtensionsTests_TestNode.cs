namespace Mtpr.Tests.HierarchyStructures;

public partial class HierarchyStructureExtensionsTests
{
    private class TestNode
    {
        public object? Value { get; set; }
        public TestNode? Next { get; set; }
    }
}