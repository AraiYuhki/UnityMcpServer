using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// Missing Referenceが検出された個別の箇所を表すモデル
    /// </summary>
    public class MissingReferenceItem
    {
        /// <summary>シーンルートからの階層パス</summary>
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; private set; }

        /// <summary>コンポーネントの型名。Missingスクリプトの場合は "MissingScript"</summary>
        [JsonProperty("componentType")]
        public string ComponentType { get; private set; }

        /// <summary>Missing参照が検出されたフィールド名。Missingスクリプト自体の場合は空文字</summary>
        [JsonProperty("fieldName")]
        public string FieldName { get; private set; }

        public static MissingReferenceItem ForMissingScript(string gameObjectPath)
        {
            return new MissingReferenceItem
            {
                GameObjectPath = gameObjectPath,
                ComponentType = "MissingScript",
                FieldName = string.Empty
            };
        }

        public static MissingReferenceItem ForMissingField(string gameObjectPath, Component component, string fieldName)
        {
            return new MissingReferenceItem
            {
                GameObjectPath = gameObjectPath,
                ComponentType = component.GetType().FullName,
                FieldName = fieldName
            };
        }
    }
}
