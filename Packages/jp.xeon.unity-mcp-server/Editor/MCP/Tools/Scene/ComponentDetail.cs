using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// コンポーネント1つ分の詳細情報（型名 + フィールド値一覧）
    /// </summary>
    public class ComponentDetail
    {
        /// <summary>コンポーネントの完全修飾型名</summary>
        [JsonProperty("type")]
        public string Type { get; private set; }

        /// <summary>
        /// シリアライズ可能なフィールドの値一覧。
        /// キー: SerializedProperty の propertyPath、値: フィールドの値またはサマリー文字列
        /// </summary>
        [JsonProperty("fields")]
        public Dictionary<string, object> Fields { get; private set; }

        public static ComponentDetail Create(Component component, Dictionary<string, object> fields)
        {
            return new ComponentDetail
            {
                Type = component.GetType().FullName,
                Fields = fields
            };
        }
    }
}
