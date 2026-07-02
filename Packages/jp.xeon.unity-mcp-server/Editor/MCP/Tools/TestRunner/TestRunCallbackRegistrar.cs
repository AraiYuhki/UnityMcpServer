using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// TestCallbacksをTestRunnerApiへ登録する。
    /// [InitializeOnLoad]によりドメインリロード後も毎回再登録されるため、
    /// PlayModeテストでリロードが挟まってもRunFinishedの受信を継続できる。
    /// </summary>
    [InitializeOnLoad]
    internal static class TestRunCallbackRegistrar
    {
        private static readonly TestRunnerApi api;

        public static TestRunnerApi Api => api;

        static TestRunCallbackRegistrar()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestCallbacks());
        }
    }
}
