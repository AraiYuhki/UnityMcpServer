using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// C#スクリプトやテキストアセットの内容を読み取るツール
    /// </summary>
    public class ReadScript : IMcpTool
    {
        public string Name => "read_script";

        public string Description =>
            "Read the content of a C# script file or any text asset in the Unity project. " +
            "Returns the full file content. " +
            "Use get_asset_list with filter 't:Script' to find available scripts.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path to the script file (e.g. 'Assets/Scripts/Player/PlayerController.cs')\"}" +
            "},\"required\":[\"assetPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var fullPath = ResolveFullPath(parameters.AssetPath);

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    $"File not found: '{parameters.AssetPath}' (resolved to '{fullPath}')");
            }

            var content = File.ReadAllText(fullPath);
            var fileName = Path.GetFileName(parameters.AssetPath);
            var lineCount = CountLines(content);

            var result = new ReadScriptResult(parameters.AssetPath, fileName, lineCount, content);
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(ReadScriptArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.AssetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            if (!parameters.AssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !parameters.AssetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"assetPath must start with 'Assets/' or 'Packages/': '{parameters.AssetPath}'");
            }
        }

        private static string ResolveFullPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, "..", assetPath);
        }

        private static int CountLines(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            var lineCount = 1;
            foreach (var c in content)
            {
                if (c == '\n')
                {
                    lineCount++;
                }
            }

            return lineCount;
        }

        private static ReadScriptArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new ReadScriptArgs();
            }

            return JsonConvert.DeserializeObject<ReadScriptArgs>(args) ?? new ReadScriptArgs();
        }
    }

    internal class ReadScriptArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;
    }

    internal class ReadScriptResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; private set; }

        [JsonProperty("fileName")]
        public string FileName { get; private set; }

        [JsonProperty("lineCount")]
        public int LineCount { get; private set; }

        [JsonProperty("content")]
        public string Content { get; private set; }

        public ReadScriptResult(string assetPath, string fileName, int lineCount, string content)
        {
            AssetPath = assetPath;
            FileName = fileName;
            LineCount = lineCount;
            Content = content;
        }
    }
}
