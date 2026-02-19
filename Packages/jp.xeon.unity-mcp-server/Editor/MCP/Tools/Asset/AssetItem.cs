using Newtonsoft.Json;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// get_asset_list の個別アセットモデル
    /// </summary>
    public class AssetItem
    {
        /// <summary>Assetsフォルダからの相対パス</summary>
        [JsonProperty("path")]
        public string Path { get; private set; }

        /// <summary>アセットのファイル名（拡張子なし）</summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>アセットの主要型名</summary>
        [JsonProperty("type")]
        public string Type { get; private set; }

        public static AssetItem Create(string path, string typeName)
        {
            return new AssetItem
            {
                Path = path,
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                Type = typeName
            };
        }
    }
}
