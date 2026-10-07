using UnityEditor;

namespace Xeon.Ci
{
    /// <summary>
    /// 自動アップデートパイプライン専用のコンパイル確認エントリポイント。
    /// -executeMethod でこのメソッドが呼べた時点でスクリプトのコンパイルは成功している。
    /// </summary>
    public static class CompileCheck
    {
        public static void Run()
        {
            EditorApplication.Exit(0);
        }
    }
}
