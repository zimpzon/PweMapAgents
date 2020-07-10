using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public enum AgentCommand { Continue, };

    public interface IMapAgentLogic
    {
        Task UpdateAgent(string id, AgentCommand command);
    }
}
