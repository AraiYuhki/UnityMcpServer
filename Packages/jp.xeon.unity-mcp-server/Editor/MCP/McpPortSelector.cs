using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace UnityMcp
{
    /// <summary>
    /// 他のプロセス（別のUnityエディタ等）が使用中のポートを避けて、ランダムなポートを選択するクラス。
    /// </summary>
    public static class McpPortSelector
    {
        /// <summary>OSのエフェメラルポート帯（49152以降）を避けた選択範囲</summary>
        public const int MinPort = 10000;
        public const int MaxPort = 49151;

        private const int MaxAttempts = 100;

        /// <summary>
        /// 使用されていないポートをランダムに選択する。
        /// </summary>
        public static bool TryPickAvailablePort(out int port)
        {
            var used = CollectUsedPorts();
            return TryPickPort(new Random(), candidate => !used.Contains(candidate) && CanBind(candidate), out port);
        }

        /// <summary>
        /// 範囲内からランダムに候補を選び、条件を満たす最初のポートを返す。
        /// </summary>
        public static bool TryPickPort(Random random, Func<int, bool> isAvailable, out int port)
        {
            for (var i = 0; i < MaxAttempts; i++)
            {
                var candidate = random.Next(MinPort, MaxPort + 1);
                if (!isAvailable(candidate))
                {
                    continue;
                }

                port = candidate;
                return true;
            }

            port = 0;
            return false;
        }

        private static HashSet<int> CollectUsedPorts()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var used = new HashSet<int>(properties.GetActiveTcpListeners().Select(endPoint => endPoint.Port));
            used.UnionWith(properties.GetActiveTcpConnections().Select(connection => connection.LocalEndPoint.Port));
            return used;
        }

        /// <summary>
        /// 実際にHttpListenerで待ち受けを試し、bind可能かを確認する。
        /// </summary>
        private static bool CanBind(int port)
        {
            var probe = new HttpListener();
            try
            {
                probe.Prefixes.Add($"http://localhost:{port}/mcp/");
                probe.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                probe.Close();
            }
        }
    }
}
