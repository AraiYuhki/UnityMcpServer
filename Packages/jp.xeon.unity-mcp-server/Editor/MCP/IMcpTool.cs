using System.Threading.Tasks;

namespace UnityMcp
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        string InputSchema { get; }
        Task<object> Execute(string arguments);
    }
}
