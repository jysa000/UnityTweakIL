using System;
using System.Reflection;
using UnityEngine;

namespace DominoGames.TweakIL
{
    public class MethodOnEntry : System.Attribute
    {
        public static void OnEntry(GameObject obj, MethodInfo method, object[] args)
        {
            var jsonArgs = Newtonsoft.Json.JsonConvert.SerializeObject(args);
            Debug.Log($"{obj.name} => {method.DeclaringType.FullName} :: {method.Name} Called with {jsonArgs}");
        }
    }
}