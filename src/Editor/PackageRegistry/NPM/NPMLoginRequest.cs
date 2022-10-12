using System;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.NPM
{
    [Serializable]
    internal class NPMLoginRequest
    {
        public string name;
        public string password;
    }
}