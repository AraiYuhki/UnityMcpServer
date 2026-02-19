using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// 指定パス以下のアセット一覧を返すツール
    /// </summary>
    public class GetAssetList : IMcpTool
    {
        public string Name => "get_asset_list";

        public string Description =>
            "Get a list of assets in the project under a specified path. " +
            "Supports AssetDatabase filter syntax (e.g. 't:Prefab', 't:Script name:Player', 'l:MyLabel'). " +
            "Returns asset paths, names, and types.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"path\":{\"type\":\"string\",\"description\":\"Root path to search under (e.g. 'Assets/Scripts'). Default: 'Assets'\",\"default\":\"Assets\"}," +
            "\"filter\":{\"type\":\"string\",\"description\":\"AssetDatabase.FindAssets filter string (e.g. 't:Prefab', 't:Script name:Player'). Default: empty (all assets)\",\"default\":\"\"}," +
            "\"maxCount\":{\"type\":\"integer\",\"description\":\"Maximum number of assets to return (default: 100)\",\"default\":100}" +
            "},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var searchPath = string.IsNullOrEmpty(parameters.Path) ? "Assets" : parameters.Path;
            var filter = parameters.Filter ?? string.Empty;

            var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });
            var totalCount = guids.Length;

            var assets = BuildAssetList(guids, parameters.MaxCount);
            return Task.FromResult<object>(new AssetListResult(totalCount, assets));
        }

        private static GetAssetListArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetAssetListArgs();
            }

            return JsonConvert.DeserializeObject<GetAssetListArgs>(args) ?? new GetAssetListArgs();
        }

        private static List<AssetItem> BuildAssetList(string[] guids, int maxCount)
        {
            var assets = new List<AssetItem>();
            var limit = System.Math.Min(guids.Length, maxCount);

            for (var i = 0; i < limit; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                var typeName = assetType != null ? assetType.FullName : "Unknown";
                assets.Add(AssetItem.Create(path, typeName));
            }

            return assets;
        }
    }

    internal class GetAssetListArgs
    {
        [JsonProperty("path")]
        public string Path { get; set; } = "Assets";

        [JsonProperty("filter")]
        public string Filter { get; set; } = string.Empty;

        [JsonProperty("maxCount")]
        public int MaxCount { get; set; } = 100;
    }
}
