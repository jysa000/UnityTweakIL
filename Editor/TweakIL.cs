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


    // 에디터 DLL 수정
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
            // Assembly-CSharp.dll 경로 설정 
            string assemblyPath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies/Assembly-CSharp.dll");

            var resolver = new DefaultAssemblyResolver();

            // Unity의 기본 어셈블리 경로 추가
            string unityManagedPath = Path.Combine(EditorApplication.applicationContentsPath, "Managed");
            resolver.AddSearchDirectory(unityManagedPath);

            // Unity의 MonoBleedingEdge 경로 추가
            string monoBleedingEdgePath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono/unityjit-win32");
            resolver.AddSearchDirectory(monoBleedingEdgePath);

            // UnityEngine 및 UnityEditor 경로 추가
            string unityEditorManagedPath = Path.Combine(EditorApplication.applicationContentsPath, "Managed/UnityEngine");
            resolver.AddSearchDirectory(unityEditorManagedPath);

            // 프로젝트의 어셈블리 경로 추가
            string projectAssembliesPath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies");
            resolver.AddSearchDirectory(projectAssembliesPath);

            // System.dll 추가를 위한 경로 확인 및 출력
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
                    Debug.Log("[TweakIL] play mode 에서는 assembly update를 수행하지 않습니다");
                    return;
                }

                // 기존 DLL 파일을 삭제하고, modified DLL 파일로 대체합니다. (이 때 유니티가 자체적으로 평가하는 DLL 무결성 체크에서 Broken이 발생합니다)
                File.Delete(assemblyPath);
                File.Copy(assemblyPath + ".modified", assemblyPath);

                // 파일의 타임 스탬프를 바꾸고
                File.SetLastWriteTime(assemblyPath, DateTime.Now);
                // Refresh를 요청하여 해당 DLL 파일이 바뀌었음을 고지하여 DLL 무결성을 회복합니다
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                Debug.Log("[TweakIL] assembly modified");
            }
        }

        private static void AddModifiedFlag(AssemblyDefinition assembly)
        {
            // 리소스 추가
            var resource = new EmbeddedResource("ModifiedFlag.txt", ManifestResourceAttributes.Private, System.Text.Encoding.UTF8.GetBytes("Modified by TweakIL"));
            assembly.MainModule.Resources.Add(resource);
        }
        private static bool ConatinsModifiedFlag(AssemblyDefinition assembly)
        {
            return assembly.MainModule.Resources.Any(r => r.Name == "ModifiedFlag.txt");
        }





    }


    // 빌드 시점 DLL 수정
}