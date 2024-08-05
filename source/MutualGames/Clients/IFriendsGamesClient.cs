using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients
{
    public interface IFriendsGamesClient
    {
        string Name { get; }
        FriendSource Source { get; }
        Guid PluginId { get; }
        IEnumerable<FriendAccountInfo> GetFriends(CancellationToken cancellationToken);
        IEnumerable<ExternalGameData> GetFriendGames(FriendAccountInfo friend, CancellationToken cancellationToken);
        Task<bool> IsAuthenticatedAsync();

        IEnumerable<string> CookieDomains { get; }
        string LoginUrl { get; }
        Task<bool> IsLoginSuccessAsync(IWebView loginWebView);
    }
}
