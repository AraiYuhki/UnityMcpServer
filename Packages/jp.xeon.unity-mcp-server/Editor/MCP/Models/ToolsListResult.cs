using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Models
{
    public class ToolsListResult
    {
        [JsonProperty("tools")]
        public List<ToolInfo> Tools { get; set; } = new List<ToolInfo>();
    }
}
