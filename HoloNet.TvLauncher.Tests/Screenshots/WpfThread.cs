using System.Windows;
using System.Windows.Threading;
using HoloNet.TvLauncher.Themes;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests.Screenshots;

/// <summary>
/// The one UI thread the screenshot tests render on. WPF needs a single-threaded apartment with
/// a running dispatcher, which xUnit's worker threads aren't, and an <see cref="Application"/>
/// holding the launcher's theme, which can exist only once per process.
/// </summary>
internal static class WpfThread
{
    private static readonly Lazy<Dispatcher> UiDispatcher = new(Start);

    /// <summary>Runs <paramref name="work"/> on the UI thread, awaiting it there.</summary>
    public static Task<T> RunAsync<T>(Func<Task<T>> work) => UiDispatcher.Value.InvokeAsync(work).Task.Unwrap();

    private static Dispatcher Start()
    {
        var started = new TaskCompletionSource<Dispatcher>();
        var thread = new Thread(() =>
        {
            try
            {
                var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                application.Resources.MergedDictionaries.Add(Theme.Load());
                Motion.LoopsEnabled = false;
                started.SetResult(Dispatcher.CurrentDispatcher);
            }
            catch (Exception exception)
            {
                started.SetException(exception);
                return;
            }

            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "Screenshot UI thread"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return started.Task.GetAwaiter().GetResult();
    }
}
