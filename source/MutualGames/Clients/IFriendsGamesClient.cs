using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;

namespace MutualGames.Clients
{
    public interface IFriendsGamesClient
    {
        string Name { get; }
        Guid PluginId { get; }
        IEnumerable<FriendInfo> GetFriends();
        IEnumerable<GameDetails> GetFriendGames(FriendInfo friend);
        bool IsAuthenticated();

        IEnumerable<string> CookieDomains { get; }
        string LoginUrl { get; }
        bool IsLoginSuccess(IWebView loginWebView);
    }

    public class FriendInfo : IEquatable<FriendInfo>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }

        public string DisplayText => $"{Name} ({Source} - {Id})";

        public override bool Equals(object obj)
        {
            if (obj is FriendInfo fi)
                return Source == fi.Source && Id == fi.Id;
            else
                return false;
        }

        public bool Equals(FriendInfo other)
        {
            return !(other is null) &&
                   Id == other.Id &&
                   Source == other.Source;
        }

        public override int GetHashCode()
        {
            int hashCode = -543865608;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Source);
            return hashCode;
        }

        public static bool operator ==(FriendInfo left, FriendInfo right)
        {
            return EqualityComparer<FriendInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(FriendInfo left, FriendInfo right)
        {
            return !(left == right);
        }
    }
}
