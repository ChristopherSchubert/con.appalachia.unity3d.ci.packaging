﻿using Appalachia.CI.Packaging.Editor.PackageRegistry.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Appalachia.CI.Packaging.Editor.PackageRegistry.UI
{
    public class RegistryManagerView : EditorWindow
    {
        private ReorderableList drawer;

        private void OnEnable()
        {
            drawer = GetRegistryList(new RegistryManager());
            minSize = new Vector2(640, 320);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scoped registries", EditorStyles.whiteLargeLabel);
            drawer.DoLayoutList();
        }

        [MenuItem("Packages/Manage scoped registries", false, 21)]
        internal static void ManageRegistries()
        {
            SettingsService.OpenProjectSettings("Project/Package Manager/Credentials");
        }

        internal static ReorderableList GetRegistryList(RegistryManager registryManager)
        {
            ReorderableList registryList = null;
            registryList =
                new ReorderableList(
                    registryManager.registries,
                    typeof(ScopedRegistry),
                    false,
                    true,
                    true,
                    true
                )
                {
                    drawHeaderCallback = rect =>
                    {
                        GUI.Label(rect, "Scoped Registries in this project");
                    },
                    elementHeight = 70,
                    drawElementCallback = (rect, index, active, focused) =>
                    {
                        var registry = registryList.list[index] as ScopedRegistry;
                        if (registry == null)
                        {
                            return;
                        }

                        var registryHasAuth = !string.IsNullOrEmpty(registry.token) &&
                                              registry.isValidCredential();

                        var rect2 = rect;
                        rect.width -= 60;
                        rect.height = 20;
                        GUI.Label(
                            rect,
                            registry.name + " (" + registry.scopes.Count + " scopes)",
                            EditorStyles.boldLabel
                        );
                        rect.y += 20;
                        GUI.Label(rect, registry.url);
                        rect.y += 20;
                        EditorGUI.BeginDisabledGroup(true);
                        GUI.Toggle(rect, registryHasAuth, "Has Credentials");
                        EditorGUI.EndDisabledGroup();

                        rect2.x = rect2.xMax - 60;
                        rect2.height = 20;
                        rect2.width = 60;
                        rect2.y += 5;
                        if (GUI.Button(rect2, "Edit"))
                        {
                            var registryEditor = GetWindow<ScopedRegistryEditorView>(
                                true,
                                "Edit registry",
                                true
                            );
                            registryEditor.Edit(registry, registryManager);
                        }
                    },
                    onAddCallback = list =>
                    {
                        var registryEditor =
                            GetWindow<ScopedRegistryEditorView>(true, "Add registry", true);
                        registryEditor.CreateNew(registryManager);
                    },
                    onRemoveCallback = list =>
                    {
                        Debug.Log("index to remove: " + list.index);
                        var entry = list.list[list.index] as ScopedRegistry;
                        registryManager.Remove(entry);
                    }
                };
            return registryList;
        }
    }
}
