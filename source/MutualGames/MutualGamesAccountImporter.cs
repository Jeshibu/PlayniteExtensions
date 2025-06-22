using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MutualGames.Models.Settings;

namespace MutualGames;

public sealed class MutualGamesAccountImporter : MutualGamesBaseImporter
{
    private readonly IFriendsGamesClient[] clients;

    public MutualGamesAccountImporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings, IEnumerable<IFriendsGamesClient> clients) : base(playniteAPI, settings)
    {
        this.clients = clients.ToArray();
    }

    public void Import()
    {
        var result = playniteAPI.Dialogs.ActivateGlobalProgress(a =>
        {
            a.ProgressMaxValue = settings.FriendIdentities.Items.SelectMany(fg => fg.Accounts).Count() + 1;

            matchingHelper.GetDeflatedNames(playniteAPI.Database.Games.Select(g => g.Name));
            a.CurrentProgressValue++;

            using (playniteAPI.Database.BufferedUpdate())
            {
                foreach (var friendIdentityGrouping in settings.FriendIdentities.Items)
                {
                    if (a.CancelToken.IsCancellationRequested) break;

                    foreach (var friend in friendIdentityGrouping.Accounts)
                    {
                        a.Text = $"Getting games for {friendIdentityGrouping.FriendName} ({friend.Source} - {friend.Name})";
                        try
                        {
                            var matchingGames = GetMatchingGames(friend, friendIdentityGrouping.FriendName, a.CancelToken, out var dbItem);
                            foreach (var matchingGame in matchingGames)
                                if (AddPropertyToGame(matchingGame, dbItem))
                                    playniteAPI.Database.Games.Update(matchingGame);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error while getting games for {friend.DisplayText}");
                            //TODO: display error to user
                        }
                        a.CurrentProgressValue++;
                    }
                }
            }
        }, new GlobalProgressOptions("Importing friend games", cancelable: true) { IsIndeterminate = false });

        playniteAPI.Dialogs.ShowMessage($"Imported {updatedCount} new friends' games.", "Mutual Games import done");
    }

    private IEnumerable<Game> GetMatchingGames(FriendAccountInfo account, string friendName, CancellationToken cancellationToken, out DatabaseObject dbItem)
    {
        dbItem = null;

        var output = new List<Game>();
        var client = clients.FirstOrDefault(c => c.Source == account.Source);
        if (client == null) return new Game[0];

        dbItem = GetDatabaseItem(friendName, client.Name);

        var friendGames = client.GetFriendGames(account, cancellationToken).ToList();

        return GetMatchingGames(client.PluginId, friendGames, account.DisplayText);
    }
}
