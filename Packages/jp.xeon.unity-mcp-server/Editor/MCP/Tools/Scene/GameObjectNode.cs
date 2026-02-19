using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン階層ツリーの1ノード。GameObjectの情報と子ノードを保持する。
    /// </summary>
    public class GameObjectNode
    {
        /// <summary>GameObjectの名前</summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>シーンルートからの階層パス（スラッシュ区切り）</summary>
        [JsonProperty("path")]
        public string Path { get; private set; }

        /// <summary>自身のアクティブ状態（activeSelf）</summary>
        [JsonProperty("active")]
        public bool Active { get; private set; }

        /// <summary>タグ</summary>
        [JsonProperty("tag")]
        public string Tag { get; private set; }

        /// <summary>レイヤー名</summary>
        [JsonProperty("layer")]
        public string Layer { get; private set; }

        /// <summary>アタッチされているコンポーネントの型名一覧（includeComponents が true の場合のみ）</summary>
        [JsonProperty("components")]
        public List<string> Components { get; private set; }

        /// <summary>子ノード一覧</summary>
        [JsonProperty("children")]
        public List<GameObjectNode> Children { get; private set; } = new();

        /// <summary>
        /// GameObjectからノードを再帰的に構築する
        /// </summary>
        /// <param name="go">対象GameOobject</param>
        /// <param name="parentPath">親までの階層パス</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="maxDepth">最大深さ（0で無制限）</param>
        /// <param name="includeComponents">コンポーネント名を含めるか</param>
        /// <param name="includeInactive">非アクティブを含めるか</param>
        public static GameObjectNode Build(
            GameObject go,
            string parentPath,
            int currentDepth,
            int maxDepth,
            bool includeComponents,
            bool includeInactive)
        {
            var path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
            var node = new GameObjectNode
            {
                Name = go.name,
                Path = path,
                Active = go.activeSelf,
                Tag = go.tag,
                Layer = LayerMask.LayerToName(go.layer),
                Components = includeComponents ? CollectComponentNames(go) : new List<string>()
            };

            var shouldRecurse = maxDepth == 0 || currentDepth < maxDepth;
            if (!shouldRecurse)
            {
                return node;
            }

            foreach (Transform child in go.transform)
            {
                if (!includeInactive && !child.gameObject.activeSelf)
                {
                    continue;
                }

                node.Children.Add(Build(child.gameObject, path, currentDepth + 1, maxDepth, includeComponents, includeInactive));
            }

            return node;
        }

        private static List<string> CollectComponentNames(GameObject go)
        {
            var names = new List<string>();
            foreach (var component in go.GetComponents<Component>())
            {
                if (component != null)
                {
                    names.Add(component.GetType().Name);
                }
            }

            return names;
        }
    }
}
