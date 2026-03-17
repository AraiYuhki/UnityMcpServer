using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityMcp.Tools.Material
{
    /// <summary>
    /// マテリアルのシェーダープロパティ一覧を返すツール
    /// </summary>
    public class GetMaterialProperties : IMcpTool
    {
        public string Name => "get_material_properties";

        public string Description =>
            "Get all shader properties of a Material asset. " +
            "Returns property names, types, and current values. " +
            "Specify the material by asset path (e.g. 'Assets/Materials/MyMat.mat') " +
            "or by a renderer on a scene GameObject (hierarchy path). " +
            "Use get_scene_hierarchy or get_asset_list to find the correct path first.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Direct asset path to .mat file (e.g. 'Assets/Materials/MyMat.mat')\"}," +
            "\"gameObjectPath\":{\"type\":\"string\",\"description\":\"Hierarchy path to a GameObject with a Renderer component (e.g. 'Environment/Cube'). Uses sharedMaterial.\"}," +
            "\"materialIndex\":{\"type\":\"integer\",\"description\":\"Index of the material on the renderer (default: 0). Only used with gameObjectPath.\",\"default\":0}" +
            "}}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            var material = ResolveMaterial(parameters);
            var properties = CollectProperties(material);

            var result = new GetMaterialPropertiesResult
            {
                MaterialName = material.name,
                ShaderName = material.shader.name,
                Properties = properties
            };

            return Task.FromResult<object>(result);
        }

        private static GetMaterialPropertiesArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new GetMaterialPropertiesArgs();
            }

            return JsonConvert.DeserializeObject<GetMaterialPropertiesArgs>(args)
                   ?? new GetMaterialPropertiesArgs();
        }

        private static UnityEngine.Material ResolveMaterial(GetMaterialPropertiesArgs parameters)
        {
            if (!string.IsNullOrEmpty(parameters.AssetPath))
            {
                return LoadMaterialFromAsset(parameters.AssetPath);
            }

            if (!string.IsNullOrEmpty(parameters.GameObjectPath))
            {
                return LoadMaterialFromRenderer(parameters.GameObjectPath, parameters.MaterialIndex);
            }

            throw new InvalidOperationException(
                "At least one of 'assetPath' or 'gameObjectPath' is required.");
        }

        private static UnityEngine.Material LoadMaterialFromAsset(string assetPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(assetPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Material not found at path: '{assetPath}'");
            }

            return material;
        }

        private static UnityEngine.Material LoadMaterialFromRenderer(string gameObjectPath, int materialIndex)
        {
            var go = FindGameObject(gameObjectPath);
            if (go == null)
            {
                throw new InvalidOperationException($"GameObject not found: '{gameObjectPath}'");
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"No Renderer component found on '{gameObjectPath}'");
            }

            var materials = renderer.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                throw new InvalidOperationException(
                    $"materialIndex {materialIndex} is out of range. Renderer has {materials.Length} material(s).");
            }

            var material = materials[materialIndex];
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Material at index {materialIndex} is null on '{gameObjectPath}'");
            }

            return material;
        }

        private static List<MaterialPropertyInfo> CollectProperties(UnityEngine.Material material)
        {
            var shader = material.shader;
            var count = shader.GetPropertyCount();
            var properties = new List<MaterialPropertyInfo>(count);

            for (var i = 0; i < count; i++)
            {
                var info = ExtractPropertyInfo(material, shader, i);
                properties.Add(info);
            }

            return properties;
        }

        private static MaterialPropertyInfo ExtractPropertyInfo(
            UnityEngine.Material material, Shader shader, int index)
        {
            var propertyName = shader.GetPropertyName(index);
            var propertyType = shader.GetPropertyType(index);
            var description = shader.GetPropertyDescription(index);
            var value = ReadPropertyValue(material, propertyName, propertyType, shader, index);

            return new MaterialPropertyInfo
            {
                Name = propertyName,
                Type = propertyType.ToString(),
                Description = description,
                Value = value
            };
        }

        private static object ReadPropertyValue(
            UnityEngine.Material material, string propertyName,
            ShaderPropertyType propertyType, Shader shader, int index)
        {
            switch (propertyType)
            {
                case ShaderPropertyType.Float:
                    return material.GetFloat(propertyName);
                case ShaderPropertyType.Range:
                    return ReadRangeValue(material, propertyName, shader, index);
                case ShaderPropertyType.Int:
                    return material.GetInteger(propertyName);
                case ShaderPropertyType.Color:
                    return FormatColor(material.GetColor(propertyName));
                case ShaderPropertyType.Vector:
                    return FormatVector(material.GetVector(propertyName));
                case ShaderPropertyType.Texture:
                    return ReadTextureValue(material, propertyName);
                default:
                    return null;
            }
        }

        private static object ReadRangeValue(
            UnityEngine.Material material, string propertyName, Shader shader, int index)
        {
            var limits = shader.GetPropertyRangeLimits(index);
            return new RangePropertyValue
            {
                CurrentValue = material.GetFloat(propertyName),
                Min = limits.x,
                Max = limits.y
            };
        }

        private static object FormatColor(Color color)
        {
            return new { r = color.r, g = color.g, b = color.b, a = color.a };
        }

        private static object FormatVector(Vector4 vector)
        {
            return new { x = vector.x, y = vector.y, z = vector.z, w = vector.w };
        }

        private static string ReadTextureValue(UnityEngine.Material material, string propertyName)
        {
            var texture = material.GetTexture(propertyName);
            if (texture == null)
            {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(texture);
            return string.IsNullOrEmpty(path) ? null : path;
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

    internal class GetMaterialPropertiesArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;

        [JsonProperty("gameObjectPath")]
        public string GameObjectPath { get; set; } = string.Empty;

        [JsonProperty("materialIndex")]
        public int MaterialIndex { get; set; } = 0;
    }

    internal class MaterialPropertyInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }

    internal class RangePropertyValue
    {
        [JsonProperty("current")]
        public float CurrentValue { get; set; }

        [JsonProperty("min")]
        public float Min { get; set; }

        [JsonProperty("max")]
        public float Max { get; set; }
    }

    internal class GetMaterialPropertiesResult
    {
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("shaderName")]
        public string ShaderName { get; set; }

        [JsonProperty("properties")]
        public List<MaterialPropertyInfo> Properties { get; set; }
    }
}
