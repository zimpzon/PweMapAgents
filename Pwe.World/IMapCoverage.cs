using Pwe.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IMapCoverage
    {
        Task UpdateCoverage(List<GeoCoord> points);
    }
}
