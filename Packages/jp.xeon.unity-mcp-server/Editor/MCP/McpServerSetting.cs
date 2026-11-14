using UnityEditor;
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

        /// <summary>
        /// ポート番号を変更する。アセットの保存は呼び出し側で行う。
        /// </summary>
        public void SetPort(int value)
        {
            port = value;
            EditorUtility.SetDirty(this);
        }
    }
}