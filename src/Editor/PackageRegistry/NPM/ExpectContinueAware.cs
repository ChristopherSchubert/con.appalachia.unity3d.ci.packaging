using System;
using System.Net;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    public class ExpectContinueAware : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                var hwr = request as HttpWebRequest;
                hwr.ServicePoint.Expect100Continue = false;
                hwr.AllowAutoRedirect = false;
            }

            return request;
        }
    }
}