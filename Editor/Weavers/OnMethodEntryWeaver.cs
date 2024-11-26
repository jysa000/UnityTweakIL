using Mono.Cecil;
using UnityEngine;
using System.Linq;
using System;
using Unity.VisualScripting;
using Mono.Cecil.Cil;

namespace DominoGames.TweakIL{

    public class TweakIL_MethodOnEntry : ITweakILWeaver
    {
        public void Execute(TweakILWeaverArgs args)
        {
            ModifyAssembly(args.assembly, args.module, args.assemblyPath);
        }

        private void ModifyAssembly(AssemblyDefinition assembly, ModuleDefinition module, string assemblyPath)
        {
            // 특정 클래스와 메서드 검색
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    foreach(var attr in method.CustomAttributes)
                    {
                        if (IsMethodOnEntryOrDerived(attr.AttributeType))
                        {
                            Debug.Log($"[TweakIL] Injecting Method [{attr.AttributeType.Name}] => {type.Name}.{method.Name}");
                            InjectMethodCall(method, module, attr.AttributeType);
                        }
                    }
                }
            }

            assembly.Write(assemblyPath + ".modified");
        }

        private void InjectMethodCall(MethodDefinition targetMethod, ModuleDefinition module, TypeReference attribute)
        {
            var ilProcessor = targetMethod.Body.GetILProcessor();
            var attributeType = attribute.Resolve();

            // MethodOnEntry.OnEntry(GameObject)
            var onEntryMethod = module.ImportReference(attributeType.Methods.FirstOrDefault(m => m.Name == "OnEntry" && !m.IsAbstract));
            //var onEntryMethod = targetMethod.Module.ImportReference(typeof(MethodOnEntry).GetMethod("OnEntry"));

            // get monobehavour.gameObject property
            var gameObjectProperty = module.ImportReference(
                typeof(MonoBehaviour).GetProperty("gameObject").GetGetMethod()
            );
             
            // System.Type.GetType(string)
            var getTypeMethod = module.ImportReference(
                typeof(System.Type).GetMethod("GetType", new[] { typeof(string) })
            );

            // System.Reflection.MethodInfo
            var getMethod = module.ImportReference(
                typeof(System.Type).GetMethod("GetMethod", new[] { typeof(string), typeof(Type[]) })
            );

            // first instruction to inject
            var firstInstruction = targetMethod.Body.Instructions[0];

            // this.gameObject
            if (targetMethod.DeclaringType.BaseType.FullName == typeof(MonoBehaviour).FullName)
            {
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0)); // this
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Callvirt, gameObjectProperty)); // this.gameObject
            }
            else
            {
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldnull)); // set gameObject to null
            }

            // Type.GetType("Type name") -> GetMethod("Method Name")
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, targetMethod.DeclaringType.FullName)); // Type name
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, getTypeMethod)); // Type.GetType()


            // 매개변수 타입 배열
            var parameterTypes = targetMethod.Parameters.Select(p => module.ImportReference(p.ParameterType)).ToArray();

            // GetMethod("Method Name", parameterTypes)
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldstr, targetMethod.Name)); // Method name

            // 매개변수 타입 배열 생성
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4, parameterTypes.Length)); // 배열 크기
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Newarr, module.ImportReference(typeof(Type))));
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Dup)); // 배열 복사
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4, i)); // 배열 인덱스
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldtoken, parameterTypes[i])); // 매개변수 타입 로드
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")))); // Type.GetTypeFromHandle
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Stelem_Ref)); // 배열에 저장
            }
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Callvirt, getMethod)); // GetMethod 호출

            bool isMethodStatic = targetMethod.IsStatic;

            // parameters
            var parameters = targetMethod.Parameters;

            // 배열 크기 설정
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4, parameters.Count));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Newarr, module.ImportReference(typeof(object))));

            for (int i = 0; i < parameters.Count; i++)
            {
                // 배열 복사
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Dup));
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4, i)); // 배열 인덱스

                // 파라미터 값 로드
                if (isMethodStatic)
                {
                    // static 함수라면 0번 매개변수부터 함수 매개변수임
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldarg, i));
                }
                else
                {
                    // instance method라면 0번 매개변수는 this이므로 제외하고 1번부터
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Ldarg, i + 1));
                }

                // 값 타입은 박싱
                if (parameters[i].ParameterType.IsValueType)
                {
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Box, parameters[i].ParameterType));
                }

                // 배열에 저장
                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Stelem_Ref));
            }


            // MethodOnEntry.OnEntry(GameObject)
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(Mono.Cecil.Cil.OpCodes.Call, onEntryMethod)); // Call static OnEntry
        }



        private bool IsMethodOnEntryOrDerived(TypeReference attributeType)
        {
            // 대상 Attribute의 정의 가져오기
            try
            {
                // 대상 Attribute의 정의 가져오기
                var currentType = attributeType.Resolve(); 

                // 상속 계층 탐색
                while (currentType != null)
                {
                    if (currentType.FullName == "DominoGames.TweakIL.MethodOnEntry") // MethodOnEntry의 풀 네임 확인
                    {
                        return true; // MethodOnEntry 또는 이를 상속받은 경우
                    }

                    currentType = currentType.BaseType?.Resolve(); // 상위 타입으로 이동
                }

                return false;
            }
            catch(Exception)
            {
                // do nothing just skipping
                return false;
            }
        }
    }
}