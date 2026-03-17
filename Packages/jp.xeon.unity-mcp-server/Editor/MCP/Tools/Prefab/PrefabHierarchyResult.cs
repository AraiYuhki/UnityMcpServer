using Newtonsoft.Json;
using UnityMcp.Tools.Scene;

namespace UnityMcp.Tools.Prefab
{
    /// <summary>
    /// get_prefab_hierarchy ツールの戻り値モデル
    /// </summary>
    public class PrefabHierarchyResult
    {
        /// <summary>プレハブアセットのパス</summary>
        [JsonProperty("assetPath")]
        public string AssetPath { get; private set; }

        /// <summary>プレハブ内の全GameObjectの総数</summary>
        [JsonProperty("gameObjectCount")]
        public int GameObjectCount { get; private set; }

        /// <summary>ルートGameObjectから始まる階層ツリー</summary>
        [JsonProperty("hierarchy")]
        public GameObjectNode Hierarchy { get; private set; }

        public PrefabHierarchyResult(string assetPath, int gameObjectCount, GameObjectNode hierarchy)
        {
            AssetPath = assetPath;
            GameObjectCount = gameObjectCount;
            Hierarchy = hierarchy;
        }
    }
}
