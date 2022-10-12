using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    public class NPMLogin
    {
        internal static string UrlCombine(string start, string more)
        {
            if (string.IsNullOrEmpty(start))
            {
                return more;
            }

            if (string.IsNullOrEmpty(more))
            {
                return start;
            }

            return start.TrimEnd('/') + "/" + more.TrimStart('/');
        }

        public static string GetBintrayToken(string user, string apiKey)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + apiKey));
        }

        public static NPMResponse GetLoginToken(string url, string user, string password)
        {
            using (var client = new WebClient())
            {
                var loginUri = UrlCombine(url, "/-/user/org.couchdb.user:" + user);
                client.Headers.Add(HttpRequestHeader.Accept,      "application/json");
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                client.Headers.Add(
                    HttpRequestHeader.Authorization,
                    "Basic " +
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password))
                );

                var request = new NPMLoginRequest();
                request.name = user;
                request.password = password;

                var requestString = JsonUtility.ToJson(request);

                try
                {
                    var responseString = client.UploadString(
                        loginUri,
                        WebRequestMethods.Http.Put,
                        requestString
                    );
                    var response = JsonUtility.FromJson<NPMResponse>(responseString);
                    return response;
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        try
                        {
                            var receiveStream = e.Response.GetResponseStream();

                            // Pipes the stream to a higher level stream reader with the required encoding format.
                            var readStream = new StreamReader(receiveStream, Encoding.UTF8);
                            var responseString = readStream.ReadToEnd();
                            e.Response.Close();
                            readStream.Close();

                            return JsonUtility.FromJson<NPMResponse>(responseString);
                        }
                        catch (Exception e2)
                        {
                            var response = new NPMResponse();
                            response.error = e2.Message;
                            return response;
                        }
                    }

                    {
                        var response = new NPMResponse();
                        response.error = e.Message;
                        return response;
                    }
                }
            }
        }
    }
}
