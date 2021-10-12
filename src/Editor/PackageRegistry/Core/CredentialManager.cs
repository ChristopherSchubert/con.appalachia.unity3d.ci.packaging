using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.Core
{
    public class CredentialManager
    {
        private readonly List<NPMCredential> credentials = new();

        private readonly string upmconfigFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".upmconfig.toml"
        );

        public CredentialManager()
        {
            if (File.Exists(upmconfigFile))
            {
                var text = File.ReadAllText(upmconfigFile);
                var config = JsonConvert.DeserializeObject<NPMCredential[]>(text);

                credentials.Clear();
                credentials.AddRange(config);
            }
        }

        public List<NPMCredential> CredentialSet => credentials;

        public string[] Registries
        {
            get
            {
                var urls = new string[credentials.Count];
                var index = 0;
                foreach (var cred in CredentialSet)
                {
                    urls[index] = cred.url;
                    ++index;
                }

                return urls;
            }
        }

        public void Write()
        {
            foreach (var credential in credentials)
            {
                if (string.IsNullOrEmpty(credential.token))
                {
                    credential.token = string.Empty;
                }
            }

            var json = JsonConvert.SerializeObject(credentials.ToArray());

            File.WriteAllText(upmconfigFile, json);
        }

        public bool HasRegistry(string url)
        {
            return credentials.Any(x => x.url.Equals(url, StringComparison.Ordinal));
        }

        public NPMCredential GetCredential(string url)
        {
            return credentials.FirstOrDefault(
                x => x.url?.Equals(url, StringComparison.Ordinal) ?? false
            );
        }

        public void SetCredential(string url, bool alwaysAuth, string token)
        {
            if (HasRegistry(url))
            {
                var cred = GetCredential(url);
                cred.url = url;
                cred.alwaysAuth = alwaysAuth;
                cred.token = token;
            }
            else
            {
                var newCred = new NPMCredential();
                newCred.url = url;
                newCred.alwaysAuth = alwaysAuth;
                newCred.token = token;

                credentials.Add(newCred);
            }
        }

        public void RemoveCredential(string url)
        {
            if (HasRegistry(url))
            {
                credentials.RemoveAll(x => x.url.Equals(url, StringComparison.Ordinal));
            }
        }
    }
}
