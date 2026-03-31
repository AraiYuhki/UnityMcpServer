using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp.Tools.Scene
{
    /// <summary>
    /// シーン上でPhysics Raycastを実行するツール
    /// </summary>
    public class Raycast : IMcpTool
    {
        public string Name => "raycast";

        public string Description =>
            "Perform a physics raycast in the scene. " +
            "Specify origin and direction as Vector3, with optional max distance and layer mask. " +
            "Returns hit information including point, normal, distance, and the hit GameObject.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"origin\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"},\"z\":{\"type\":\"number\"}},\"required\":[\"x\",\"y\",\"z\"],\"description\":\"Ray origin position as {x, y, z}\"}," +
            "\"direction\":{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"},\"z\":{\"type\":\"number\"}},\"required\":[\"x\",\"y\",\"z\"],\"description\":\"Ray direction as {x, y, z}\"}," +
            "\"maxDistance\":{\"type\":\"number\",\"description\":\"Maximum raycast distance (default: 1000)\"}," +
            "\"layerMask\":{\"type\":\"integer\",\"description\":\"Layer mask for filtering (default: -1 for all layers)\"}" +
            "},\"required\":[\"origin\",\"direction\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);

            if (parameters.Origin == null)
            {
                throw new InvalidOperationException("origin is required.");
            }

            if (parameters.Direction == null)
            {
                throw new InvalidOperationException("direction is required.");
            }

            var origin = ParseVector3(parameters.Origin, "origin");
            var direction = ParseVector3(parameters.Direction, "direction");
            var maxDistance = parameters.MaxDistance;
            var layerMask = parameters.LayerMask;

            if (Physics.Raycast(origin, direction, out var hit, maxDistance, layerMask))
            {
                var result = BuildHitResult(hit);
                return Task.FromResult<object>(result);
            }

            var noHitResult = new RaycastResult
            {
                HasHit = false,
                Message = "Raycast did not hit any object."
            };
            return Task.FromResult<object>(noHitResult);
        }

        private static RaycastResult BuildHitResult(RaycastHit hit)
        {
            return new RaycastResult
            {
                HasHit = true,
                Point = new Vector3Result(hit.point),
                Normal = new Vector3Result(hit.normal),
                Distance = hit.distance,
                GameObjectName = hit.collider.gameObject.name,
                GameObjectPath = GetHierarchyPath(hit.collider.transform),
                Message = $"Raycast hit '{hit.collider.gameObject.name}' at distance {hit.distance:F3}."
            };
        }

        private static Vector3 ParseVector3(JObject obj, string parameterName)
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"{parameterName} must be a valid Vector3 object with x, y, z.");
            }

            var x = obj.Value<float>("x");
            var y = obj.Value<float>("y");
            var z = obj.Value<float>("z");
            return new Vector3(x, y, z);
        }

        private static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static RaycastArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new RaycastArgs();
            }

            return JsonConvert.DeserializeObject<RaycastArgs>(args) ?? new RaycastArgs();
        }
    }

    internal class RaycastArgs
    {
        [JsonProperty("origin")]
        public JObject Origin { get; set; }

        [JsonProperty("direction")]
        public JObject Direction { get; set; }

        [JsonProperty("maxDistance")]
        public float MaxDistance { get; set; } = 1000f;

        [JsonProperty("layerMask")]
        public int LayerMask { get; set; } = -1;
    }

    internal class RaycastResult
    {
        [JsonProperty("hasHit")]
        public bool HasHit { get; set; }

        [JsonProperty("point")]
        public Vector3Result Point { get; set; }

        [JsonProperty("normal")]
        public Vector3Result Normal { get; set; }

        [JsonProperty("distance")]
        public float Distance { get; set; }

        [JsonProperty("gameObjectName")]
        public string GameObjectName { get; set; }

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class Vector3Result
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }

        public Vector3Result()
        {
        }

        public Vector3Result(Vector3 v)
        {
            X = v.x;
            Y = v.y;
            Z = v.z;
        }
    }
}
