using System;

namespace UnityMcp.Tools.Compile
{
    /// <summary>
    /// 古いアセンブリのまま実行してしまう事故を防ぐための共通ゲート。
    /// テスト実行やPlay入場の前に呼び出し、コンパイルが未確定またはエラー保持中なら操作を拒否する。
    /// </summary>
    public static class CompileGate
    {
        /// <summary>
        /// コンパイルが確定していてエラーが無いことを確認する。満たさない場合は例外を投げる。
        /// </summary>
        /// <param name="operation">呼び出し元の操作名（エラーメッセージ用）</param>
        /// <param name="force">trueならゲートを無効化して続行する</param>
        public static void Ensure(string operation, bool force)
        {
            if (force)
            {
                return;
            }

            RequireSettled(operation);
            RequireNoErrors(operation);
        }

        private static void RequireSettled(string operation)
        {
            var state = CompilationCache.State;
            if (!state.IsStale())
            {
                return;
            }

            throw new InvalidOperationException(
                $"{operation} was refused because compilation is '{state.ToWireString()}'. " +
                "Running now would use the previous assemblies. " +
                "Call 'compile_and_wait' first, or retry with force=true to bypass this gate.");
        }

        private static void RequireNoErrors(string operation)
        {
            if (!HasErrors())
            {
                return;
            }

            throw new InvalidOperationException(
                $"{operation} was refused because the project has compile errors. " +
                "Fix them and call 'compile_and_wait' to confirm, or retry with force=true to bypass this gate.");
        }

        private static bool HasErrors()
        {
            if (CompilationCache.CompilationFailed)
            {
                return true;
            }

            foreach (var message in CompilationCache.GetMessages())
            {
                if (message.Type == "Error")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
