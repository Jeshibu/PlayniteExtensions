using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GamersGateLibrary
{
    public interface IWebViewWrapper : IDisposable
    {
        string DownloadPageSource(string targetUrl);
    }

    /// <summary>
    /// Intended for use for only one request
    /// </summary>
    public class WebViewWrapper : IWebViewWrapper
    {
        public WebViewWrapper(IPlayniteAPI playniteAPI, int width = 675, int height = 600)
        {
            view = System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var v = playniteAPI.WebViews.CreateView(width, height, Colors.Black);
                v.Open();
                return v;
            });
            view.LoadingChanged += View_LoadingChanged;
        }

        private readonly IWebView view;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly object requestLifespanLock = new object();

        public string TargetUrl { get; private set; }
        private TaskCompletionSource<string> DownloadCompletionSource { get; set; }

        public string DownloadPageSource(string targetUrl)
        {
            lock (requestLifespanLock)
            {
                TargetUrl = targetUrl;

                view.Navigate(targetUrl);

                DownloadCompletionSource = new TaskCompletionSource<string>();
                DownloadCompletionSource.Task.Wait();
                string source = DownloadCompletionSource.Task.Result;
                DownloadCompletionSource = null;

                return source;
            }
        }

        private static bool IsAuthenticated(string pageSource)
        {
            bool authenticated = Regex.IsMatch(pageSource, @"/images/avatar/current/(\d+)");
            return authenticated;
        }

        private bool IsTargetUrl()
        {
            var currentUri = new Uri(view.GetCurrentAddress());
            return currentUri.GetLeftPart(UriPartial.Query) == TargetUrl;
        }

        private async void View_LoadingChanged(object sender, Playnite.SDK.Events.WebViewLoadingChangedEventArgs e)
        {
            try
            {
                if (!IsTargetUrl())
                    return;

                var source = await view.GetPageSourceAsync();
                if (IsAuthenticated(source))
                {
                    DownloadCompletionSource?.TrySetResult(source);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error trying to navigate to " + TargetUrl);
            }
        }

        public void Dispose()
        {
            try
            {
                view.Close();
                view.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error disposing WebViewWrapper");
            }
        }
    }
}
