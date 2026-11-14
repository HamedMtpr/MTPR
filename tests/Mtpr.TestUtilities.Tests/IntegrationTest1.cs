namespace Mtpr.TestUtilities.Tests;

[CollectionDefinition(nameof(IntegrationTest1)), Order(3)]
[Collection(nameof(IntegrationTest1))]
public class IntegrationTest1 : IntegrationTest
{
    public static bool Collection1RunTestCase1;
    public static bool Collection1RunTestCase2;
    public static bool Collection1RunTestCase3;

    [Fact, Order(3)]
    public static void Test_Case3()
    {
        Assert.True(IntegrationTest2.Collection2RunTestCase1);
        Assert.True(IntegrationTest2.Collection2RunTestCase2);
        Assert.True(IntegrationTest2.Collection2RunTestCase3);

        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.True(Collection1RunTestCase1);
        Assert.True(Collection1RunTestCase2);
        Assert.False(Collection1RunTestCase3);

        Collection1RunTestCase3 = true;
    }

    [Fact, Order(2)]
    public static void Test_Case2()
    {
        Assert.True(IntegrationTest2.Collection2RunTestCase1);
        Assert.True(IntegrationTest2.Collection2RunTestCase2);
        Assert.True(IntegrationTest2.Collection2RunTestCase3);

        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.True(Collection1RunTestCase1);
        Assert.False(Collection1RunTestCase2);
        Assert.False(Collection1RunTestCase3);

        Collection1RunTestCase2 = true;
    }

    [Fact, Order(1)]
    public static void Test_Case1()
    {
        Assert.True(IntegrationTest2.Collection2RunTestCase1);
        Assert.True(IntegrationTest2.Collection2RunTestCase2);
        Assert.True(IntegrationTest2.Collection2RunTestCase3);

        Assert.True(IntegrationTest3.Collection3RunTestCase1);
        Assert.True(IntegrationTest3.Collection3RunTestCase2);
        Assert.True(IntegrationTest3.Collection3RunTestCase3);

        Assert.False(Collection1RunTestCase1);
        Assert.False(Collection1RunTestCase2);
        Assert.False(Collection1RunTestCase3);

        Collection1RunTestCase1 = true;
    }
}