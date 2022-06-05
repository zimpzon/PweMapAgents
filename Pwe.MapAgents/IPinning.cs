using Pwe.Shared;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public interface IPinning
    {
        Task<Pin> GetCurrentPinning();
        Task StorePinning(Pin pinning);
    }
}
