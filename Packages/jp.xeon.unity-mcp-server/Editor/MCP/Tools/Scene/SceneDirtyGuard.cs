using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SceneRef = UnityEngine.SceneManagement.Scene;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 未保存シーンによるモーダルダイアログを発生させないための共通ガード。
    /// Play入場・テスト実行・シーン切替の前に呼び出し、保存・破棄・エラー返却のいずれかで必ず即応する。
    /// SaveCurrentModifiedScenesIfUserWantsTo のようなプロンプトを伴うAPIは使用しない。
    /// </summary>
    public static class SceneDirtyGuard
    {
        /// <summary>
        /// 未保存の変更を解決する。解決できない場合は例外を投げて操作を中断させる。
        /// </summary>
        /// <param name="operation">呼び出し元の操作名（エラーメッセージ用）</param>
        /// <param name="autoSave">trueなら開いているシーンを自動保存して続行する</param>
        /// <param name="discard">trueなら変更をディスクの内容で明示的に破棄する</param>
        /// <returns>実施した処理の説明。未保存の変更が無かった場合はnull。</returns>
        public static string Resolve(string operation, bool autoSave, bool discard)
        {
            var dirtyScenes = CollectDirtyScenes();
            if (dirtyScenes.Count == 0)
            {
                return null;
            }

            if (autoSave)
            {
                return SaveAll(dirtyScenes);
            }

            if (discard)
            {
                return Discard(dirtyScenes);
            }

            throw new InvalidOperationException(BuildRefusalMessage(operation, dirtyScenes));
        }

        /// <summary>
        /// 未保存の変更を持つシーンを列挙する
        /// </summary>
        public static List<SceneRef> CollectDirtyScenes()
        {
            var result = new List<SceneRef>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    result.Add(scene);
                }
            }

            return result;
        }

        private static string SaveAll(List<SceneRef> dirtyScenes)
        {
            RequireSavablePaths(dirtyScenes);

            if (!EditorSceneManager.SaveOpenScenes())
            {
                throw new InvalidOperationException(
                    "Failed to save the open scenes. Save them explicitly with 'save_scene' and retry.");
            }

            return $"Auto-saved {dirtyScenes.Count} modified scene(s).";
        }

        private static string Discard(List<SceneRef> dirtyScenes)
        {
            if (SceneManager.sceneCount == 1)
            {
                return DiscardSingleScene(SceneManager.GetSceneAt(0));
            }

            RequireSavablePaths(dirtyScenes);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            return $"Discarded changes in {dirtyScenes.Count} scene(s) by reloading them from disk.";
        }

        private static string DiscardSingleScene(SceneRef scene)
        {
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return "Discarded the untitled scene and replaced it with an empty one.";
            }

            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            return $"Discarded changes in '{scene.name}' by reloading it from disk.";
        }

        /// <summary>
        /// 一度も保存されていないシーンは保存先が決められないため、明示的な指定を要求する
        /// </summary>
        private static void RequireSavablePaths(List<SceneRef> dirtyScenes)
        {
            foreach (var scene in dirtyScenes)
            {
                if (string.IsNullOrEmpty(scene.path))
                {
                    throw new InvalidOperationException(
                        "An untitled scene has unsaved changes and has no asset path. " +
                        "Call 'save_scene_as' with a scenePath first.");
                }
            }
        }

        private static string BuildRefusalMessage(
            string operation,
            List<SceneRef> dirtyScenes)
        {
            var names = new List<string>();
            foreach (var scene in dirtyScenes)
            {
                names.Add(string.IsNullOrEmpty(scene.name) ? "(untitled)" : scene.name);
            }

            return $"scene is dirty: {operation} was refused because {dirtyScenes.Count} open scene(s) have " +
                   $"unsaved changes ({string.Join(", ", names)}). " +
                   "Unity would show a modal dialog that MCP cannot dismiss. " +
                   "Hint: call 'save_scene', or retry with autoSave=true to save first, or discard=true to drop the changes.";
        }
    }
}
