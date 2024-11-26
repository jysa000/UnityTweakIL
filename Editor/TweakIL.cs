using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Threading;
using UnityEditor.Compilation;
using System;
using System.Configuration.Assemblies;

namespace DominoGames.TweakIL
{
    public interface ITweakILWeaver {
        public void Execute(TweakILWeaverArgs args);
    }
    public class TweakILWeaverArgs
    {
        public AssemblyDefinition assembly;
        public ModuleDefinition module;
        public string assemblyPath;
    }


    // ������ DLL ����
    [InitializeOnLoad]
    public static class TweakIL
    {
        private static bool canDelayCall = true;
        private static bool attachOnce = true;

        static TweakIL()
        {
            canDelayCall = true;

            if (attachOnce)
            {
                EditorApplication.delayCall += () =>
                {
                    if (canDelayCall)
                    {
                        TryModifyAssembly();
                        canDelayCall = false;
                    }
                };
                attachOnce = false;
            }
        }





        private static void TryModifyAssembly()
        {
            // Assembly-CSharp.dll ��� ���� 
            string assemblyPath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies/Assembly-CSharp.dll");

            var resolver = new DefaultAssemblyResolver();

            // Unity�� �⺻ ����� ��� �߰�
            string unityManagedPath = Path.Combine(EditorApplication.applicationContentsPath, "Managed");
            resolver.AddSearchDirectory(unityManagedPath);

            // Unity�� MonoBleedingEdge ��� �߰�
            string monoBleedingEdgePath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono/unityjit-win32");
            resolver.AddSearchDirectory(monoBleedingEdgePath);

            // UnityEngine �� UnityEditor ��� �߰�
            string unityEditorManagedPath = Path.Combine(EditorApplication.applicationContentsPath, "Managed/UnityEngine");
            resolver.AddSearchDirectory(unityEditorManagedPath);

            // ������Ʈ�� ����� ��� �߰�
            string projectAssembliesPath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies");
            resolver.AddSearchDirectory(projectAssembliesPath);

            // System.dll �߰��� ���� ��� Ȯ�� �� ���
            string systemDllPath = Path.Combine(monoBleedingEdgePath, "System.dll");
            if (!File.Exists(systemDllPath))
            {
                Debug.LogError($"[TweakIL] System.dll not found at path: {systemDllPath}");
                return;
            }

            Debug.Log($"[TweakIL] System.dll found at: {systemDllPath}");
            resolver.AddSearchDirectory(Path.GetDirectoryName(systemDllPath));

            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, AssemblyResolver = resolver });

            if (ConatinsModifiedFlag(assembly))
            {
                Debug.LogWarning($"[TweakIL] Assembly has already modified. skipping");
                assembly.Dispose();
                return;
            }

            var weavers = TweakILUtil.ActivateAllImplementingClasses();
            for(int i = 0; i < weavers.Count; i ++)
            {
                weavers[i].Execute(new TweakILWeaverArgs() { 
                    assembly = assembly,
                    module = assembly.MainModule,
                    assemblyPath = assemblyPath,
                });
            }

            AddModifiedFlag(assembly);

            assembly.Write(assemblyPath + ".modified");
            assembly.Dispose();

            Thread.Sleep(300);

            ReplaceAssemblyFile(assemblyPath);
        }

        private static void ReplaceAssemblyFile(string assemblyPath)
        {
            if (File.Exists(assemblyPath + ".modified"))
            {
                if (EditorApplication.isPlaying)
                {
                    Debug.Log("[TweakIL] play mode ������ assembly update�� �������� �ʽ��ϴ�");
                    return;
                }

                // ���� DLL ������ �����ϰ�, modified DLL ���Ϸ� ��ü�մϴ�. (�� �� ����Ƽ�� ��ü������ ���ϴ� DLL ���Ἲ üũ���� Broken�� �߻��մϴ�)
                File.Delete(assemblyPath);
                File.Copy(assemblyPath + ".modified", assemblyPath);

                // ������ Ÿ�� �������� �ٲٰ�
                File.SetLastWriteTime(assemblyPath, DateTime.Now);
                // Refresh�� ��û�Ͽ� �ش� DLL ������ �ٲ������ �����Ͽ� DLL ���Ἲ�� ȸ���մϴ�
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                Debug.Log("[TweakIL] assembly modified");
            }
        }

        private static void AddModifiedFlag(AssemblyDefinition assembly)
        {
            // ���ҽ� �߰�
            var resource = new EmbeddedResource("ModifiedFlag.txt", ManifestResourceAttributes.Private, System.Text.Encoding.UTF8.GetBytes("Modified by TweakIL"));
            assembly.MainModule.Resources.Add(resource);
        }
        private static bool ConatinsModifiedFlag(AssemblyDefinition assembly)
        {
            return assembly.MainModule.Resources.Any(r => r.Name == "ModifiedFlag.txt");
        }





    }


    // ���� ���� DLL ����
}