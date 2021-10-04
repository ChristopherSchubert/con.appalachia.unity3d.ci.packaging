using System;
using System.Collections.Generic;
using Appalachia.CI.Packaging.PackageRegistry.Core;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Appalachia.CI.Packaging.PackageRegistry.UI
{
    internal class UpgradePackagesView : EditorWindow
    {
        private readonly Dictionary<PackageInfo, bool> upgradeList = new();
        private UpgradePackagesManager manager;

        private Vector2 scrollPos;

        private bool upgradeAll;

        private void OnEnable()
        {
            manager = new UpgradePackagesManager();

            minSize = new Vector2(640, 320);
            upgradeAll = false;
        }

        private void OnDisable()
        {
            manager = null;
        }

        private void OnGUI()
        {
            if (manager != null)
            {
                manager.Update();

                EditorGUILayout.LabelField("Upgrade packages", EditorStyles.whiteLargeLabel);

                if (manager.packagesLoaded)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    EditorGUI.BeginChangeCheck();
                    upgradeAll = EditorGUILayout.ToggleLeft("Upgrade all packages", upgradeAll);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var info in manager.UpgradeablePackages)
                        {
                            upgradeList[info] = upgradeAll;
                        }
                    }

                    foreach (var info in manager.UpgradeablePackages)
                    {
                        Package(info);
                    }

                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Upgrade"))
                    {
                        Upgrade();
                        CloseWindow();
                    }

                    if (GUILayout.Button("Close"))
                    {
                        CloseWindow();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("Loading packages...", EditorStyles.whiteLargeLabel);
                }
            }
        }

        [MenuItem("Packages/Upgrade Packages", false, 23)]
        internal static void ManageRegistries()
        {
            GetWindow<UpgradePackagesView>(true, "Upgrade packages", true);
        }

        private void Package(PackageInfo info)
        {
            var boxStyle = new GUIStyle();
            boxStyle.padding = new RectOffset(10, 10, 0, 0);

            EditorGUILayout.BeginHorizontal(boxStyle);

            EditorGUI.BeginChangeCheck();

            var upgrade = false;
            if (upgradeList.ContainsKey(info))
            {
                upgrade = upgradeList[info];
            }

            upgrade = EditorGUILayout.BeginToggleGroup(
                info.displayName + ":" + info.version,
                upgrade
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (!upgrade)
                {
                    upgradeAll = false;
                }
            }

            upgradeList[info] = upgrade;

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.LabelField(manager.GetLatestVersion(info));

            EditorGUILayout.EndHorizontal();
        }

        private void Upgrade()
        {
            if (manager != null)
            {
                EditorUtility.DisplayProgressBar("Upgrading packages", "Starting", 0);

                var output = "";
                var failures = false;
                try
                {
                    foreach (var info in manager.UpgradeablePackages)
                    {
                        if (upgradeList[info])
                        {
                            EditorUtility.DisplayProgressBar(
                                "Upgrading packages",
                                "Upgrading " + info.displayName,
                                0.5f
                            );

                            var error = "";
                            if (manager.UpgradePackage(info, ref error))
                            {
                                output += "[Success] Upgraded " +
                                          info.displayName +
                                          Environment.NewLine;
                            }
                            else
                            {
                                output += "[Error] Failed upgrade of" +
                                          info.displayName +
                                          " with error: " +
                                          error +
                                          Environment.NewLine;
                                failures = true;
                            }
                        }
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                string message;
                if (failures)
                {
                    message = "Upgraded with errors." + Environment.NewLine + output;
                }
                else
                {
                    message = "Upgraded all packages. " + Environment.NewLine + output;
                }

                EditorUtility.DisplayDialog("Upgrade finished", message, "OK");
            }
        }

        private void CloseWindow()
        {
            Close();
            GUIUtility.ExitGUI();
        }
    }
}
