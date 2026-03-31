using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// get_scene_hierarchy ツールの戻り値モデル
    /// </summary>
    public class SceneHierarchyResult
    {
        /// <summary>シーン名</summary>
        [JsonProperty("sceneName")]
        public string SceneName { get; private set; }

        /// <summary>シーンファイルのパス</summary>
        [JsonProperty("scenePath")]
        public string ScenePath { get; private set; }

        /// <summary>シーン内の全GameObjectの総数</summary>
        [JsonProperty("gameObjectCount")]
        public int GameObjectCount { get; private set; }

        /// <summary>ルートGameObjectから始まる階層ツリー</summary>
        [JsonProperty("hierarchy")]
        public List<GameObjectNode> Hierarchy { get; private set; }

        public SceneHierarchyResult(string sceneName, string scenePath, int gameObjectCount, List<GameObjectNode> hierarchy)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            GameObjectCount = gameObjectCount;
            Hierarchy = hierarchy;
        }
    }
}
