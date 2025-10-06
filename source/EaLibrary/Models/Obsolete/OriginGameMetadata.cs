using Playnite.SDK.Models;

namespace EaLibrary.Models;

public class OriginGameMetadata : GameMetadata
{
    public GameStoreDataResponse StoreDetails { get; set; }
    public StorePageMetadata StoreMetadata { get; set; }
}
