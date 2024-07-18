using Playnite.SDK;
using System;
using System.Threading.Tasks;

namespace PlayniteExtensions.Common
{
    public interface IWebViewWrapper : IDisposable
    {
        WebViewResponse DownloadPageSource(string url);
        Task<WebViewResponse> DownloadPageSourceAsync(string url);
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
            view = System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IWebView v = playniteAPI.WebViews.CreateOffscreenView();
                return v;
            });
        }

        private readonly IWebView view;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly object requestLifespanLock = new object();

        public WebViewResponse DownloadPageSource(string url)
        {
            lock (requestLifespanLock)
            {
                logger.Debug($"Getting {url}");

                view.NavigateAndWait(url);

                return new WebViewResponse
                {
                    Url = view.GetCurrentAddress(),
                    Content = view.GetPageSource()
                };
            }
        }

        public async Task<WebViewResponse> DownloadPageSourceAsync(string url)
        {
            logger.Debug($"Getting {url}");

            view.NavigateAndWait(url);

            return new WebViewResponse
            {
                Url = view.GetCurrentAddress(),
                Content = await view.GetPageSourceAsync()
            };
        }

        public void Dispose()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
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
}
