using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DominoGames.TweakIL
{
    // TweakIL 에 필요한 Util 함수들

    public static class TweakILUtil
    {
        // ITweakWeaver interface를 찾고 Acivate 함
        public static List<ITweakILWeaver> ActivateAllImplementingClasses()
        {
            List<ITweakILWeaver> result = new();

            // 찾을 인터페이스 타입 설정
            Type interfaceType = typeof(ITweakILWeaver); // 구현할 인터페이스 타입

            // 현재 스크립트 폴더 경로를 가져 옴
            string currentScriptPath = GetCurrentScriptFolderPath();

            // MonoScript에서 인터페이스 구현 클래스 검색
            List<Type> implementingTypes = GetAllImplementingTypesInPath(interfaceType, currentScriptPath + "/Weavers");

            // 검색된 타입 활성화
            foreach (var type in implementingTypes)
            {
                // 일반 클래스의 경우 Reflection을 통해 생성
                result.Add(Activator.CreateInstance(type) as ITweakILWeaver);
                Debug.Log($"[TweakIL] Activated Weaver instance of {type.Name}.");
            }

            return result;
        }

        /// <summary>
        /// 특정 경로 내에서 특정 인터페이스를 구현하는 모든 클래스 찾기
        /// </summary>
        private static List<Type> GetAllImplementingTypesInPath(Type interfaceType, string path)
        {
            List<Type> results = new List<Type>();

            // 특정 경로에서 모든 스크립트 검색
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { path });
            foreach (var guid in guids)
            {
                // GUID를 경로로 변환
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // MonoScript 가져오기
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null) continue;

                // 스크립트의 타입 가져오기
                Type type = script.GetClass();
                if (type == null) continue;

                // 인터페이스를 구현했는지 확인
                if (interfaceType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    results.Add(type);
                }
            }

            return results;
        }
        /// <summary>
        /// 현재 실행 중인 스크립트의 폴더 경로를 가져옴
        /// </summary>
        private static string GetCurrentScriptFolderPath()
        {
            // 현재 클래스 이름으로 MonoScript 찾기
            string scriptName = nameof(TweakIL);
            string[] guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");

            if (guids.Length == 0) return null;

            // GUID를 경로로 변환
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (string.IsNullOrEmpty(scriptPath)) return null;

            // 폴더 경로 반환
            return System.IO.Path.GetDirectoryName(scriptPath).Replace("\\", "/");
        }
    }
}