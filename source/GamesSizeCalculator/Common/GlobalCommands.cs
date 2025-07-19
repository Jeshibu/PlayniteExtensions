﻿using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace PluginsCommon.Commands;

public static class GlobalCommands
{
    private static readonly ILogger logger = LogManager.GetLogger();

    public static RelayCommand<object> NavigateUrlCommand
    {
        get => new((url) =>
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
        get => new((path) =>
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
        if (url.IsNullOrEmpty())
        {
            throw new Exception("No URL was given.");
        }

        if (!url.IsUri())
        {
            url = "http://" + url;
        }

        ProcessStarter.StartUrl(url);
    }
}