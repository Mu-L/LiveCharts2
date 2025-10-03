namespace Test.WPF;

[TestClass]
public class TestsInitializer
{
    [AssemblyInitialize()]
    public static void MyTestInitialize(TestContext testContext) =>
        TestApp.StartApp();

    [AssemblyCleanup]
    public static void TearDown() =>
        TestApp.EndApp();
}
