﻿using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Barnite.Scrapers;

public abstract class MetadataScraper
{
    public abstract string Name { get; }
    public abstract string WebsiteUrl { get; }

    protected IPlatformUtility PlatformUtility { get; set; }
    protected IWebDownloader Webclient { get; set; }
    public bool Initialized { get; protected set; } = false;

    public MetadataScraper() { }

    public void Initialize(IPlatformUtility platformUtility, IWebDownloader webclient)
    {
        PlatformUtility = platformUtility;
        Webclient = webclient;
        Initialized = true;
    }

    protected abstract string GetSearchUrlFromBarcode(string barcode);

    protected string GetAbsoluteUrl(string relativeUrl)
    {
        return relativeUrl.GetAbsoluteUrl(GetSearchUrlFromBarcode("1"));
    }

    public GameMetadata GetMetadataFromBarcode(string barcode)
    {
        if (!Initialized)
            throw new Exception("Not initialized");

        var searchUrl = GetSearchUrlFromBarcode(barcode);
        var response = Webclient.DownloadString(searchUrl, ScrapeRedirectUrl, ScrapeJsCookies);

        var data = ScrapeGameDetailsHtml(response.ResponseContent);
        if (data != null)
        {
            SetLink(response, data);
            return data;
        }

        //so that wasn't a game details page; try and parse it as a search result page instead
        var links = ScrapeSearchResultHtml(response.ResponseContent)?.ToList();
        if (links != null && links.Count == 1)
        {
            response = Webclient.DownloadString(links[0].Url, ScrapeRedirectUrl, ScrapeJsCookies);
            data = ScrapeGameDetailsHtml(response.ResponseContent);
            SetLink(response, data);
            return data;
        }

        return null;
    }

    /// <summary>
    /// Scrape an HTML page for game metadata. Implementing classes should fail fast (and return null) out of this method if this page does not represent game metadata.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    protected abstract GameMetadata ScrapeGameDetailsHtml(string html);

    /// <summary>
    /// Scrape a list of links to game detail pages from a search result.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    protected abstract IEnumerable<GameLink> ScrapeSearchResultHtml(string html);

    protected virtual CookieCollection ScrapeJsCookies(string html)
    {
        return null;
    }

    protected virtual string ScrapeRedirectUrl(string requestUrl, string html)
    {
        return null;
    }

    private void SetLink(DownloadStringResponse response, GameMetadata data)
    {
        if (data == null || response == null)
            return;

        var links = data.Links ??= [];
        links.Add(new Link(this.Name, response.ResponseUrl));
    }
}

public class GameLink
{
    public string Name;
    public string Url;
}
