using System;
using UnityEditor;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// スクリプトの追加・変更・削除を検知して CompilationCache を保留状態へ落とすポストプロセッサ。
    /// AssetDatabase.Refresh() から呼ばれるため、リフレッシュ直後に get_compile_errors を叩いても
    /// 前回結果が確定済みとして返らない（stale判定される）。
    /// </summary>
    public class ScriptChangeWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ContainsScript(importedAssets) &&
                !ContainsScript(deletedAssets) &&
                !ContainsScript(movedAssets))
            {
                return;
            }

            CompilationCache.MarkPending();
        }

        private static bool ContainsScript(string[] paths)
        {
            if (paths == null)
            {
                return false;
            }

            foreach (var path in paths)
            {
                if (IsCompilationInput(path))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCompilationInput(string path)
        {
            return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase);
        }
    }
}
