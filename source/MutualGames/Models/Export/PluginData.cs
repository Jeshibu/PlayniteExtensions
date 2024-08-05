using Playnite.SDK.Plugins;
using System;

namespace MutualGames.Models.Export
{
    public class PluginData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public static PluginData FromPlugin(LibraryPlugin plugin)
        {
            return new PluginData { Id = plugin.Id, Name = plugin.Name };
        }
    }
}