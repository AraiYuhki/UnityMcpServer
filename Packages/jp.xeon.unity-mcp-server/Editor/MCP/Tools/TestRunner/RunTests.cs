using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.TestTools.TestRunner.Api;
using UnityMcp.Tools.Compile;
using UnityMcp.Tools.Scene;

namespace UnityMcp
{
    /// <summary>
    /// Unity Test Runnerでテストを開始するツール
    /// PlayModeテストはPlay Mode移行時にドメインリロードが発生し、開始要求元のawaitが
    /// 消滅してしまうため、このツールはテストを開始した事実のみを即時返す。
    /// 結果は対応するget_*_test_resultsツールでポーリングする。
    /// </summary>
    public class RunTests : IMcpTool
    {
        public string Name { get; }
        public string Description { get; }

        public string InputSchema { get; } =
            "{\"type\":\"object\",\"properties\":{" +
            "\"autoSave\":{\"type\":\"boolean\",\"description\":\"Save modified scenes before running instead of refusing (default: false)\",\"default\":false}," +
            "\"discard\":{\"type\":\"boolean\",\"description\":\"Discard unsaved scene changes before running instead of refusing (default: false)\",\"default\":false}," +
            "\"force\":{\"type\":\"boolean\",\"description\":\"Bypass the compile gate and run even when compilation is unsettled or failing (default: false)\",\"default\":false}," +
            "\"testNames\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Run only tests whose full name (FixtureName.TestName) exactly matches one of these.\"}," +
            "\"groupNames\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Run only tests whose full name matches one of these regex patterns. Useful for a fixture or namespace, e.g. \\\"^MyNamespace\\\\.\\\"\"}," +
            "\"categoryNames\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Run only tests/fixtures tagged with one of these NUnit [Category] names.\"}," +
            "\"assemblyNames\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Run only tests in these assemblies (assembly name without .dll).\"}," +
            "\"onlyFailures\":{\"type\":\"boolean\",\"description\":\"Rerun only the tests that failed or were skipped in the last completed run of this test mode, instead of the full suite (default: false)\",\"default\":false}," +
            "\"changedFilesOnly\":{\"type\":\"boolean\",\"description\":\"Run only tests in assemblies affected by uncommitted git changes (and any assembly that depends on them), instead of the full suite (default: false)\",\"default\":false}," +
            "\"gitRef\":{\"type\":\"string\",\"description\":\"Git ref to diff against when changedFilesOnly is true (default: \\\"HEAD\\\")\",\"default\":\"HEAD\"}" +
            "},\"required\":[]}";

        private readonly TestMode testMode;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ツール名</param>
        /// <param name="description">ツールの説明</param>
        /// <param name="testMode">実行するテストモード</param>
        public RunTests(string name, string description, TestMode testMode)
        {
            Name = name;
            Description = description;
            this.testMode = testMode;
        }

        /// <summary>
        /// 指定されたモードでテストを開始する。完了は待たない。
        /// 古いアセンブリでの実行と未保存シーンのモーダル停止を事前に封じる。
        /// </summary>
        /// <returns>開始できたかどうかを示すステータス</returns>
        public Task<object> Execute(string args)
        {
            if (TestRunSessionState.IsRunning(testMode))
            {
                return Task.FromResult<object>(new TestRunStatus
                {
                    Status = "running",
                    Message = $"{testMode} tests are already running. Poll the corresponding get_*_test_results tool."
                });
            }

            var parameters = ParseArgs(args);
            CompileGate.Ensure(Name, parameters.Force);
            SceneDirtyGuard.Resolve(Name, parameters.AutoSave, parameters.Discard);

            var filter = BuildFilter(parameters, out var skipReason);
            if (skipReason != null)
            {
                return Task.FromResult<object>(new TestRunStatus
                {
                    Status = "not_started",
                    Message = skipReason
                });
            }

            StartRun(filter);

            var hasFilter = filter.testNames != null || filter.groupNames != null
                             || filter.categoryNames != null || filter.assemblyNames != null;
            return Task.FromResult<object>(new TestRunStatus
            {
                Status = "started",
                Message = hasFilter
                    ? $"{testMode} tests started with a filter applied. Poll the corresponding get_*_test_results tool for the outcome."
                    : $"{testMode} tests started. Poll the corresponding get_*_test_results tool for the outcome."
            });
        }

