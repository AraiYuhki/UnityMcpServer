using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// 使用中のポートと被らないポートをランダムに選び、設定へ保存してサーバーを再起動する。
    /// 複数のUnityエディタを同時に開いた際のポート競合を避ける用途を想定している。
    /// </summary>
    public static class McpPortRandomizer
    {
        private const string SettingAssetPath = "Assets/MCPServerSetting.asset";
        private const string ClientConfigFileName = ".mcp.json";
        private const string ClientServerName = "unity-mcp";

        [MenuItem("Tools/Randomize MCP Server Port")]
        public static void RandomizeAndRestart()
        {
            if (!McpPortSelector.TryPickAvailablePort(out var port))
            {
                Debug.LogError("[MCP] No available port was found. The port was not changed.");
                return;
            }

            var setting = FindOrCreateSetting();
            setting.SetPort(port);
            AssetDatabase.SaveAssets();

            McpServer.Restart();
            Debug.Log($"[MCP] Port changed to {port}. {UpdateClientConfig(port)}");
        }

        private static McpServerSetting FindOrCreateSetting()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(McpServerSetting)}");
            if (guids is { Length: > 0 })
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<McpServerSetting>(path);
            }

            var created = ScriptableObject.CreateInstance<McpServerSetting>();
            AssetDatabase.CreateAsset(created, SettingAssetPath);
            return created;
        }

        /// <summary>
        /// プロジェクト直下の .mcp.json に unity-mcp のエントリがあれば、URLのポートを新しい値へ更新する。
        /// クライアントが古いポートへ接続し続けないようにするための処理。
        /// </summary>
        private static string UpdateClientConfig(int port)
        {
            var configPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ClientConfigFileName);
            if (!File.Exists(configPath))
            {
                return $"{ClientConfigFileName} was not found; point your MCP client to http://localhost:{port}/mcp/.";
            }

            var root = JObject.Parse(File.ReadAllText(configPath));
            var server = root["mcpServers"]?[ClientServerName];
            if (server == null)
            {
                return $"{ClientConfigFileName} has no '{ClientServerName}' entry; point your MCP client to http://localhost:{port}/mcp/.";
            }

            server["url"] = $"http://localhost:{port}/mcp/";
            File.WriteAllText(configPath, root.ToString());
            return $"Updated {ClientConfigFileName}; reconnect your MCP client (e.g. /mcp).";
        }
    }
}
