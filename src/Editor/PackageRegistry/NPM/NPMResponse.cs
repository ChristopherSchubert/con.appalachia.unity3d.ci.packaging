using System;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
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
