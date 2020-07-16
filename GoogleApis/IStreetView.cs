using Pwe.Shared;
using System.Threading.Tasks;

namespace GoogleApis
{
    public interface IStreetView
    {
        Task<byte[]> GetRandomImage(GeoCoord point);
    }
}
