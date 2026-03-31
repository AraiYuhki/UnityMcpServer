using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// find_missing_references ツールの戻り値モデル
    /// </summary>
    public class MissingReferenceResult
    {
        /// <summary>Missing Referenceが1件以上存在するか</summary>
        [JsonProperty("hasMissingReferences")]
        public bool HasMissingReferences { get; private set; }

        /// <summary>検出されたMissing Referenceの総数</summary>
        [JsonProperty("totalCount")]
        public int TotalCount { get; private set; }

        /// <summary>スキャン対象の説明（"CurrentScene" または アセットパス）</summary>
        [JsonProperty("scannedTarget")]
        public string ScannedTarget { get; private set; }

        /// <summary>検出されたMissing Referenceの一覧</summary>
        [JsonProperty("items")]
        public List<MissingReferenceItem> Items { get; private set; }

        public MissingReferenceResult(string scannedTarget, List<MissingReferenceItem> items)
        {
            ScannedTarget = scannedTarget;
            Items = items;
            TotalCount = items.Count;
            HasMissingReferences = items.Count > 0;
        }
    }
}
