using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// get_component_info ツールの戻り値モデル
    /// </summary>
    public class ComponentInfoResult
    {
        /// <summary>GameObjectの名前</summary>
        [JsonProperty("gameObjectName")]
        public string GameObjectName { get; private set; }

        /// <summary>シーンルートからの階層パス</summary>
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; private set; }

        /// <summary>自身のアクティブ状態（activeSelf）</summary>
        [JsonProperty("active")]
        public bool Active { get; private set; }

        /// <summary>コンポーネントの詳細一覧</summary>
        [JsonProperty("components")]
        public List<ComponentDetail> Components { get; private set; }

        public static ComponentInfoResult Create(GameObject go, string path, List<ComponentDetail> components)
        {
            return new ComponentInfoResult
            {
                GameObjectName = go.name,
                GameObjectPath = path,
                Active = go.activeSelf,
                Components = components
            };
        }
    }
}
