using System;
using System.IO;
using System.Net;
using System.Text;
using Appalachia.CI.Packaging.Editor.PackageRegistry.Core;
using UnityEngine;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    public class NPMPublish
    {
        public static void Publish(string packageFolder, string registry)
        {
            var manager = new CredentialManager();
            if (!manager.HasRegistry(registry))
            {
                throw new IOException("Credentials not set for registry " + registry);
            }

            var token = manager.GetCredential(registry).token;

            var manifest = new PublicationManifest(packageFolder, registry);
            ;

            using (var client = new ExpectContinueAware())
            {
                var upload = NPMLogin.UrlCombine(registry, manifest.name);

                client.Encoding = Encoding.UTF8;
                client.Headers.Add(HttpRequestHeader.Accept,        "application/json");
                client.Headers.Add(HttpRequestHeader.ContentType,   "application/json");
                client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);

                // Headers set by the NPM client, but not by us. Option to try with compatibility issues.

                // client.Headers.Add("npm-in-ci", "false");
                // client.Headers.Add("npm-scope", "");
                // client.Headers.Add(HttpRequestHeader.UserAgent, "npm/6.14.4 node/v12.16.2 linux x64");
                // var random = new Random();
                // string a = String.Format("{0:X8}", random.Next(0x10000000, int.MaxValue)).ToLower();
                // string b = String.Format("{0:X8}", random.Next(0x10000000, int.MaxValue)).ToLower();

                // client.Headers.Add("npm-session", a + b);
                // client.Headers.Add("referer", "publish");

                try
                {
                    var responseString = client.UploadString(
                        upload,
                        WebRequestMethods.Http.Put,
                        manifest.Request
                    );

                    try
                    {
                        var response = JsonUtility.FromJson<NPMResponse>(responseString);
                        if (string.IsNullOrEmpty(response.ok))
                        {
                            throw new IOException(responseString);
                        }
                    }
                    catch (Exception)
                    {
                        throw new IOException(responseString);
                    }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        var receiveStream = e.Response.GetResponseStream();

                        // Pipes the stream to a higher level stream reader with the required encoding format.
                        var readStream = new StreamReader(receiveStream, Encoding.UTF8);
                        var responseString = readStream.ReadToEnd();
                        e.Response.Close();
                        readStream.Close();

                        try
                        {
                            var response = JsonUtility.FromJson<NPMResponse>(responseString);

                            if (string.IsNullOrEmpty(response.error))
                            {
                                throw new IOException(responseString);
                            }

                            var reason = string.IsNullOrEmpty(response.reason)
                                ? ""
                                : Environment.NewLine + response.reason;

                            throw new IOException(response.error + reason);
                        }
                        catch (Exception)
                        {
                            throw new IOException(responseString);
                        }
                    }

                    if (e.InnerException != null)
                    {
                        throw new IOException(e.InnerException.Message);
                    }

                    throw new IOException(e.Message);
                }
            }
        }
    }
}
