using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcp
{
    /// <summary>
    /// git差分で変更された.csファイルから、影響を受けるアセンブリ名を解決する。
    /// asmdef間の参照（GUID参照・名前参照の両方）を辿り、変更されたコードに依存する
    /// テストアセンブリまで含めて返す。
    /// </summary>
    internal static class GitChangedAssemblyResolver
    {
        /// <summary>
        /// 指定した参照との差分から、影響を受けるアセンブリ名の集合を返す。
        /// </summary>
        /// <param name="gitRef">比較基準のgit参照（例: "HEAD"）</param>
        public static HashSet<string> Resolve(string gitRef)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var changedFiles = GetChangedCsFiles(projectRoot, gitRef);
            if (changedFiles.Count == 0)
            {
                return new HashSet<string>();
            }

            var asmdefs = DiscoverAsmdefs(projectRoot);
            var changedAssemblies = ResolveOwningAssemblies(changedFiles, asmdefs);
            return ExpandWithDependents(changedAssemblies, asmdefs);
        }

        private static List<string> GetChangedCsFiles(string projectRoot, string gitRef)
        {
            var relativePaths = new List<string>();
            relativePaths.AddRange(RunGit(projectRoot, "diff", "--name-only", gitRef));
            relativePaths.AddRange(RunGit(projectRoot, "ls-files", "--others", "--exclude-standard"));

            return relativePaths
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetFullPath(Path.Combine(projectRoot, path)))
                .Distinct()
                .ToList();
        }

        private static string[] RunGit(string workingDirectory, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            try
            {
                using var process = Process.Start(startInfo);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"git {string.Join(" ", arguments)} failed: {error.Trim()}");
                }

                return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Win32Exception e)
            {
                throw new InvalidOperationException(
                    "git command was not found. Ensure git is installed and available on PATH.", e);
            }
        }

        private sealed class AsmdefNode
        {
            public string Name { get; }
            public string Directory { get; }
            public HashSet<string> References { get; } = new();

            public AsmdefNode(string name, string directory)
            {
                Name = name;
                Directory = directory;
            }
        }

        private static List<AsmdefNode> DiscoverAsmdefs(string projectRoot)
        {
            var asmdefFiles = FindAsmdefFiles(projectRoot);
            var guidToName = new Dictionary<string, string>();
            var rawReferencesByName = new Dictionary<string, List<string>>();
            var nodes = new List<AsmdefNode>();

            foreach (var file in asmdefFiles)
            {
                var json = JObject.Parse(File.ReadAllText(file));
                var name = json["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var guid = ReadGuid(file + ".meta");
                if (!string.IsNullOrEmpty(guid))
                {
                    guidToName[guid] = name;
                }

                rawReferencesByName[name] = json["references"]?.Select(r => r.ToString()).ToList()
                                             ?? new List<string>();
                nodes.Add(new AsmdefNode(name, Path.GetDirectoryName(file) + Path.DirectorySeparatorChar));
            }

            foreach (var node in nodes)
            {
                ResolveReferences(node, rawReferencesByName[node.Name], guidToName);
            }

            return nodes;
        }

        private static List<string> FindAsmdefFiles(string projectRoot)
        {
            var asmdefFiles = new List<string>();
            foreach (var root in new[] { "Assets", "Packages" })
            {
                var rootPath = Path.Combine(projectRoot, root);
                if (!Directory.Exists(rootPath))
                {
                    continue;
                }
                asmdefFiles.AddRange(Directory.EnumerateFiles(rootPath, "*.asmdef", SearchOption.AllDirectories));
            }

            return asmdefFiles;
        }

        private static void ResolveReferences(
            AsmdefNode node,
            List<string> rawReferences,
            Dictionary<string, string> guidToName)
        {
            foreach (var raw in rawReferences)
            {
                var resolved = ResolveReferenceName(raw, guidToName);
                if (!string.IsNullOrEmpty(resolved))
                {
                    node.References.Add(resolved);
                }
            }
        }

        private static string ResolveReferenceName(string raw, Dictionary<string, string> guidToName)
        {
            const string guidPrefix = "GUID:";
            if (!raw.StartsWith(guidPrefix, StringComparison.Ordinal))
            {
                return raw;
            }

            var guid = raw.Substring(guidPrefix.Length);
            return guidToName.TryGetValue(guid, out var name) ? name : null;
        }

        private static string ReadGuid(string metaFilePath)
        {
            if (!File.Exists(metaFilePath))
            {
                return null;
            }

            var match = Regex.Match(File.ReadAllText(metaFilePath), @"guid:\s*([0-9a-fA-F]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static HashSet<string> ResolveOwningAssemblies(List<string> changedFiles, List<AsmdefNode> asmdefs)
        {
            var orderedByDepth = asmdefs.OrderByDescending(node => node.Directory.Length).ToList();
            var owners = new HashSet<string>();

            foreach (var file in changedFiles)
            {
                var owner = orderedByDepth.FirstOrDefault(
                    node => file.StartsWith(node.Directory, StringComparison.OrdinalIgnoreCase));
                if (owner != null)
                {
                    owners.Add(owner.Name);
                }
            }

            return owners;
        }

        /// <summary>
        /// 変更されたアセンブリ自身に加え、それを（間接的にも）参照しているアセンブリを
        /// 依存グラフを逆向きに辿って収集する。テストアセンブリはソースを参照しているため
        /// この探索で自然に含まれる。
        /// </summary>
        private static HashSet<string> ExpandWithDependents(HashSet<string> changedAssemblies, List<AsmdefNode> asmdefs)
        {
            var result = new HashSet<string>(changedAssemblies);
            var queue = new Queue<string>(changedAssemblies);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var node in asmdefs)
                {
                    if (!node.References.Contains(current) || !result.Add(node.Name))
                    {
                        continue;
                    }
                    queue.Enqueue(node.Name);
                }
            }

            return result;
        }
    }
}
