using System.Threading;
using System.Threading.Tasks;

namespace ViveportLibrary.Api
{
    public interface IViveportApiClient
    {
        Task<GetCustomAttributeResponseRoot> GetAttributesAsync(CancellationToken cancellationToken = default);
        Task<CmsAppDetailsResponse> GetGameDetailsAsync(string appId, CancellationToken cancellationToken = default);
    }
}