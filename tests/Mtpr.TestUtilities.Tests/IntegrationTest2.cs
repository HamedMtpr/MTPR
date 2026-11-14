namespace Mtpr.TestUtilities.Tests;

[CollectionDefinition(nameof(IntegrationTest2)), Order(2)]
[Collection(nameof(IntegrationTest2))]
public class IntegrationTest2 : IntegrationTest
{
    public static bool Collection2RunTestCase1;
    public static bool Collection2RunTestCase2;
    public static bool Collection2RunTestCase3;

    [Fact, Order(3)]
    public static void Test_Case3()
    {
        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.True(Collection2RunTestCase1);
        Assert.True(Collection2RunTestCase2);
        Assert.False(Collection2RunTestCase3);

        Collection2RunTestCase3 = true;
    }

    [Fact, Order(2)]
    public static void Test_Case2()
    {
        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.True(Collection2RunTestCase1);
        Assert.False(Collection2RunTestCase2);
        Assert.False(Collection2RunTestCase3);

        Collection2RunTestCase2 = true;
    }

    [Fact, Order(1)]
    public static void Test_Case1()
    {
        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.False(Collection2RunTestCase1);
        Assert.False(Collection2RunTestCase2);
        Assert.False(Collection2RunTestCase3);

        Collection2RunTestCase1 = true;
    }
}