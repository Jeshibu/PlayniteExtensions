﻿using GongSolutions.Wpf.DragDrop;
using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace MutualGames
{
    public class MutualGamesSettings : ObservableObject
    {
        public ObservableCollection<FriendSourceSettings> FriendSources { get; set; } = new ObservableCollection<FriendSourceSettings>();
        public FriendIdentities FriendIdentities { get; set; } = new FriendIdentities();
        public GameField ImportTo { get; set; } = GameField.Categories;
        public string PropertyNameFormat { get; set; } = "Owned by {0}";
        public CrossLibraryImportMode CrossLibraryImportMode { get; set; } = CrossLibraryImportMode.ImportAll;
        public Guid ImportCrossLibraryFeatureId { get; set; } = Guid.Empty;
    }

    public class FriendSourceSettings : ObservableObject
    {
        public string Name { get; set; }
        public ObservableCollection<FriendInfo> Friends { get; set; } = new ObservableCollection<FriendInfo>();

        public RelayCommand RefreshCommand => new RelayCommand(BackgroundAction(SetFriends));

        public RelayCommand AuthenticateCommand => new RelayCommand(BackgroundAction(Login));

        private Action BackgroundAction(Action action)
        {
            return () => Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void SetFriends()
        {
            var friends = Client.GetFriends();
            Friends.Clear();
            foreach (var friend in friends)
                Friends.Add(friend);

            Friends.Sort(f => f.Name);

            OnPropertyChanged(nameof(HeaderText));
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
                    {
                        webView.Close();
                    }
                };

                webView.OpenDialog();
            }
            OnPropertyChanged(nameof(IsAuthenticated));
        }

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
                    if (Client.IsAuthenticatedAsync().Result)
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

    public class FriendIdentities : IDropTarget
    {
        public ObservableCollection<FriendIdentityGrouping> Items { get; set; } = new ObservableCollection<FriendIdentityGrouping>();

        public void DragOver(IDropInfo dropInfo)
        {
            var sourceItems = GetDragSourceItems(dropInfo);
            if (sourceItems.Count == 0)
            {
                dropInfo.Effects = System.Windows.DragDropEffects.None;
                dropInfo.DropTargetAdorner = null;
            }
            else
            {
                dropInfo.Effects = System.Windows.DragDropEffects.Copy;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var sourceItems = GetDragSourceItems(dropInfo);
            if (sourceItems.Count == 0)
                return;

            if (dropInfo.TargetItem is FriendIdentityGrouping fig)
            {
                fig.Identities.AddMissing(sourceItems);
            }
            else if (dropInfo.TargetCollection is IList<FriendInfo> friends)
            {
                friends.AddMissing(sourceItems);
            }
            else if (dropInfo.TargetCollection is IList<FriendIdentityGrouping> friendGroupings)
            {
                var g = new FriendIdentityGrouping { FriendName = sourceItems[0].Name };
                g.Identities.AddMissing(sourceItems);
                friendGroupings.Add(g);
            }
        }

        private List<FriendInfo> GetDragSourceItems(IDropInfo dropInfo)
        {
            if (dropInfo.Data is IEnumerable<FriendInfo> enumerable)
                return enumerable.ToList();

            if (dropInfo.Data is FriendInfo fi)
                return new List<FriendInfo> { fi };

            return new List<FriendInfo>();
        }
    }

    public class FriendIdentityGrouping
    {
        public string FriendName { get; set; }
        public ObservableCollection<FriendInfo> Identities { get; set; } = new ObservableCollection<FriendInfo>();
    }

    public enum FriendSource
    {
        Steam,
        EA,
        GOG
    }

    public enum CrossLibraryImportMode
    {
        SameLibraryOnly,
        ImportAll,
        ImportAllWithFeature,
    }

    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        Failed
    }

    public class MutualGamesSettingsViewModel : PluginSettingsViewModel<MutualGamesSettings, MutualGames>
    {
        private List<GameFeature> gameFeatures;
        public IFriendsGamesClient[] Clients { get; }
        public GameField[] ImportFieldOptions { get; } = new[] { GameField.Categories, GameField.Tags };
        public List<GameFeature> Features
        {
            get
            {
                if (gameFeatures != null)
                    return gameFeatures;

                var output = new List<GameFeature> { new GameFeature { Id = Guid.Empty, Name = "None" } };
                output.AddRange(PlayniteApi.Database.Features.OrderBy(f => f.Name));
                return gameFeatures = output;
            }
        }

        public bool SameLibraryChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.SameLibraryOnly;
            set { if (value) Settings.CrossLibraryImportMode = CrossLibraryImportMode.SameLibraryOnly; }
        }
        public bool ImportAllChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAll;
            set { if (value) Settings.CrossLibraryImportMode = CrossLibraryImportMode.ImportAll; }
        }
        public bool ImportAllWithFeatureChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAllWithFeature;
            set { if (value) Settings.CrossLibraryImportMode = CrossLibraryImportMode.ImportAllWithFeature; }
        }

        public MutualGamesSettingsViewModel(MutualGames plugin) : base(plugin, plugin.PlayniteApi)
        {
            Clients = plugin.GetClients().ToArray();
            InitializeSettings();
        }

        public void InitializeSettings()
        {
            Settings = Plugin.LoadPluginSettings<MutualGamesSettings>();

            if (Settings == null)
            {
                Settings = new MutualGamesSettings();
                var titleComparer = new TitleComparer();
                var crossPlatformFeature = Features.FirstOrDefault(f => titleComparer.Equals(f.Name, "Cross-Platform Multiplayer"));
                if (crossPlatformFeature != null)
                {
                    Settings.CrossLibraryImportMode = CrossLibraryImportMode.ImportAllWithFeature;
                    Settings.ImportCrossLibraryFeatureId = crossPlatformFeature.Id;
                }
            }

            foreach (var c in Clients)
            {
                var existingSettings = Settings.FriendSources.FirstOrDefault(fs => fs.Name == c.Name);
                if (existingSettings == null)
                {
                    Settings.FriendSources.Add(new FriendSourceSettings { Client = c, Name = c.Name, PlayniteApi = PlayniteApi });
                }
                else
                {
                    existingSettings.Client = c;
                    existingSettings.PlayniteApi = PlayniteApi;
                }
            }
        }

        public override bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            try
            {
                var exampleFriend = new FriendInfo { Id = "42069", Name = "xXx_1337Slayer_xXx", Source = "Steam" };
                var formatted = string.Format(Settings.PropertyNameFormat, exampleFriend.Name, exampleFriend.Source);
            }
            catch (ArgumentNullException)
            {
                errors.Add("Format can't be empty");
            }
            catch (FormatException)
            {
                errors.Add("Format is invalid. Make sure you only use {0} and {1} as variables, and escape any other uses of { or } with the same character (so {{ or }})");
            }

            return errors.Count == 0;
        }
    }
}