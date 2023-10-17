using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace PlayniteExtensions.Common
{
    public static class GlobalCommands
    {
        private static ILogger logger = LogManager.GetLogger();

        public static RelayCommand<object> NavigateUrlCommand
        {
            get => new RelayCommand<object>((url) =>
            {
                try
                {
                    NavigateUrl(url);
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    logger.Error(e, "Failed to open url.");
                }
            });
        }

        public static RelayCommand<string> NavigateDirectoryCommand
        {
            get => new RelayCommand<string>((path) =>
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Process.Start(path);
                    }
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    logger.Error(e, "Failed to open directory.");
                }
            });
        }

        public static void NavigateUrl(object url)
        {
            if (url is string stringUrl)
            {
                NavigateUrl(stringUrl);
            }
            else if (url is Link linkUrl)
            {
                NavigateUrl(linkUrl.Url);
            }
            else if (url is Uri uriUrl)
            {
                NavigateUrl(uriUrl.OriginalString);
            }
            else
            {
                throw new Exception("Unsupported URL format.");
            }
        }

        public static void NavigateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("No URL was given.");
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                url = "http://" + url;
            }

            StartUrl(url);
        }

        public static Process StartUrl(string url)
        {
            logger.Debug($"Opening URL: {url}");
            try
            {
                return Process.Start(url);
            }
            catch (Exception e)
            {
                // There are some crash report with 0x80004005 error when opening standard URL.
                logger.Error(e, "Failed to open URL.");
                return Process.Start(CmdLineTools.Cmd, $"/C start {url}");
            }
        }

        private static class CmdLineTools
        {
            public const string TaskKill = "taskkill";
            public const string Cmd = "cmd";
            public const string IPConfig = "ipconfig";
        }
    }
}
