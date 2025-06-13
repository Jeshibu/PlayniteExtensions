using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MutualGames.Models.Settings;

public class FriendSourceSettings : ObservableObject
{
    public string Name { get; set; }

    public FriendSource Source { get; set; }

    public ObservableCollection<FriendAccountInfo> Friends { get; set; } = new ObservableCollection<FriendAccountInfo>();

    private Action BackgroundAction(Action action)
    {
        return () => Application.Current.Dispatcher.BeginInvoke(action);
    }

    private void SetFriends()
    {
        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            try
            {
                Friends = Client.GetFriends(a.CancelToken).OrderBy(f => f.Name).ToObservable();

                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(Friends));
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting friends for {Name}");
                PlayniteApi.Dialogs.ShowErrorMessage($"Couldn't get friends for {Client?.Name} - check if you're authenticated.");
                OnPropertyChanged(nameof(AuthStatus));
            }
        }, new GlobalProgressOptions($"Getting {Source} friends", cancelable: true) { IsIndeterminate = true });
    }

    private void Login()
    {
        using (var webView = PlayniteApi.WebViews.CreateView(600, 550))
        {
            foreach (var cookieDomain in Client.CookieDomains)
                webView.DeleteDomainCookies(cookieDomain);

            webView.Navigate(Client.LoginUrl);

            webView.LoadingChanged += async (_, e) =>
            {
                if (e.IsLoading)
                    return;

                if (await Client.IsLoginSuccessAsync(webView))
                    webView.Close();
            };

            webView.OpenDialog();
        }
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    [DontSerialize]
    public RelayCommand RefreshCommand => new RelayCommand(SetFriends);

    [DontSerialize]
    public RelayCommand AuthenticateCommand => new RelayCommand(BackgroundAction(Login));

    [DontSerialize]
    public IFriendsGamesClient Client { get; set; }

    [DontSerialize]
    public bool IsAuthenticated => Client.IsAuthenticatedAsync().Result;

    [DontSerialize]
    public string HeaderText => $"{Name} ({Friends.Count} friends)";

    [DontSerialize]
    internal IPlayniteAPI PlayniteApi { get; set; }

    [DontSerialize]
    private ILogger logger = LogManager.GetLogger();

    [DontSerialize]
    public AuthStatus AuthStatus
    {
        get
        {
            try
            {
                if (IsAuthenticated)
                    return AuthStatus.Ok;
                else
                    return AuthStatus.AuthRequired;
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to check Groupees auth status.");
                return AuthStatus.Failed;
            }
        }
    }
}
