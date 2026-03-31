using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// 現在のシーンのNavMeshをベイクするツール
    /// </summary>
    public class BakeNavMesh : IMcpTool
    {
        public string Name => "bake_navmesh";

        public string Description =>
            "Bake the NavMesh for the current scene using default NavMesh settings. " +
            "This is a synchronous operation that may take time for large scenes.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{},\"required\":[]}";

        public Task<object> Execute(string args)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var sceneName = scene.name;

            try
            {
                NavMeshBuilder.BuildNavMesh();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to bake NavMesh for scene '{sceneName}'. " +
                    "Ensure the scene contains valid NavMesh surfaces and geometry. " +
                    $"Details: {ex.Message}");
            }

            EditorSceneManager.MarkSceneDirty(scene);

            var result = new BakeNavMeshResult
            {
                SceneName = sceneName,
                Message = $"NavMesh baked successfully for scene '{sceneName}'."
            };
            return Task.FromResult<object>(result);
        }
    }

    internal class BakeNavMeshResult
    {
        [JsonProperty("sceneName")]
        public string SceneName { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
