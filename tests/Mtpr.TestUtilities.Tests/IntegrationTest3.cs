namespace Mtpr.TestUtilities.Tests;

[CollectionDefinition(nameof(IntegrationTest3)), Order(1)]
[Collection(nameof(IntegrationTest3))]
public class IntegrationTest3 : IntegrationTest
{
    public static bool Collection3RunTestCase1;
    public static bool Collection3RunTestCase2;
    public static bool Collection3RunTestCase3;

    [Fact, Order(3)]
    public static void Test_Case3()
    {
        Assert.False(IntegrationTest2.Collection2RunTestCase1);
        Assert.False(IntegrationTest2.Collection2RunTestCase2);
        Assert.False(IntegrationTest2.Collection2RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.True(Collection3RunTestCase1);
        Assert.True(Collection3RunTestCase2);
        Assert.False(Collection3RunTestCase3);

        Collection3RunTestCase3 = true;
    }

    [Fact, Order(2)]
    public static void Test_Case2()
    {
        Assert.False(IntegrationTest2.Collection2RunTestCase1);
        Assert.False(IntegrationTest2.Collection2RunTestCase2);
        Assert.False(IntegrationTest2.Collection2RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.True(Collection3RunTestCase1);
        Assert.False(Collection3RunTestCase2);
        Assert.False(Collection3RunTestCase3);

        Collection3RunTestCase2 = true;
    }

    [Fact, Order(1)]
    public static void Test_Case1()
    {
        Assert.False(IntegrationTest2.Collection2RunTestCase1);
        Assert.False(IntegrationTest2.Collection2RunTestCase2);
        Assert.False(IntegrationTest2.Collection2RunTestCase3);

        Assert.False(IntegrationTest1.Collection1RunTestCase1);
        Assert.False(IntegrationTest1.Collection1RunTestCase2);
        Assert.False(IntegrationTest1.Collection1RunTestCase3);

        Assert.False(Collection3RunTestCase1);
        Assert.False(Collection3RunTestCase2);
        Assert.False(Collection3RunTestCase3);

        Collection3RunTestCase1 = true;
    }
}