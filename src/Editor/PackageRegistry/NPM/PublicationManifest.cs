using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Appalachia.CI.Integration;
using Appalachia.CI.Integration.FileSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    /// <summary>
    ///     Helper class to create the JSON data to upload to the package server
    /// </summary>
    internal class PublicationManifest
    {
        private readonly JObject j = new();
        private string base64Data;
        private string sha1;
        private string sha512;
        private long size;

        internal PublicationManifest(string packageFolder, string registry)
        {
            CreateTarball(packageFolder);

            var manifest = LoadManifest(packageFolder);

            name = manifest["name"].ToString();
            var version = manifest["version"].ToString();
            var description = manifest["description"].ToString();

            var tarballName = name + "-" + version + ".tgz";
            var tarballPath = name + "/-/" + tarballName;

            var tarballUri = NPMLogin.UrlCombine(registry, tarballPath);
            tarballUri = Regex.Replace(tarballUri, @"^https:\/\/", "http://");

            var readmeFile = GetReadmeFilename(packageFolder);
            string readme = null;
            if (readmeFile != null)
            {
                readme = GetReadme(readmeFile);
            }

            j["_id"] = name;
            j["name"] = name;
            j["description"] = description;

            j["dist-tags"] = new JObject();
            j["dist-tags"]["latest"] = version;

            j["versions"] = new JObject();
            j["versions"][version] = manifest;

            if (!string.IsNullOrEmpty(readmeFile))
            {
                j["versions"][version]["readme"] = readme;
                j["versions"][version]["readmeFilename"] = readmeFile;
            }

            j["versions"][version]["_id"] = name + "@" + version;

            // Extra options set by the NPM client. Will not set here as they do not seem neccessary.

            // j["versions"][version]["_npmUser"] = new JObject();
            // j["versions"][version]["_npmUser"]["name"] = "";
            // j["versions"][version]["_npmUser"]["email"] = "";
            // j["versions"][version]["_npmVersion"] = "6.14.4";
            // j["versions"][version]["_nodeVersion"] = "12.16.2";

            j["versions"][version]["dist"] = new JObject();
            j["versions"][version]["dist"]["integrity"] = sha512;
            j["versions"][version]["dist"]["shasum"] = sha1;
            j["versions"][version]["dist"]["tarball"] = tarballUri;

            if (!string.IsNullOrEmpty(readme))
            {
                j["readme"] = readme;
            }

            j["_attachments"] = new JObject();
            j["_attachments"][tarballName] = new JObject();
            j["_attachments"][tarballName]["content_type"] = "application/octet-stream";
            j["_attachments"][tarballName]["length"] = new JValue(size);
            j["_attachments"][tarballName]["data"] = base64Data;
        }

        public string name { get; }

        public string Request => j.ToString(Formatting.None);

        internal static JObject LoadManifest(string packageFolder)
        {
            var manifestPath = AppaPath.Combine(packageFolder, "package.json");

            if (!AppaFile.Exists(manifestPath))
            {
                throw new AppaIOException(
                    "Invalid package folder. Cannot find package.json in " + packageFolder
                );
            }

            var manifest = JObject.Parse(AppaFile.ReadAllText(manifestPath));

            if (manifest["name"] == null)
            {
                throw new AppaIOException("Package name not set");
            }

            if (manifest["version"] == null)
            {
                throw new AppaIOException("Package version not set");
            }

            if (manifest["description"] == null)
            {
                throw new AppaIOException("Package description not set");
            }

            return manifest;
        }

        private string GetReadmeFilename(string packageFolder)
        {
            foreach (var path in AppaDirectory.EnumerateFiles(packageFolder))
            {
                var file = AppaPath.GetFileName(path);
                if (file.Equals("readme.md",  StringComparison.InvariantCultureIgnoreCase) ||
                    file.Equals("readme.txt", StringComparison.InvariantCultureIgnoreCase) ||
                    file.Equals("readme",     StringComparison.InvariantCultureIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        private string GetReadme(string readmeFile)
        {
            return AppaFile.ReadAllText(readmeFile);
        }

        private string SHA512(byte[] data)
        {
            var sha = new SHA512Managed();
            var checksum = sha.ComputeHash(data);
            return "sha512-" + Convert.ToBase64String(checksum);
        }

        private string SHA1(byte[] data)
        {
            var sha = new SHA1Managed();
            var checksum = sha.ComputeHash(data);
            return BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower();
        }

        public void CreateTarball(string packageFolder)
        {
            var folder = FileUtil.GetUniqueTempPathInProject();
            var file = PackageTarball.Create(packageFolder, folder);

            var bytes =AppaFile.ReadAllBytes(file);
            base64Data = Convert.ToBase64String(bytes);
            size = bytes.Length;

            sha1 = SHA1(bytes);
            sha512 = SHA512(bytes);

            AppaFile.Delete(file);
            AppaDirectory.Delete(folder);
        }
    }
}
