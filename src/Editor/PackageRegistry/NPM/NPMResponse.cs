using System;

namespace Appalachia.CI.Packaging.PackageRegistry.NPM
{
    [Serializable]
    public class NPMResponse
    {
        public string error;
        public string ok;
        public string token;

        public bool success;

        public string reason;
    }
}