        /// <summary>
        /// 明示フィルタとonlyFailures/changedFilesOnlyの結果を合成してFilterを組み立てる。
        /// 絞り込みを要求されたのに対象が無い場合は、フルスイートを黙って実行せずskipReasonを返す。
        /// </summary>
        private Filter BuildFilter(RunTestsArgs parameters, out string skipReason)
        {
            skipReason = null;
            var testNames = new HashSet<string>(parameters.TestNames ?? System.Array.Empty<string>());
            var assemblyNames = new HashSet<string>(parameters.AssemblyNames ?? System.Array.Empty<string>());
            var hasExplicitFilter = testNames.Count > 0 || assemblyNames.Count > 0
                                     || (parameters.GroupNames?.Length ?? 0) > 0
                                     || (parameters.CategoryNames?.Length ?? 0) > 0;

            if (parameters.OnlyFailures)
            {
                var rerunNames = TestRunSessionState.GetLastNotPassedTestNames(testMode);
                if (rerunNames.Count == 0 && !hasExplicitFilter)
                {
                    skipReason = $"No failed or skipped tests found in the last completed {testMode} run. Nothing to rerun.";
                    return null;
                }
                testNames.UnionWith(rerunNames);
            }

            if (parameters.ChangedFilesOnly)
            {
                var changedAssemblies = GitChangedAssemblyResolver.Resolve(parameters.GitRef);
                if (changedAssemblies.Count == 0 && !hasExplicitFilter && testNames.Count == 0)
                {
                    skipReason = $"No changed .cs files detected relative to '{parameters.GitRef}'. Nothing to run.";
                    return null;
                }
                assemblyNames.UnionWith(changedAssemblies);
            }

            return new Filter
            {
                testMode = testMode,
                testNames = testNames.Count > 0 ? testNames.ToArray() : null,
                groupNames = NullIfEmpty(parameters.GroupNames),
                categoryNames = NullIfEmpty(parameters.CategoryNames),
                assemblyNames = assemblyNames.Count > 0 ? assemblyNames.ToArray() : null
            };
        }

        private static string[] NullIfEmpty(string[] values)
        {
            return values is { Length: > 0 } ? values : null;
        }

        private void StartRun(Filter filter)
        {
            TestRunSessionState.MarkRunning(testMode);
            TestRunCallbackRegistrar.Api.Execute(new ExecutionSettings
            {
                filters = new[] { filter }
            });
        }

        private static RunTestsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new RunTestsArgs();
            }

            return JsonConvert.DeserializeObject<RunTestsArgs>(args) ?? new RunTestsArgs();
        }
    }

    internal class RunTestsArgs
    {
        [JsonProperty("autoSave")]
        public bool AutoSave { get; set; }

        [JsonProperty("discard")]
        public bool Discard { get; set; }

        [JsonProperty("force")]
        public bool Force { get; set; }

        [JsonProperty("testNames")]
        public string[] TestNames { get; set; }

        [JsonProperty("groupNames")]
        public string[] GroupNames { get; set; }

        [JsonProperty("categoryNames")]
        public string[] CategoryNames { get; set; }

        [JsonProperty("assemblyNames")]
        public string[] AssemblyNames { get; set; }

        [JsonProperty("onlyFailures")]
        public bool OnlyFailures { get; set; }

        [JsonProperty("changedFilesOnly")]
        public bool ChangedFilesOnly { get; set; }

        [JsonProperty("gitRef")]
        public string GitRef { get; set; } = "HEAD";
    }
}
