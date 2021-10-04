using System;
using System.Collections.Generic;
using System.IO;
using Appalachia.CI.Packaging.PackageRegistry.NPM;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Appalachia.CI.Packaging.AssemblyDefinitions
{
    public class GitRepository
    {
        public DirectoryInfo repositoryRoot;
        public DirectoryInfo repositoryGitFolder;
        public JObject packageJson;

        public override string ToString()
        {
            return $"{repositoryRoot.FullName}: {packageJson["version"]}";
        }
    }
    
    public class AssemblyDefinitionModelWrapper
    {
        public AssemblyDefinitionModelWrapper(string filePath, string fileId, AssemblyDefinitionModel model)
        {
            this.filePath = filePath;
            this.fileId = fileId;
            this.model = model;
        }

        public string filePath;
        public string fileId;
        public AssemblyDefinitionModel model;
        public GitRepository repository;
        public bool IsPackage => filePath.StartsWith("Package");
        public bool IsAsset => filePath.StartsWith("Asset");
    }
    
    [Serializable]
    public class AssemblyDefinitionModel
    {
        public string name;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public string[] versionDefines;
        public bool noEngineReferences;
    }

    public static class AssemblyManager
    {
        public static List<GitRepository> repositories;
        public static List<AssemblyDefinitionModelWrapper> assetAssemblies;
        public static Dictionary<string, AssemblyDefinitionModelWrapper> assemblyLookup;
        
        [InitializeOnLoadMethod]        
        public static void InitializeAssemblies()
        {

            assetAssemblies = new List<AssemblyDefinitionModelWrapper>();
            assemblyLookup = new Dictionary<string, AssemblyDefinitionModelWrapper>();
            repositories = new List<GitRepository>();
            
            var assemblyDefinitionFileIds = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");

            foreach (var assemblyDefinitionFileId in assemblyDefinitionFileIds)
            {
                var assemblyDefinitionFilePath =
                    AssetDatabase.GUIDToAssetPath(assemblyDefinitionFileId);

                var assemblyDefinition =
                    AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(
                        assemblyDefinitionFilePath
                    );

                var assemblyDefinitionJSON = assemblyDefinition.text;

                var model = JsonUtility.FromJson<AssemblyDefinitionModel>(assemblyDefinitionJSON);
                var wrapper = new AssemblyDefinitionModelWrapper(
                    assemblyDefinitionFilePath,
                    assemblyDefinitionFileId,
                    model
                );
                
                if (wrapper.IsAsset)
                {
                    assetAssemblies.Add(wrapper);
                }

                assemblyLookup.Add(assemblyDefinitionFileId, wrapper);
            }
            
            var directoryInfo = new DirectoryInfo(Application.dataPath);
            
            var children = directoryInfo.GetDirectories();

            RecursiveGitRepositorySearch(children, repositories);

            foreach (var gitRepository in repositories)
            {
            }
        }

        private static void RecursiveGitRepositorySearch(DirectoryInfo[] children, List<GitRepository> repositories)
        {
            foreach (var child in children)
            {
                var subchildren = child.GetDirectories();

                foreach (var subchild in subchildren)
                {
                    if (subchild.Name == ".git")
                    {
                        var repo = new GitRepository
                        {
                            repositoryRoot = child, repositoryGitFolder = subchild
                        };

                        var childFiles = child.GetFiles();

                        foreach (var childFile in childFiles)
                        {
                            if (childFile.Name == "package.json")
                            {
                                repo.packageJson =
                                    PublicationManifest.LoadManifest(childFile.Directory.FullName);
                            }
                        }

                        repositories.Add(repo);
                    }
                }
                
                
                RecursiveGitRepositorySearch(subchildren, repositories);
            }

        }
    }
}
