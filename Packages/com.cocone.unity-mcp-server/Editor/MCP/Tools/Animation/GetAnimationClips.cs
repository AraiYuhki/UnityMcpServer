using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Tools.Animation
{
    /// <summary>
    /// AnimatorControllerまたはGameObjectに含まれるアニメーションクリップ一覧を返すツール
    /// </summary>
    public class GetAnimationClips : IMcpTool
    {
        public string Name => "get_animation_clips";

        public string Description =>
            "Get all animation clips in an Animator Controller or on a specific GameObject. " +
            "Returns clip names, lengths, loop settings, and event counts. " +
            "Specify by asset path (.controller or .anim) or by scene GameObject path with an Animator component. " +
            "Use get_scene_hierarchy or get_asset_list to find the correct path first.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path to a GameObject with an Animator component (e.g. 'Character/Model')\"}," +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Direct asset path to a .controller or .anim file (e.g. 'Assets/Animations/Run.anim')\"}" +
            "}}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var clips = ResolveClips(parameters);
            var clipInfos = CollectClipInfos(clips);

            var result = new GetAnimationClipsResult
            {
                Clips = clipInfos
            };

            return Task.FromResult<object>(result);
        }

        private static GetAnimationClipsArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetAnimationClipsArgs();
            }

            return JsonConvert.DeserializeObject<GetAnimationClipsArgs>(args)
                   ?? new GetAnimationClipsArgs();
        }

        private static AnimationClip[] ResolveClips(GetAnimationClipsArgs parameters)
        {
            if (!string.IsNullOrEmpty(parameters.AssetPath))
            {
                return LoadClipsFromAsset(parameters.AssetPath);
            }

            if (!string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                return LoadClipsFromGameObject(parameters.GameObjectPath);
            }

            throw new InvalidOperationException(
                "At least one of 'assetPath' or 'gameObjectPath' is required.");
        }

        private static AnimationClip[] LoadClipsFromAsset(string assetPath)
        {
            if (assetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                return LoadSingleClip(assetPath);
            }

            if (assetPath.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
            {
                return LoadClipsFromController(assetPath);
            }

            throw new InvalidOperationException(
                $"Unsupported asset type. Expected .anim or .controller file: '{assetPath}'");
        }

        private static AnimationClip[] LoadSingleClip(string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"AnimationClip not found at path: '{assetPath}'");
            }

            return new[] { clip };
        }

        private static AnimationClip[] LoadClipsFromController(string assetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"AnimatorController not found at path: '{assetPath}'");
            }

            return controller.animationClips;
        }

        private static AnimationClip[] LoadClipsFromGameObject(string gameObjectPath)
        {
            var go = FindGameObject(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException(
                    $"GameObject not found: '{gameObjectPath}'");
            }

            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(
                    $"No Animator component found on '{gameObjectPath}'");
            }

            if (animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    $"No AnimatorController assigned to Animator on '{gameObjectPath}'");
            }

            return animator.runtimeAnimatorController.animationClips;
        }

        private static List<AnimationClipInfo> CollectClipInfos(AnimationClip[] clips)
        {
            var result = new List<AnimationClipInfo>(clips.Length);

            foreach (var clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                var info = CreateClipInfo(clip);
                result.Add(info);
            }

            return result;
        }

        private static AnimationClipInfo CreateClipInfo(AnimationClip clip)
        {
            return new AnimationClipInfo
            {
                Name = clip.name,
                Length = clip.length,
                IsLooping = clip.isLooping,
                FrameRate = clip.frameRate,
                EventCount = clip.events.Length,
                WrapMode = clip.wrapMode.ToString()
            };
        }

        private static GameObject FindGameObject(string path)
        {
            var parts = path.Split('/');
            var scene = EditorSceneManager.GetActiveScene();

            GameObject root = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != parts[0])
                {
                    continue;
                }

                root = go;
                break;
            }

            if (root == null)
            {
                return null;
            }

            if (parts.Length == 1)
            {
                return root;
            }

            var remaining = string.Join("/", parts, 1, parts.Length - 1);
            var child = root.transform.Find(remaining);
            return child != null ? child.gameObject : null;
        }
    }

    internal class GetAnimationClipsArgs
    {
        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;
    }

    internal class AnimationClipInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("length")]
        public float Length { get; set; }

        [JsonProperty("isLooping")]
        public bool IsLooping { get; set; }

        [JsonProperty("frameRate")]
        public float FrameRate { get; set; }

        [JsonProperty("eventCount")]
        public int EventCount { get; set; }

        [JsonProperty("wrapMode")]
        public string WrapMode { get; set; }
    }

    internal class GetAnimationClipsResult
    {
        [JsonProperty("clips")]
        public List<AnimationClipInfo> Clips { get; set; }
    }
}
