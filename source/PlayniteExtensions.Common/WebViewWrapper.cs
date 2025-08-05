using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Common;

public interface IWebViewWrapper : IDisposable
{
    WebViewResponse DownloadPageSource(string url);
    WebViewResponse DownloadPageText(string url);
    Task<WebViewResponse> DownloadPageSourceAsync(string url);
    Task<WebViewResponse> DownloadPageTextAsync(string url);
}

public class WebViewResponse
{
    public string Url { get; set; }
    public string Content { get; set; }
}


/// <summary>
/// Intended for use for only one request
/// </summary>
public class OffScreenWebViewWrapper : IWebViewWrapper
{
    public OffScreenWebViewWrapper(IPlayniteAPI playniteAPI)
    {
        view = System.Windows.Application.Current.Dispatcher.Invoke(() => playniteAPI.WebViews.CreateOffscreenView());
        view.LoadingChanged += View_LoadingChanged;
    }

    private void View_LoadingChanged(object sender, Playnite.SDK.Events.WebViewLoadingChangedEventArgs e)
    {
        if (!e.IsLoading)
            loadCompleteEvent.Set();
    }

    private readonly IWebView view;
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly AsyncAutoResetEvent loadCompleteEvent = new();

    public WebViewResponse DownloadPageSource(string url) => DownloadPageSourceAsync(url).Result;
    public WebViewResponse DownloadPageText(string url) => DownloadPageTextAsync(url).Result;

    public async Task<WebViewResponse> DownloadPageSourceAsync(string url) => await DownloadPageContentAsync(url, v => v.GetPageSource());

    public async Task<WebViewResponse> DownloadPageTextAsync(string url) => await DownloadPageContentAsync(url, v => v.GetPageText());

    private async Task<WebViewResponse> DownloadPageContentAsync(string url, Func<IWebView, string> getContentMethod)
    {
        await semaphore.WaitAsync();
        try
        {
            logger.Debug($"Getting {url}");

            view.NavigateAndWait(url);

            var output = new WebViewResponse { Url = view.GetCurrentAddress() };
            output.Content = getContentMethod(view);

            logger.Debug($@"Result for getting {url}: {JsonConvert.SerializeObject(output)}");

            return output;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> NavigateAndWait(string url, int timeOutMilliseconds = 15000)
    {
        view.Navigate(url);
        return await RunWithTimeout(loadCompleteEvent.WaitAsync(), timeOutMilliseconds);
    }

    private static async Task<bool> RunWithTimeout(Task t, int waitms)
    {
        return await Task.WhenAny(t, Task.Delay(waitms)) == t;
    }

    public void Dispose()
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                view.LoadingChanged -= View_LoadingChanged;
                view.Close();
                view.Dispose();
            });
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error disposing WebViewWrapper");
        }
    }
}

internal sealed class AsyncAutoResetEvent
{
    private static readonly Task s_completed = Task.FromResult(true);
    private readonly Queue<TaskCompletionSource<bool>> _waits = new();
    private bool _signaled;

    public Task WaitAsync()
    {
        lock (_waits)
        {
            if (_signaled)
            {
                _signaled = false;
                return s_completed;
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                _waits.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }

    public void Set()
    {
        TaskCompletionSource<bool> toRelease = null;

        lock (_waits)
        {
            if (_waits.Count > 0)
            {
                toRelease = _waits.Dequeue();
            }
            else if (!_signaled)
            {
                _signaled = true;
            }
        }

        toRelease?.SetResult(true);
    }
}
