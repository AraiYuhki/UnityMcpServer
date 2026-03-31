using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// get_asset_list ツールの戻り値モデル
    /// </summary>
    public class AssetListResult
    {
        /// <summary>フィルタにヒットしたアセットの総数（maxCount適用前）</summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; private set; }

        /// <summary>返却されたアセット一覧</summary>
        [JsonProperty("assets")]
        public List<AssetItem> Assets { get; private set; }

        public AssetListResult(int totalCount, List<AssetItem> assets)
        {
            TotalCount = totalCount;
            Assets = assets;
        }
    }
}
