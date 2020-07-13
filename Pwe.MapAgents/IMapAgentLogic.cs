using Pwe.Shared;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public enum AgentCommand { Continue, };

    public interface IMapAgentLogic
    {
        Task UpdateAgent(string agentId, AgentCommand command);
        Task<MapAgentPath> GetPath(string agentId);
        Task<string> GetAgentClientPath(string agentId);
    }
}
