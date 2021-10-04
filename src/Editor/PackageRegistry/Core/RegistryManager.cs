using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Appalachia.CI.Packaging.PackageRegistry.Core
{
    public class RegistryManager
    {
        private readonly string manifest = Path.Combine(
            Application.dataPath,
            "..",
            "Packages",
            "manifest.json"
        );

        public RegistryManager()
        {
            credentialManager = new CredentialManager();
            registries = new List<ScopedRegistry>();

            var manifestJSON = JObject.Parse(File.ReadAllText(manifest));

            var Jregistries = (JArray) manifestJSON["scopedRegistries"];
            if (Jregistries != null)
            {
                foreach (var JRegistry in Jregistries)
                {
                    registries.Add(LoadRegistry((JObject) JRegistry));
                }
            }
            else
            {
                Debug.Log("No scoped registries set");
            }
        }

        public List<ScopedRegistry> registries { get; }

        public CredentialManager credentialManager { get; }

        private ScopedRegistry LoadRegistry(JObject Jregistry)
        {
            var registry = new ScopedRegistry();
            registry.name = (string) Jregistry["name"];
            registry.url = (string) Jregistry["url"];

            var scopes = new List<string>();
            foreach (var scope in (JArray) Jregistry["scopes"])
            {
                scopes.Add((string) scope);
            }

            registry.scopes = new List<string>(scopes);

            if (credentialManager.HasRegistry(registry.url))
            {
                var credential = credentialManager.GetCredential(registry.url);
                registry.auth = credential.alwaysAuth;
                registry.token = credential.token;
            }

            return registry;
        }

        private void UpdateScope(ScopedRegistry registry, JToken registryElement)
        {
            var scopes = new JArray();
            foreach (var scope in registry.scopes)
            {
                scopes.Add(scope);
            }

            registryElement["scopes"] = scopes;
        }

        private JToken GetOrCreateScopedRegistry(ScopedRegistry registry, JObject manifestJSON)
        {
            var Jregistries = (JArray) manifestJSON["scopedRegistries"];
            if (Jregistries == null)
            {
                Jregistries = new JArray();
                manifestJSON["scopedRegistries"] = Jregistries;
            }

            foreach (var JRegistryElement in Jregistries)
            {
                if ((JRegistryElement["name"] != null) &&
                    (JRegistryElement["url"] != null) &&
                    string.Equals(
                        JRegistryElement["name"].Value<string>(),
                        registry.name,
                        StringComparison.Ordinal
                    ) &&
                    string.Equals(
                        JRegistryElement["url"].Value<string>(),
                        registry.url,
                        StringComparison.Ordinal
                    ))
                {
                    UpdateScope(registry, JRegistryElement);
                    return JRegistryElement;
                }
            }

            var JRegistry = new JObject();
            JRegistry["name"] = registry.name;
            JRegistry["url"] = registry.url;
            UpdateScope(registry, JRegistry);
            Jregistries.Add(JRegistry);

            return JRegistry;
        }

        public void Remove(ScopedRegistry registry)
        {
            var manifestJSON = JObject.Parse(File.ReadAllText(manifest));
            var Jregistries = (JArray) manifestJSON["scopedRegistries"];

            foreach (var JRegistryElement in Jregistries)
            {
                if ((JRegistryElement["name"] != null) &&
                    (JRegistryElement["url"] != null) &&
                    JRegistryElement["name"]
                       .Value<string>()
                       .Equals(registry.name, StringComparison.Ordinal) &&
                    JRegistryElement["url"]
                       .Value<string>()
                       .Equals(registry.url, StringComparison.Ordinal))
                {
                    JRegistryElement.Remove();
                    break;
                }
            }

            write(manifestJSON);
        }

        public void Save(ScopedRegistry registry)
        {
            var manifestJSON = JObject.Parse(File.ReadAllText(manifest));

            var manifestRegistry = GetOrCreateScopedRegistry(registry, manifestJSON);

            if (!string.IsNullOrEmpty(registry.token))
            {
                credentialManager.SetCredential(registry.url, registry.auth, registry.token);
            }
            else
            {
                credentialManager.RemoveCredential(registry.url);
            }

            write(manifestJSON);

            credentialManager.Write();
        }

        private void write(JObject manifestJSON)
        {
            File.WriteAllText(manifest, manifestJSON.ToString());
            AssetDatabase.Refresh();
        }
    }
}
