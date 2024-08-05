using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace MutualGames.Models.Settings
{
    public class FriendAccountInfo : IEquatable<FriendAccountInfo>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FriendSource Source { get; set; }

        [DontSerialize]
        public string DisplayText => $"{Name} ({Source} - {Id})";

        public override bool Equals(object obj)
        {
            if (obj is FriendAccountInfo fi)
                return Source == fi.Source && Id == fi.Id;
            else
                return false;
        }

        public bool Equals(FriendAccountInfo other)
        {
            return other != null &&
                   Id == other.Id &&
                   Source == other.Source;
        }

        public override int GetHashCode()
        {
            int hashCode = -543865608;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<FriendSource>.Default.GetHashCode(Source);
            return hashCode;
        }

        public static bool operator ==(FriendAccountInfo left, FriendAccountInfo right)
        {
            return EqualityComparer<FriendAccountInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(FriendAccountInfo left, FriendAccountInfo right)
        {
            return !(left == right);
        }
    }
}