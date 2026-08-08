#if MCP_UGUI
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UnityMcp.Tools.InputSimulation
{
    /// <summary>
    /// simulate_ui_* ツールで共有するuGUI操作の補助処理。
    /// </summary>
    internal static class UiSimulationUtility
    {
        /// <summary>
        /// シーンルートからの階層パスでGameObjectを探す。見つからない場合は例外を投げる。
        /// </summary>
        public static GameObject RequireGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required.");
            }

            var target = FindGameObject(path);
            if (target == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{path}'");
            }

            return target;
        }

        /// <summary>
        /// 対象のスクリーン座標を推定する。RectTransformでなければゼロを返す。
        /// </summary>
        public static Vector2 ResolveScreenPosition(GameObject target)
        {
            var rectTransform = target.transform as RectTransform;
            return rectTransform != null ? (Vector2)rectTransform.position : Vector2.zero;
        }

        /// <summary>
        /// ポインタイベントデータを組み立てる
        /// </summary>
        public static PointerEventData BuildPointerData(GameObject target, string button, Vector2 position)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = ParseButton(button),
                position = position,
                pressPosition = position,
                pointerPress = target,
                pointerCurrentRaycast = new RaycastResult { gameObject = target },
                pointerPressRaycast = new RaycastResult { gameObject = target }
            };
        }

        public static PointerEventData.InputButton ParseButton(string button)
        {
            switch (button)
            {
                case "left":
                    return PointerEventData.InputButton.Left;
                case "right":
                    return PointerEventData.InputButton.Right;
                case "middle":
                    return PointerEventData.InputButton.Middle;
                default:
                    throw new InvalidOperationException(
                        $"Invalid button: '{button}'. Use 'left', 'right', or 'middle'.");
            }
        }

        private static GameObject FindGameObject(string path)
        {
            var parts = path.Split('/');
            var scene = SceneManager.GetActiveScene();

            var root = FindRoot(scene, parts[0]);
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

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == name)
                {
                    return go;
                }
            }

            return null;
        }
    }
}
#endif
