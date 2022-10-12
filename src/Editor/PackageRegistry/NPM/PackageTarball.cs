using System.IO;
using Appalachia.CI.Integration;
using Appalachia.CI.Integration.FileSystem;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    /// <summary>
    ///     Tool to create tarballs for packages.
    ///     UnityEditor.PackageManager.Client.Pack() creates a broken tarball that gets rejected by bintray.
    /// </summary>
    public class PackageTarball
    {
        public static string Create(string packageFolder, string outputFolder)
        {
            var manifest = PublicationManifest.LoadManifest(packageFolder);

            var packageName = manifest["name"] + "-" + manifest["version"] + ".tgz";

            AppaDirectory.CreateDirectory(outputFolder);

            var outputFile = AppaPath.Combine(outputFolder, packageName);

            Stream outStream = AppaFile.Create(outputFile);
            Stream gzoStream = new GZipOutputStream(outStream);
            var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

            AddDirectoryFilesToTar(tarArchive, packageFolder, true, "packages/");
            tarArchive.Close();
            gzoStream.Close();
            outStream.Close();

            return outputFile;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (AppaDirectory.Exists(path) && !path.EndsWith(AppaPath.DirectorySeparatorChar.ToString()))
            {
                return path + AppaPath.DirectorySeparatorChar;
            }

            return path;
        }

        private static void AddDirectoryFilesToTar(
            TarArchive tarArchive,
            string sourceDirectory,
            bool recurse,
            string directoryName)
        {
            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            var tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarEntry.Name = directoryName;
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            var filenames = AppaDirectory.GetFiles(sourceDirectory);
            foreach (var filename in filenames)
            {
                var fileEntry = TarEntry.CreateEntryFromFile(filename);
                fileEntry.Name = directoryName + AppaPath.GetFileName(filename);
                tarArchive.WriteEntry(fileEntry, true);
            }

            if (recurse)
            {
                var directories = AppaDirectory.GetDirectories(sourceDirectory);
                foreach (var directory in directories)
                {
                    var newDirectory = directoryName + new AppaDirectoryInfo(directory).Name + "/";
                    AddDirectoryFilesToTar(tarArchive, directory, recurse, newDirectory);
                }
            }
        }
    }
}
