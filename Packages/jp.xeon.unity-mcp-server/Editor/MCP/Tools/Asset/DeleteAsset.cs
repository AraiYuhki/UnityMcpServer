using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// アセットファイルを削除するツール（OSのゴミ箱に移動）
    /// </summary>
    public class DeleteAsset : IMcpTool
    {
        public string Name => "delete_asset";

        public string Description =>
            "Delete an asset file from the Unity project. " +
            "The asset is moved to the OS trash for safety, so it can be recovered if needed. " +
            "Use get_asset_list to find asset paths.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Path to the asset to delete (e.g. 'Assets/Data/MyData.asset')\"}" +
            "},\"required\":[\"assetPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (string.IsNullOrEmpty(parameters.AssetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(parameters.AssetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Asset not found at path: '{parameters.AssetPath}'");
            }

            var success = AssetDatabase.MoveAssetToTrash(parameters.AssetPath);
            var result = new DeleteAssetResult(parameters.AssetPath, success);
            return Task.FromResult<object>(result);
        }

        private static DeleteAssetArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new DeleteAssetArgs();
            }

            return JsonConvert.DeserializeObject<DeleteAssetArgs>(args) ?? new DeleteAssetArgs();
        }
    }

    internal class DeleteAssetArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;
    }

    internal class DeleteAssetResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; private set; }

        [JsonProperty("success")]
        public bool Success { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public DeleteAssetResult(string assetPath, bool success)
        {
            AssetPath = assetPath;
            Success = success;
            Message = success
                ? $"Asset '{assetPath}' moved to trash."
                : $"Failed to delete asset '{assetPath}'.";
        }
    }
}
