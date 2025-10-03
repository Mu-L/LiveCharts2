using System.Windows;
using System.Windows.Threading;

namespace Test.WPF;

public static class TestApp
{
    private static Application? s_application;
    private static Window? s_mainWindow;
    private static Dispatcher? s_staDispatcher;
    private static readonly ManualResetEvent s_dispatcherReady = new(false);
    private static Thread? s_staThread;
    private static volatile bool s_initialized = false;
    private static readonly object s_initLock = new();

    public static void StartApp()
    {
        if (s_initialized)
            return;

        lock (s_initLock)
        {
            if (s_initialized)
                return;

            Console.WriteLine("[TestApp] Starting STA thread...");

            s_staThread = new Thread(() =>
            {
                try
                {
                    Console.WriteLine($"[TestApp] STA thread started. Apartment state: {Thread.CurrentThread.GetApartmentState()}");

                    s_staDispatcher = Dispatcher.CurrentDispatcher;
                    Console.WriteLine("[TestApp] Dispatcher created");

                    // Create WPF objects on the STA thread
                    s_application = new Application();
                    s_mainWindow = new Window
                    {
                        Title = "Test Window",
                        Width = 800,
                        Height = 600,
                        Topmost = true, // Ensure the window is on top
                        WindowState = WindowState.Normal // Start minimized
                    };

                    Console.WriteLine("[TestApp] WPF objects created on STA thread");

                    _ = s_dispatcherReady.Set();

                    s_mainWindow.Show();
                    Console.WriteLine("[TestApp] Main window shown");

                    Console.WriteLine("[TestApp] Starting dispatcher message loop");
                    Dispatcher.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestApp] STA thread error: {ex}");
                    _ = s_dispatcherReady.Set(); // Signal even on error
                }
            });

            s_staThread.SetApartmentState(ApartmentState.STA);
            s_staThread.IsBackground = false; // Keep the process alive
            s_staThread.Start();

            // Wait for the STA thread to be ready with timeout
            if (!s_dispatcherReady.WaitOne(10000)) // Increased timeout
            {
                throw new TimeoutException("STA thread failed to initialize within 10 seconds");
            }

            s_initialized = true;
            Console.WriteLine("[TestApp] STA thread ready!");
        }
    }

    public static void EndApp()
    {
        Console.WriteLine("[TestApp] Ending app...");
        s_staDispatcher?.Invoke(() =>
        {
            s_mainWindow?.Close();
            s_application?.Shutdown();
            Dispatcher.ExitAllFrames();
        });
    }

    public static void Run(
        Func<FrameworkElement> elementBuilder, Action<UITestContext> test, int timeOutInSecs = 10)
    {
        if (s_staDispatcher == null || s_mainWindow == null)
            throw new InvalidOperationException("STA dispatcher not initialized. StartApp() may have failed.");

        var ctx = new UITestContext();
        Exception? dispatcherException = null;

        Console.WriteLine("[TestApp] Running test on STA thread...");

        // Use the STA dispatcher
        s_staDispatcher.Invoke(() =>
        {
            try
            {
                Console.WriteLine("[TestApp] Creating test element...");
                var sut = elementBuilder();

                sut.Loaded += (sender, _) =>
                {
                    Console.WriteLine("[TestApp] Element loaded, running test...");
                    try
                    {
                        ctx.SUT = sender;
                        test(ctx);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TestApp] Test execution failed: {ex}");
                        ctx.Throw(ex.Message);
                    }
                };

                s_mainWindow.Content = sut;
                Console.WriteLine("[TestApp] Element set as window content");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestApp] Dispatcher operation failed: {ex}");
                dispatcherException = ex;
                ctx.EndTest();
            }
        });

        if (dispatcherException != null)
            throw dispatcherException;

        if (!ctx.EWH.WaitOne(TimeSpan.FromSeconds(timeOutInSecs)))
            Assert.Fail("Test timed out waiting for control to load.");

        if (ctx?.ErrorMessage is not null)
            Assert.Fail($"Test failed with exception: {ctx.ErrorMessage}");

        Console.WriteLine("[TestApp] Test completed successfully");
    }

    public class UITestContext
    {
        public object SUT { get; set; } = null!;
        public EventWaitHandle EWH { get; set; } = new ManualResetEvent(false);
        public string? ErrorMessage { get; private set; }

        public void EndTest() =>
            _ = EWH.Set();

        public void Throw(string errorMessage)
        {
            ErrorMessage = errorMessage;
            _ = EWH.Set();
        }
    }
}
