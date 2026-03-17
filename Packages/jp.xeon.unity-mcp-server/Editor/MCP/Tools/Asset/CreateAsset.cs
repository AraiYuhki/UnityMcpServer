using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// ScriptableObjectアセットを新規作成するツール
    /// </summary>
    public class CreateAsset : IMcpTool
    {
        public string Name => "create_asset";

        public string Description =>
            "Create a new ScriptableObject asset in the Unity project. " +
            "Specify the ScriptableObject type name and the save path. " +
            "The type must be a class that derives from ScriptableObject and be available in loaded assemblies.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"typeName\":{\"type\":\"string\",\"description\":\"ScriptableObject type name (e.g. 'MyDataAsset')\"}," +
            "\"savePath\":{\"type\":\"string\",\"description\":\"Asset path to save (e.g. 'Assets/Data/MyData.asset')\"}" +
            "},\"required\":[\"typeName\",\"savePath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var type = FindScriptableObjectType(parameters.TypeName);
            var instance = ScriptableObject.CreateInstance(type);

            EnsureDirectoryExists(parameters.SavePath);
            AssetDatabase.CreateAsset(instance, parameters.SavePath);
            AssetDatabase.SaveAssets();

            var result = new CreateAssetResult(parameters.SavePath, parameters.TypeName);
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(CreateAssetArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.TypeName))
            {
                throw new InvalidOperationException("typeName is required.");
            }

            if (string.IsNullOrEmpty(parameters.SavePath))
            {
                throw new InvalidOperationException("savePath is required.");
            }

            if (!parameters.SavePath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"savePath must end with '.asset': '{parameters.SavePath}'");
            }

            if (!parameters.SavePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"savePath must start with 'Assets/': '{parameters.SavePath}'");
            }
        }

        private static Type FindScriptableObjectType(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(t => t.Name == typeName && typeof(ScriptableObject).IsAssignableFrom(t));

            if (type == null)
            {
                throw new InvalidOperationException(
                    $"Type '{typeName}' not found or does not derive from ScriptableObject.");
            }

            return type;
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static CreateAssetArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CreateAssetArgs();
            }

            return JsonConvert.DeserializeObject<CreateAssetArgs>(args) ?? new CreateAssetArgs();
        }
    }

    internal class CreateAssetArgs
    {
        [JsonProperty("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonProperty("savePath")]
        public string SavePath { get; set; } = string.Empty;
    }

    internal class CreateAssetResult
    {
        [JsonProperty("savePath")]
        public string SavePath { get; private set; }

        [JsonProperty("typeName")]
        public string TypeName { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public CreateAssetResult(string savePath, string typeName)
        {
            SavePath = savePath;
            TypeName = typeName;
            Message = $"ScriptableObject '{typeName}' created at '{savePath}'.";
        }
    }
}
