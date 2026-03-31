using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// MCPサーバーの設定を保持するScriptableObject
    /// Assets/Create/MCPServerSettingから作成可能
    /// </summary>
    [CreateAssetMenu(fileName = "MCPServerSetting", menuName = "MCPServerSetting")]
    public class McpServerSetting : ScriptableObject
    {
        /// <summary>
        /// サーバーが待ち受けるポート番号
        /// </summary>
        [SerializeField] private int port = 7000;

        public int Port => port;
    }
}