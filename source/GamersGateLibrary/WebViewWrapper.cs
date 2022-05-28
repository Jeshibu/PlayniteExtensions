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

    public class WebViewWrapper : IWebViewWrapper
    {
        public WebViewWrapper(IPlayniteAPI playniteAPI, int width = 675, int height = 600)
        {
            view = System.Windows.Application.Current.Dispatcher.Invoke(() => playniteAPI.WebViews.CreateView(width, height, Colors.Black));
            view.LoadingChanged += View_LoadingChanged;
        }

        private readonly IWebView view;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly object requestLifespanLock = new object();

        public string TargetUrl { get; private set; }

        public string DownloadPageSource(string targetUrl)
        {
            lock (requestLifespanLock)
            {
                TargetUrl = targetUrl;

                var output = System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    view.NavigateAndWait(targetUrl);
                    string src = view.GetPageSource();

                    if (!IsTargetUrl() || !IsAuthenticated(src))
                    {
                        view.OpenDialog();
                    }
                    return src;
                });

                return output;
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
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (!IsTargetUrl())
                        return;

                    var source = await view.GetPageSourceAsync();
                    if (IsAuthenticated(source))
                        view.Close();
                    else
                        return;
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error trying to navigate to " + TargetUrl);
            }
        }

        public void Dispose()
        {
            view.Dispose();
        }
    }
}
