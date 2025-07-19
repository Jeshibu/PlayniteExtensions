using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Events;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MobyGamesMetadata.Tests;

public class SearchResultTests
{
    [Fact]
    public void NumericTitleUrlParsesCorrectId()
    {
        var sr = new SearchResult();
        sr.SetUrlAndId("https://www.mobygames.com/game/86550/640/");
        Assert.Equal(86550, sr.Id);
    }

    [Fact]
    public void SearchResultParsingTest()
    {
        string html = File.ReadAllText("GameSearch.html");
        var webViewFactory = new FakeWebViewFactory(html);
        MobyGamesScraper scraper = new(new PlatformUtility([]), webViewFactory);
        var searchResults = scraper.GetGameSearchResults("Phantom Breaker Omnia").ToList();
        Assert.NotEmpty(searchResults);
    }

    private class FakeWebViewFactory : IWebViewFactory
    {
        public FakeWebViewFactory(string pageSource)
        {
            PageSource = pageSource;
        }

        public string PageSource { get; }

        public IWebView CreateOffscreenView() => new FakeWebView(PageSource);

        public IWebView CreateOffscreenView(WebViewSettings settings) => throw new NotImplementedException();

        public IWebView CreateView(int width, int height) => throw new NotImplementedException();

        public IWebView CreateView(int width, int height, System.Windows.Media.Color background) => throw new NotImplementedException();

        public IWebView CreateView(WebViewSettings settings) => throw new NotImplementedException();
    }

    private class FakeWebView : IWebView
    {
        public FakeWebView(string pageSource)
        {
            PageSource = pageSource;
        }

        public bool CanExecuteJavascriptInMainFrame { get; set; } = true;

        public System.Windows.Window WindowHost => throw new NotImplementedException();

        public string PageSource { get; }

        public event EventHandler<WebViewLoadingChangedEventArgs> LoadingChanged;

        public void Close() { }

        public void DeleteCookies(string url, string name) => throw new NotImplementedException();

        public void DeleteDomainCookies(string domain) => throw new NotImplementedException();

        public void DeleteDomainCookiesRegex(string domainRegex) => throw new NotImplementedException();

        public void Dispose() { }

        public Task<JavaScriptEvaluationResult> EvaluateScriptAsync(string script) => throw new NotImplementedException();

        public List<HttpCookie> GetCookies() => throw new NotImplementedException();

        public string GetCurrentAddress() => throw new NotImplementedException();

        public string GetPageSource() => PageSource;

        public Task<string> GetPageSourceAsync() => throw new NotImplementedException();

        public string GetPageText() => throw new NotImplementedException();

        public Task<string> GetPageTextAsync() => throw new NotImplementedException();

        public void Navigate(string url) => throw new NotImplementedException();

        public void NavigateAndWait(string url) { }

        public void Open() => throw new NotImplementedException();

        public bool? OpenDialog() => throw new NotImplementedException();

        public void SetCookies(string url, string domain, string name, string value, string path, DateTime expires) => throw new NotImplementedException();

        public void SetCookies(string url, HttpCookie cookie) => throw new NotImplementedException();
    }
}
