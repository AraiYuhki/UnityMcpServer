using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Console
{
    /// <summary>
    /// Unity ConsoleのログエントリをキャッシュからAIに返すツール
    /// </summary>
    public class GetConsoleLogs : IMcpTool
    {
        public string Name => "get_console_logs";

        public string Description =>
            "Get log messages from the Unity Console (errors, warnings, and logs). " +
            "Returns cached log entries since the last Domain Reload. " +
            "Use this to diagnose runtime errors, check debug output, or verify behavior after running the game. " +
            "Supports filtering by log type and message content.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"logTypes\":{\"type\":\"array\",\"items\":{\"type\":\"string\",\"enum\":[\"Log\",\"Warning\",\"Error\",\"Assert\",\"Exception\"]}," +
            "\"description\":\"Log types to include. If omitted, all types are returned.\"}," +
            "\"maxCount\":{\"type\":\"integer\",\"description\":\"Maximum number of entries to return, taken from most recent (default: 100)\",\"default\":100}," +
            "\"filter\":{\"type\":\"string\",\"description\":\"Return only entries whose message contains this string.\"}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var allEntries = ConsoleLogCache.GetEntries();
            var totalCount = allEntries.Length;

            var filtered = ApplyFilters(allEntries, parameters);
            var trimmed = TrimToMaxCount(filtered, parameters.MaxCount);

            return Task.FromResult<object>(new ConsoleLogResult(totalCount, trimmed));
        }

        private static GetConsoleLogsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetConsoleLogsArgs();
            }

            return JsonConvert.DeserializeObject<GetConsoleLogsArgs>(args) ?? new GetConsoleLogsArgs();
        }

        private static List<LogEntry> ApplyFilters(LogEntry[] entries, GetConsoleLogsArgs parameters)
        {
            var hasTypeFilter = parameters.LogTypes != null && parameters.LogTypes.Count > 0;
            var hasTextFilter = !string.IsNullOrEmpty(parameters.Filter);
            var result = new List<LogEntry>();

            foreach (var entry in entries)
            {
                if (hasTypeFilter && !parameters.LogTypes.Contains(entry.Type))
                {
                    continue;
                }

                if (hasTextFilter && !entry.Message.Contains(parameters.Filter))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        private static List<LogEntry> TrimToMaxCount(List<LogEntry> entries, int maxCount)
        {
            if (entries.Count <= maxCount)
            {
                return entries;
            }

            return entries.GetRange(entries.Count - maxCount, maxCount);
        }
    }

    internal class GetConsoleLogsArgs
    {
        [JsonProperty("logTypes")]
        public List<string> LogTypes { get; set; } = new();

        [JsonProperty("maxCount")]
        public int MaxCount { get; set; } = 100;

        [JsonProperty("filter")]
        public string Filter { get; set; } = string.Empty;
    }
}
