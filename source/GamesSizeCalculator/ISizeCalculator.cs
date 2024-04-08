using Playnite.SDK.Models;
using System.Threading.Tasks;

namespace GamesSizeCalculator
{
    public interface ISizeCalculator
    {
        string ServiceName { get; }
        Task<ulong?> GetInstallSizeAsync(Game game);
        bool IsPreferredInstallSizeCalculator(Game game);
    }
}