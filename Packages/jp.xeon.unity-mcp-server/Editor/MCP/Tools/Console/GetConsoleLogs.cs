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
            "Returns cached log entries since the last Domain Reload, each with a monotonically increasing 'id'. " +
            "Pass the 'nextToken' from a previous call (or from a state-transition tool result) as 'sinceToken' " +
            "to get only the entries that appeared since then. " +
            "Use onlyErrors/onlyExceptions to isolate problems, e.g. 'only exceptions raised by the last operation'.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"logTypes\":{\"type\":\"array\",\"items\":{\"type\":\"string\",\"enum\":[\"Log\",\"Warning\",\"Error\",\"Assert\",\"Exception\"]}," +
            "\"description\":\"Log types to include. If omitted, all types are returned.\"}," +
            "\"maxCount\":{\"type\":\"integer\",\"description\":\"Maximum number of entries to return, taken from most recent (default: 100)\",\"default\":100}," +
            "\"filter\":{\"type\":\"string\",\"description\":\"Return only entries whose message contains this string.\"}," +
            "\"sinceToken\":{\"type\":\"integer\",\"description\":\"Return only entries whose id is greater than or equal to this token.\"}," +
            "\"onlyErrors\":{\"type\":\"boolean\",\"description\":\"Return only Error/Exception/Assert entries (default: false)\",\"default\":false}," +
            "\"onlyExceptions\":{\"type\":\"boolean\",\"description\":\"Return only Exception entries (default: false)\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var allEntries = SelectSource(parameters.SinceToken);
            var totalCount = allEntries.Length;

            var filtered = ApplyFilters(allEntries, parameters);
            var trimmed = TrimToMaxCount(filtered, parameters.MaxCount);
            var truncated = parameters.SinceToken > 0 && ConsoleLogCache.OldestToken > parameters.SinceToken;

            return Task.FromResult<object>(
                new ConsoleLogResult(totalCount, trimmed, ConsoleLogCache.CurrentToken, truncated));
        }

        private static LogEntry[] SelectSource(long sinceToken)
        {
            if (sinceToken <= 0)
            {
                return ConsoleLogCache.GetEntries();
            }

            return ConsoleLogCache.GetEntriesSince(sinceToken);
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
            var result = new List<LogEntry>();

            foreach (var entry in entries)
            {
                if (IsIncluded(entry, parameters))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static bool IsIncluded(LogEntry entry, GetConsoleLogsArgs parameters)
        {
            if (parameters.OnlyExceptions && !entry.IsException())
            {
                return false;
            }

            if (parameters.OnlyErrors && !entry.IsError())
            {
                return false;
            }

            if (!MatchesTypeFilter(entry, parameters.LogTypes))
            {
                return false;
            }

            return MatchesTextFilter(entry, parameters.Filter);
        }

        private static bool MatchesTypeFilter(LogEntry entry, List<string> logTypes)
        {
            if (logTypes == null || logTypes.Count == 0)
            {
                return true;
            }

            return logTypes.Contains(entry.Type);
        }

        private static bool MatchesTextFilter(LogEntry entry, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            return entry.Message.Contains(filter);
        }

        private static List<LogEntry> TrimToMaxCount(List<LogEntry> entries, int maxCount)
        {
            if (maxCount <= 0 || entries.Count <= maxCount)
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

        [JsonProperty("sinceToken")]
        public long SinceToken { get; set; }

        [JsonProperty("onlyErrors")]
        public bool OnlyErrors { get; set; }

        [JsonProperty("onlyExceptions")]
        public bool OnlyExceptions { get; set; }
    }
}
