using Playnite.SDK;
using System.Net;

namespace PlayniteExtensions.Common
{
    public static class PlayniteConvert
    {
        public static Cookie ToCookie(this HttpCookie httpCookie)
        {
            if (httpCookie == null)
                return null;

            var cookie = new Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path, httpCookie.Domain)
            {
                HttpOnly = httpCookie.HttpOnly,
                Secure = httpCookie.Secure,
            };

            if (httpCookie.Expires.HasValue)
                cookie.Expires = httpCookie.Expires.Value;

            return cookie;
        }
    }
}
