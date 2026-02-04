using System;

namespace UnityMcp
{
    /// <summary>
    /// MCPリクエストのデータクラス
    /// クライアントからサーバーへ送信されるJSON形式のリクエストを表す
    /// </summary>
    [Serializable]
    public class McpRequest
    {
        /// <summary>
        /// 実行するツールの名前
        /// </summary>
        public string tool;

        /// <summary>
        /// ツールに渡す引数（JSON文字列）
        /// JsonUtilityではobject型を扱えないためstring型で受け取る
        /// </summary>
        public string arguments;
    }

    /// <summary>
    /// MCPレスポンスのデータクラス
    /// サーバーからクライアントへ返されるJSON形式のレスポンスを表す
    /// </summary>
    [Serializable]
    public class McpResponse
    {
        /// <summary>
        /// 処理が成功したかどうか
        /// </summary>
        public bool ok;

        /// <summary>
        /// ツールの実行結果（JSON文字列）
        /// JsonUtilityではobject型を扱えないためstring型で返す
        /// </summary>
        public string result;

        /// <summary>
        /// エラーメッセージ（失敗時のみ設定）
        /// </summary>
        public string error;
    }
}
