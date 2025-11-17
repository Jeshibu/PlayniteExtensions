using Playnite.SDK;
using Playnite.SDK.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PlayniteExtensions.Tests.Common;

public class FakeWebViewFactory(Dictionary<string, string> sourceFilesByUrl) : IWebViewFactory
{
    private FakeWebView WebView => field ??= new(sourceFilesByUrl);

    public IWebView CreateOffscreenView() => WebView;

    public IWebView CreateOffscreenView(WebViewSettings settings) => WebView;

    public IWebView CreateView(int width, int height) => WebView;

    public IWebView CreateView(int width, int height, System.Windows.Media.Color background) => WebView;

    public IWebView CreateView(WebViewSettings settings) => WebView;

    public List<string> CalledUrls => WebView.CalledUrls;
}

public class FakeWebView(Dictionary<string, string> sourceFilesByUrl) : IWebView
{
    public string Url { get; set; }

    public bool CanExecuteJavascriptInMainFrame { get; set; } = true;

    public System.Windows.Window WindowHost => throw new NotImplementedException();
    public List<string> CalledUrls { get; set; } = new();

    public event EventHandler<WebViewLoadingChangedEventArgs> LoadingChanged;

    public void Close()
    {
    }

    public void DeleteCookies(string url, string name) => throw new NotImplementedException();

    public void DeleteDomainCookies(string domain) => throw new NotImplementedException();

    public void DeleteDomainCookiesRegex(string domainRegex) => throw new NotImplementedException();

    public void Dispose()
    {
    }

    public Task<JavaScriptEvaluationResult> EvaluateScriptAsync(string script) => throw new NotImplementedException();

    public List<HttpCookie> GetCookies() => throw new NotImplementedException();

    public string GetCurrentAddress() => Url;

    public string GetPageSource()
    {
        if (!sourceFilesByUrl.TryGetValue(Url, out string filePath))
            throw new KeyNotFoundException($"Couldn't find file path for URL {Url}");

        return File.ReadAllText(filePath);
    }

    public async Task<string> GetPageSourceAsync() => GetPageSource();

    public string GetPageText() => throw new NotImplementedException();

    public async Task<string> GetPageTextAsync() => throw new NotImplementedException();

    public void Navigate(string url) => NavigateAndWait(url);

    public void NavigateAndWait(string url)
    {
        CalledUrls.Add(url);
        Url = url;
    }

    public void Open() => throw new NotImplementedException();

    public bool? OpenDialog() => throw new NotImplementedException();

    public void SetCookies(string url, string domain, string name, string value, string path, DateTime expires) => throw new NotImplementedException();

    public void SetCookies(string url, HttpCookie cookie) => throw new NotImplementedException();
}