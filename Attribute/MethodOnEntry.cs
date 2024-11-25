using System;
using System.Reflection;
using UnityEngine;

namespace DominoGames.TweakIL
{
    public class MethodOnEntry : System.Attribute
    {
        public static void OnEntry(GameObject obj, MethodInfo method)
        {
            Debug.Log($"{obj.name}=>{method.DeclaringType.FullName}::{method.Name} OnEntry");
        }
    }
}