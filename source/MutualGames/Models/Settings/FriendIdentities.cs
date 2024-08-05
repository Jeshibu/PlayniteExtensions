using GongSolutions.Wpf.DragDrop;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MutualGames.Models.Settings
{
    public class FriendIdentities : IDropTarget
    {
        public ObservableCollection<FriendIdentity> Items { get; set; } = new ObservableCollection<FriendIdentity>();

        [DontSerialize]
        public RelayCommand<FriendIdentity> RemoveCommand { get; }

        public FriendIdentities()
        {
            RemoveCommand = new RelayCommand<FriendIdentity>(f => Items.Remove(f));
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var sourceItems = GetDragSourceItems(dropInfo);
            if (sourceItems.Count == 0)
            {
                dropInfo.Effects = DragDropEffects.None;
                dropInfo.DropTargetAdorner = null;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.Copy;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var sourceItems = GetDragSourceItems(dropInfo);
            if (sourceItems.Count == 0)
                return;

            if (dropInfo.TargetItem is FriendIdentity fi)
            {
                fi.Accounts.AddMissing(sourceItems);
            }
            else if (dropInfo.TargetCollection is IList<FriendAccountInfo> accounts)
            {
                accounts.AddMissing(sourceItems);
            }
            else if (dropInfo.TargetCollection is IList<FriendIdentity> friendIdentities)
            {
                var g = new FriendIdentity { FriendName = sourceItems[0].Name };
                g.Accounts.AddMissing(sourceItems);
                friendIdentities.Add(g);
            }
        }

        private List<FriendAccountInfo> GetDragSourceItems(IDropInfo dropInfo)
        {
            if (dropInfo.Data is IEnumerable<FriendAccountInfo> enumerable)
                return enumerable.ToList();

            if (dropInfo.Data is FriendAccountInfo fi)
                return new List<FriendAccountInfo> { fi };

            return new List<FriendAccountInfo>();
        }
    }
}
