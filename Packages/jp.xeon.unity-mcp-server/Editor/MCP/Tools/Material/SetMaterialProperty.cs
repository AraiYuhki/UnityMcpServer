using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityMcp.Tools.Material
{
    /// <summary>
    /// マテリアルのシェーダープロパティを設定するツール
    /// </summary>
    public class SetMaterialProperty : IMcpTool
    {
        public string Name => "set_material_property";

        public string Description =>
            "Set a shader property value on a Material asset. " +
            "Use get_material_properties first to find valid property names and types. " +
            "Supports Float, Range, Int, Color (HTML '#RRGGBB' or {r,g,b,a}), " +
            "Vector ({x,y,z,w}), and Texture (asset path string).";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path to the .mat file (e.g. 'Assets/Materials/MyMat.mat')\"}," +
            "\"propertyName\":{\"type\":\"string\",\"description\":\"Shader property name (e.g. '_Color', '_MainTex', '_Metallic')\"}," +
            "\"value\":{\"description\":\"Value to set. Type depends on the property: number for Float/Range/Int, string for Color (HTML) or Texture (asset path), object for Color ({r,g,b,a}) or Vector ({x,y,z,w}).\"}" +
            "},\"required\":[\"assetPath\",\"propertyName\",\"value\"]}";

        public Task<object> Execute(string args)
        {
            var json = JObject.Parse(args);
            var assetPath = json.Value<string>("assetPath");
            var propertyName = json.Value<string>("propertyName");
            var valueToken = json["value"];

            ValidateArgs(assetPath, propertyName, valueToken);

            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(assetPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Material not found at path: '{assetPath}'");
            }

            if (!material.HasProperty(propertyName))
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' not found on material '{material.name}'");
            }

            var propertyType = FindPropertyType(material.shader, propertyName);
            ApplyValue(material, propertyName, propertyType, valueToken);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            var newValue = ReadBackValue(material, propertyName, propertyType);
            var result = new SetMaterialPropertyResult
            {
                AssetPath = assetPath,
                PropertyName = propertyName,
                NewValue = newValue,
                Message = $"Property '{propertyName}' on '{material.name}' has been updated."
            };

            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(string assetPath, string propertyName, JToken value)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new InvalidOperationException("propertyName is required.");
            }

            if (value == null)
            {
                throw new InvalidOperationException("value is required.");
            }
        }

        private static ShaderPropertyType FindPropertyType(Shader shader, string propertyName)
        {
            var count = shader.GetPropertyCount();
            for (var i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) == propertyName)
                {
                    return shader.GetPropertyType(i);
                }
            }

            throw new InvalidOperationException(
                $"Shader property '{propertyName}' not found in shader '{shader.name}'");
        }

        private static void ApplyValue(
            UnityEngine.Material material, string propertyName,
            ShaderPropertyType propertyType, JToken valueToken)
        {
            switch (propertyType)
            {
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    material.SetFloat(propertyName, valueToken.Value<float>());
                    break;
                case ShaderPropertyType.Int:
                    material.SetInteger(propertyName, valueToken.Value<int>());
                    break;
                case ShaderPropertyType.Color:
                    material.SetColor(propertyName, ParseColor(valueToken));
                    break;
                case ShaderPropertyType.Vector:
                    material.SetVector(propertyName, ParseVector(valueToken));
                    break;
                case ShaderPropertyType.Texture:
                    material.SetTexture(propertyName, LoadTexture(valueToken));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported shader property type: {propertyType}");
            }
        }

        private static Color ParseColor(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                var htmlString = token.Value<string>();
                if (ColorUtility.TryParseHtmlString(htmlString, out var color))
                {
                    return color;
                }

                throw new InvalidOperationException(
                    $"Invalid HTML color string: '{htmlString}'. Use format '#RRGGBB' or '#RRGGBBAA'.");
            }

            if (token.Type == JTokenType.Object)
            {
                return new Color(
                    token.Value<float>("r"),
                    token.Value<float>("g"),
                    token.Value<float>("b"),
                    token["a"] != null ? token.Value<float>("a") : 1f);
            }

            throw new InvalidOperationException(
                "Color value must be an HTML string (e.g. '#FF0000') or object with 'r','g','b','a' fields.");
        }

        private static Vector4 ParseVector(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                return new Vector4(
                    token["x"] != null ? token.Value<float>("x") : 0f,
                    token["y"] != null ? token.Value<float>("y") : 0f,
                    token["z"] != null ? token.Value<float>("z") : 0f,
                    token["w"] != null ? token.Value<float>("w") : 0f);
            }

            throw new InvalidOperationException(
                "Vector value must be an object with 'x','y','z','w' fields.");
        }

        private static Texture LoadTexture(JToken token)
        {
            var texturePath = token.Value<string>();
            if (string.IsNullOrEmpty(texturePath) || texturePath == "null")
            {
                return null;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Texture not found at path: '{texturePath}'");
            }

            return texture;
        }

        private static object ReadBackValue(
            UnityEngine.Material material, string propertyName, ShaderPropertyType propertyType)
        {
            switch (propertyType)
            {
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    return material.GetFloat(propertyName);
                case ShaderPropertyType.Int:
                    return material.GetInteger(propertyName);
                case ShaderPropertyType.Color:
                    var color = material.GetColor(propertyName);
                    return new { r = color.r, g = color.g, b = color.b, a = color.a };
                case ShaderPropertyType.Vector:
                    var vec = material.GetVector(propertyName);
                    return new { x = vec.x, y = vec.y, z = vec.z, w = vec.w };
                case ShaderPropertyType.Texture:
                    var tex = material.GetTexture(propertyName);
                    return tex != null ? AssetDatabase.GetAssetPath(tex) : null;
                default:
                    return null;
            }
        }
    }

    internal class SetMaterialPropertyResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        [JsonProperty("propertyName")]
        public string PropertyName { get; set; }

        [JsonProperty("newValue")]
        public object NewValue { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
