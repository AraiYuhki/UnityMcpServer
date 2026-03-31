using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// コンパイルメッセージをメモリキャッシュするクラス。
    /// Domain Reload後も自動再登録されるよう [InitializeOnLoad] を使用する。
    /// </summary>
    [InitializeOnLoad]
    public static class CompilationCache
    {
        private static readonly List<CompileMessage> messages = new();

        static CompilationCache()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        /// <summary>
        /// キャッシュされているコンパイルメッセージを返す
        /// </summary>
        public static IReadOnlyList<CompileMessage> GetMessages()
        {
            return messages;
        }

        /// <summary>
        /// コンパイル開始時にキャッシュをクリアする
        /// </summary>
        private static void OnCompilationStarted(object context)
        {
            messages.Clear();
        }

        /// <summary>
        /// アセンブリコンパイル完了時にメッセージをキャッシュに追加する
        /// </summary>
        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            foreach (var message in compilerMessages)
            {
                messages.Add(CompileMessage.FromCompilerMessage(message));
            }
        }
    }
}
