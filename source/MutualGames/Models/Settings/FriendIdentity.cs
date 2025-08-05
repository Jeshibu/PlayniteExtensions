using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.ObjectModel;

namespace MutualGames.Models.Settings;

public class FriendIdentity
{
    public string FriendName { get; set; }
    public ObservableCollection<FriendAccountInfo> Accounts { get; set; } = [];

    [DontSerialize]
    public RelayCommand<FriendAccountInfo> RemoveCommand { get; }

    public FriendIdentity()
    {
        RemoveCommand = new RelayCommand<FriendAccountInfo>(f => Accounts.Remove(f));
    }
}
