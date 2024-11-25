using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DominoGames.TweakIL
{
    // TweakIL �� �ʿ��� Util �Լ���

    public static class TweakILUtil
    {
        // ITweakWeaver interface�� ã�� Acivate ��
        public static List<ITweakILWeaver> ActivateAllImplementingClasses()
        {
            List<ITweakILWeaver> result = new();

            // ã�� �������̽� Ÿ�� ����
            Type interfaceType = typeof(ITweakILWeaver); // ������ �������̽� Ÿ��

            // ���� ��ũ��Ʈ ���� ��θ� ���� ��
            string currentScriptPath = GetCurrentScriptFolderPath();

            // MonoScript���� �������̽� ���� Ŭ���� �˻�
            List<Type> implementingTypes = GetAllImplementingTypesInPath(interfaceType, currentScriptPath + "/Weavers");

            // �˻��� Ÿ�� Ȱ��ȭ
            foreach (var type in implementingTypes)
            {
                // �Ϲ� Ŭ������ ��� Reflection�� ���� ����
                result.Add(Activator.CreateInstance(type) as ITweakILWeaver);
                Debug.Log($"[TweakIL] Activated Weaver instance of {type.Name}.");
            }

            return result;
        }

        /// <summary>
        /// Ư�� ��� ������ Ư�� �������̽��� �����ϴ� ��� Ŭ���� ã��
        /// </summary>
        private static List<Type> GetAllImplementingTypesInPath(Type interfaceType, string path)
        {
            List<Type> results = new List<Type>();

            // Ư�� ��ο��� ��� ��ũ��Ʈ �˻�
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { path });
            foreach (var guid in guids)
            {
                // GUID�� ��η� ��ȯ
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // MonoScript ��������
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null) continue;

                // ��ũ��Ʈ�� Ÿ�� ��������
                Type type = script.GetClass();
                if (type == null) continue;

                // �������̽��� �����ߴ��� Ȯ��
                if (interfaceType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    results.Add(type);
                }
            }

            return results;
        }
        /// <summary>
        /// ���� ���� ���� ��ũ��Ʈ�� ���� ��θ� ������
        /// </summary>
        private static string GetCurrentScriptFolderPath()
        {
            // ���� Ŭ���� �̸����� MonoScript ã��
            string scriptName = nameof(TweakIL);
            string[] guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");

            if (guids.Length == 0) return null;

            // GUID�� ��η� ��ȯ
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (string.IsNullOrEmpty(scriptPath)) return null;

            // ���� ��� ��ȯ
            return System.IO.Path.GetDirectoryName(scriptPath).Replace("\\", "/");
        }
    }
}