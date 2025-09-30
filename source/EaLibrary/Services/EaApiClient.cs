using Playnite.SDK;

namespace EaLibrary.Services;

public class EaApiClient
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Origin/10.6.0.00000 EAApp/13.468.0.5990 Chrome/109.0.5414.120 Safari/537.36";
    private const string ClientId = "JUNO_PC_CLIENT";
    private const string ClientSecret = "4mRLtYMb6vq9qglomWEaT4ChxsXWcyqbQpuBNfMPOYOiDmYYQmjuaBsF2Zp0RyVeWkfqhE9TuGgAw7te";
    private const string LoggedOutUrl = "qrc:///html/logout.html";
    private const string LoggedInUrl = "qrc:///html/login_successful.html";

    public IWebViewFactory WebViewFactory { get; }

    private WebViewSettings WebViewSettings => new() { UserAgent = UserAgent, JavaScriptEnabled = true, WindowWidth = 800, WindowHeight = 600 };

    public EaApiClient(IWebViewFactory webViewFactory)
    {
        WebViewFactory = webViewFactory;
    }

    public bool IsAuthenticated()
    {
        using var webview = WebViewFactory.CreateOffscreenView(WebViewSettings);
        bool isAuthenticated = false;
        webview.LoadingChanged += (_, e) =>
        {
            if (webview.GetCurrentAddress() == LoggedInUrl)
            {
                isAuthenticated = true;
                webview.Close();
            }
        };

        return isAuthenticated;
    }

    public string GetAuthToken(IWebView webView)
    {
        /*
            request.AddParameter("client_id", "JUNO_PC_CLIENT");
            request.AddParameter("response_type", "token");
            request.AddParameter("redirect_uri", "https://pc.ea.com/login.html");
            request.AddParameter("token_format", "JWT");
            request.AddParameter("pc_sign", HardwareInfo.GetPcSign());
         */
        var pcSign = PcSignature.GetPcSignature();
        var url = $"https://accounts.ea.com/connect/auth?client_id={ClientId}&redirect_uri=https://pc.ea.com/login.html&display=junoClient/login&locale=en_US&pc_sign={pcSign}&response_type=token&token_format=JWT";
        return null;
    }
}
