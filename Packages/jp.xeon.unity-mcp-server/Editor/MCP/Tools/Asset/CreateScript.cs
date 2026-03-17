using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityMcp.Tools.Asset
{
    /// <summary>
    /// 新しいC#スクリプトファイルを作成するツール
    /// </summary>
    public class CreateScript : IMcpTool
    {
        public string Name => "create_script";

        public string Description =>
            "Create a new C# script file in the Unity project. " +
            "Supports creating MonoBehaviour, ScriptableObject, or plain C# class scripts. " +
            "If content is provided, it will be written directly. " +
            "Otherwise, a template will be generated based on the scriptType.";

        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{" +
            "\"assetPath\":{\"type\":\"string\",\"description\":\"Asset path for the new script (e.g. 'Assets/Scripts/Player/PlayerController.cs')\"}," +
            "\"scriptType\":{\"type\":\"string\",\"enum\":[\"MonoBehaviour\",\"ScriptableObject\",\"PlainClass\",\"Interface\",\"Enum\",\"StaticClass\"],\"description\":\"Type of script template to generate (default: 'MonoBehaviour'). Ignored if content is provided.\",\"default\":\"MonoBehaviour\"}," +
            "\"namespaceName\":{\"type\":\"string\",\"description\":\"Namespace to wrap the class in. Ignored if content is provided.\"}," +
            "\"content\":{\"type\":\"string\",\"description\":\"Full script content. If provided, scriptType and namespaceName are ignored.\"}" +
            "},\"required\":[\"assetPath\"]}";

        public Task<object> Execute(string args)
        {
            var parameters = ParseArgs(args);
            ValidateArgs(parameters);

            var fullPath = Path.GetFullPath(parameters.AssetPath);
            var directoryPath = Path.GetDirectoryName(fullPath);
            var className = Path.GetFileNameWithoutExtension(parameters.AssetPath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var scriptContent = string.IsNullOrEmpty(parameters.Content)
                ? GenerateTemplate(className, parameters.ScriptType, parameters.NamespaceName)
                : parameters.Content;

            File.WriteAllText(fullPath, scriptContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(parameters.AssetPath);
            AssetDatabase.Refresh();

            var result = new CreateScriptResult(
                parameters.AssetPath,
                className,
                string.IsNullOrEmpty(parameters.Content) ? parameters.ScriptType : "Custom");
            return Task.FromResult<object>(result);
        }

        private static void ValidateArgs(CreateScriptArgs parameters)
        {
            if (string.IsNullOrEmpty(parameters.AssetPath))
            {
                throw new InvalidOperationException("assetPath is required.");
            }

            if (!parameters.AssetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"assetPath must end with '.cs': '{parameters.AssetPath}'");
            }

            if (!parameters.AssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !parameters.AssetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"assetPath must start with 'Assets/' or 'Packages/': '{parameters.AssetPath}'");
            }
        }

        private static string GenerateTemplate(string className, string scriptType, string namespaceName)
        {
            switch (scriptType)
            {
                case "MonoBehaviour":
                    return WrapNamespace(MonoBehaviourTemplate(className), namespaceName);
                case "ScriptableObject":
                    return WrapNamespace(ScriptableObjectTemplate(className), namespaceName);
                case "PlainClass":
                    return WrapNamespace(PlainClassTemplate(className), namespaceName);
                case "Interface":
                    return WrapNamespace(InterfaceTemplate(className), namespaceName);
                case "Enum":
                    return WrapNamespace(EnumTemplate(className), namespaceName);
                case "StaticClass":
                    return WrapNamespace(StaticClassTemplate(className), namespaceName);
                default:
                    return WrapNamespace(MonoBehaviourTemplate(className), namespaceName);
            }
        }

        private static string MonoBehaviourTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : MonoBehaviour");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string ScriptableObjectTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"[CreateAssetMenu(fileName = \"{className}\", menuName = \"{className}\")]");
            sb.AppendLine($"public class {className} : ScriptableObject");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string PlainClassTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string InterfaceTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"public interface {className}");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string EnumTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"public enum {className}");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string StaticClassTemplate(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string WrapNamespace(string body, string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                return body;
            }

            var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            var foundClassOrEnum = false;

            foreach (var line in lines)
            {
                if (!foundClassOrEnum && IsTypeDeclaration(line))
                {
                    foundClassOrEnum = true;
                    sb.AppendLine($"namespace {namespaceName}");
                    sb.AppendLine("{");
                }

                if (foundClassOrEnum && !string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine($"    {line}");
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool IsTypeDeclaration(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("public class ") ||
                   trimmed.StartsWith("public static class ") ||
                   trimmed.StartsWith("public interface ") ||
                   trimmed.StartsWith("public enum ") ||
                   trimmed.StartsWith("[CreateAssetMenu");
        }

        private static CreateScriptArgs ParseArgs(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CreateScriptArgs();
            }

            return JsonConvert.DeserializeObject<CreateScriptArgs>(args) ?? new CreateScriptArgs();
        }
    }

    internal class CreateScriptArgs
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; } = string.Empty;

        [JsonProperty("scriptType")]
        public string ScriptType { get; set; } = "MonoBehaviour";

        [JsonProperty("namespaceName")]
        public string NamespaceName { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }

    internal class CreateScriptResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; private set; }

        [JsonProperty("className")]
        public string ClassName { get; private set; }

        [JsonProperty("scriptType")]
        public string ScriptType { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public CreateScriptResult(string assetPath, string className, string scriptType)
        {
            AssetPath = assetPath;
            ClassName = className;
            ScriptType = scriptType;
            Message = $"Script '{className}' created at '{assetPath}'.";
        }
    }
}
