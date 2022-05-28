using Pwe.Shared;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public interface ISelfie
    {
        Task<bool> IsSelfiePending();
        Task MarkPendingSelfieTaken();
        Task<(Image image, GeoCoord location)> Take(List<GeoCoord> path);
    }
}
