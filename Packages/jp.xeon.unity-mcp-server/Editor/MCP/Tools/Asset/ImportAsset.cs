using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// アセットを強制再インポートまたはアセットデータベースを更新するツール
    /// </summary>
    public class ImportAsset : IMcpTool
    {
        public string Name => "import_asset";

        public string Description =>
            "Force re-import an asset or refresh the entire asset database. " +
            "Useful after modifying assets externally or to ensure the asset database is up to date. " +
            "Specify assetPath to reimport a single asset, or set refreshAll to true to refresh everything.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Specific asset path to reimport (e.g. 'Assets/Textures/Icon.png')\"}," +
            "\"refreshAll\":{\"type\":\"boolean\",\"description\":\"If true, refresh the entire asset database instead of a single asset. Default: false\",\"default\":false}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (parameters.RefreshAll)
            {
                return Task.FromResult<object>(RefreshAllAssets());
            }

            if (!string.IsNullOrEmpty(parameters.AssetPath))
            {
                return Task.FromResult<object>(ReimportSingleAsset(parameters.AssetPath));
            }

            throw new InvalidOperationException(
                "Either assetPath or refreshAll must be specified.");
        }

        private static ImportAssetResult RefreshAllAssets()
        {
            AssetDatabase.Refresh();
            return new ImportAssetResult("all", "Asset database refreshed successfully.");
        }

        private static ImportAssetResult ReimportSingleAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Asset not found at path: '{assetPath}'");
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return new ImportAssetResult(assetPath, $"Asset '{assetPath}' reimported successfully.");
        }

        private static ImportAssetArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new ImportAssetArgs();
            }

            return JsonConvert.DeserializeObject<ImportAssetArgs>(args) ?? new ImportAssetArgs();
        }
    }

    internal class ImportAssetArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;

        [JsonProperty("refreshAll")]
        public bool RefreshAll { get; set; }
    }

    internal class ImportAssetResult
    {
        [JsonProperty("target")]
        public string Target { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public ImportAssetResult(string target, string message)
        {
            Target = target;
            Message = message;
        }
    }
}
