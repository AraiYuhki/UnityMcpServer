using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// アセットファイルを移動またはリネームするツール
    /// </summary>
    public class MoveAsset : IMcpTool
    {
        public string Name => "move_asset";

        public string Description =>
            "Move or rename an asset file in the Unity project. " +
            "Can be used for both moving to a different folder and renaming. " +
            "Use get_asset_list to find asset paths.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"sourcePath\":{\"type\":\"string\",\"description\":\"Current asset path (e.g. 'Assets/Data/OldName.asset')\"}," +
            "\"destinationPath\":{\"type\":\"string\",\"description\":\"New asset path (e.g. 'Assets/NewFolder/NewName.asset')\"}" +
            "},\"required\":[\"sourcePath\",\"destinationPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var sourceAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(parameters.SourcePath);
            if (sourceAsset == null)
            {
                throw new InvalidOperationException(
                    $"Source asset not found: '{parameters.SourcePath}'");
            }

            EnsureDirectoryExists(parameters.DestinationPath);

            var errorMessage = AssetDatabase.MoveAsset(parameters.SourcePath, parameters.DestinationPath);
            var success = string.IsNullOrEmpty(errorMessage);
            var result = new MoveAssetResult(parameters.SourcePath, parameters.DestinationPath, success, errorMessage);
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(MoveAssetArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.SourcePath))
            {
                throw new InvalidOperationException("sourcePath is required.");
            }

            if (string.IsNullOrEmpty(parameters.DestinationPath))
            {
                throw new InvalidOperationException("destinationPath is required.");
            }
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static MoveAssetArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new MoveAssetArgs();
            }

            return JsonConvert.DeserializeObject<MoveAssetArgs>(args) ?? new MoveAssetArgs();
        }
    }

    internal class MoveAssetArgs
    {
        [JsonProperty("sourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [JsonProperty("destinationPath")]
        public string DestinationPath { get; set; } = string.Empty;
    }

    internal class MoveAssetResult
    {
        [JsonProperty("sourcePath")]
        public string SourcePath { get; private set; }

        [JsonProperty("destinationPath")]
        public string DestinationPath { get; private set; }

        [JsonProperty("success")]
        public bool Success { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public MoveAssetResult(string sourcePath, string destinationPath, bool success, string errorMessage)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            Success = success;
            Message = success
                ? $"Asset moved from '{sourcePath}' to '{destinationPath}'."
                : $"Failed to move asset: {errorMessage}";
        }
    }
}
