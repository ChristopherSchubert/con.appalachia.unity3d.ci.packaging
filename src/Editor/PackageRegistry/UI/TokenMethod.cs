using Appalachia.CI.Packaging.Editor.PackageRegistry.Core;
using UnityEngine;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.UI
{
    internal class TokenMethod : GUIContent
    {
        internal GetToken action;
        internal string passwordName;
        internal string usernameName;

        public TokenMethod(
            string name,
            string usernameName,
            string passwordName,
            GetToken action) : base(name)
        {
            this.usernameName = usernameName;
            this.passwordName = passwordName;
            this.action = action;
        }

        internal delegate bool GetToken(ScopedRegistry registry, string username, string password);
    }
}