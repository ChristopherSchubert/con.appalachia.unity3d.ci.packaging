using System.Collections.Generic;
using System.IO;
using Appalachia.CI.Integration;
using Appalachia.CI.Integration.Assemblies;
using Appalachia.CI.Integration.Repositories;
using UnityEditor;

namespace Appalachia.CI.Packaging.Editor.AssemblyDefinitions
{
    public static class AssemblyManager
    {
        public static List<RepositoryDirectoryMetadata> repositories;
        public static List<AssemblyDefinitionMetadata> assetAssemblies;
        public static Dictionary<string, AssemblyDefinitionMetadata> assemblyLookup;

        //[InitializeOnLoadMethod]
        public static void InitializeAssemblies()
        {
            assetAssemblies = new List<AssemblyDefinitionMetadata>();
            assemblyLookup = new Dictionary<string, AssemblyDefinitionMetadata>();
            repositories = new List<RepositoryDirectoryMetadata>();

            var assemblyDefinitionFileIds = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");

            foreach (var assemblyDefinitionFileId in assemblyDefinitionFileIds)
            {
                var assemblyDefinitionFilePath =
                    AssetDatabase.GUIDToAssetPath(assemblyDefinitionFileId);

                var wrapper = AssemblyDefinitionMetadata.CreateNew(assemblyDefinitionFilePath);

                if (wrapper.IsAsset)
                {
                    assetAssemblies.Add(wrapper);
                }

                assemblyLookup.Add(assemblyDefinitionFileId, wrapper);
            }

            var directoryInfo = new DirectoryInfo(ProjectLocations.GetAssetsDirectoryPath());

            var children = directoryInfo.GetDirectories();

            RecursiveGitRepositorySearch(children, repositories);
        }

        private static void RecursiveGitRepositorySearch(
            DirectoryInfo[] children,
            List<RepositoryDirectoryMetadata> repositories)
        {
            foreach (var child in children)
            {
                var subchildren = child.GetDirectories();

                foreach (var subchild in subchildren)
                {
                    if (subchild.Name == ".git")
                    {
                        var repo = RepositoryDirectoryMetadata.FromRoot(child);
                        
                        repositories.Add(repo);
                        break;
                    }
                }

                RecursiveGitRepositorySearch(subchildren, repositories);
            }
        }
    }
}
