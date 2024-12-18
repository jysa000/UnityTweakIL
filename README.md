# UnityTweakIL

Allows injecting IL instructions into Unity's Assembly-CSharp.dll

You can create weavers that implements the ITweakILWeaver interface to modify assemblies using Mono.Cecil.


## Tutorial

Basically, "OnEntry()" injection is already implemented in the project.

1. You can add the attribute [MethodOnEntry] to any method in a class that inherits from MonoBehaviour.
2. When the method is executed, "gameObjectName => ClassName :: methodName" will be logged first (via MethodOnEntry.OnEntry()).
3. After that, the original method will execute as usual.

++ You can create a class that inherits from MethodOnEntry to extend the OnEntry functionlity. OnMethodEntryWeaver will automatically detect the child classes and work seamlessly.



## Structure

* On editor compilation finished => Editor/TweakIL.TryModifyAssembly()
    * Load assembly files
    * Load and execute weavers
    * Write to Assembly-CSharp.dll



## Restriction

* The weavers' file path is predefined and cannot be modified.
* The predefined System.dll path may not be compatible with all devices. Please verify the path to System.dll.