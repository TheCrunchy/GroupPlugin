using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace CrunchGroup
{
    //no patchshim because this needs to be ran manually after my scripts are compiled 
    public static class KeenScriptManagerPatch
    {
        public static void ScriptInit(MySession __instance)
        {
            var assemb = typeof(KeenScriptManagerPatch).Assembly;
            var assemblies = new List<Assembly>();
            assemblies.Add(assemb);
            assemblies.AddRange(Core.myAssemblies);
            foreach (var assembly in assemblies)
            {
                Core.Log.Info($"{assembly.FullName}");
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsPublic || !typeof(MyGameLogicComponent).IsAssignableFrom(type))
                        continue;

                    AddEntityScript(__instance.ScriptManager, type);
                }
                __instance.RegisterComponentsFromAssembly(assembly, true);

            }
 
        }

        //mostly copied from keen code
        private static void AddEntityScript(MyScriptManager manager, Type type)
        {
            var objectBuilderType = typeof(MyObjectBuilder_Base);
            var descriptor = type.GetCustomAttribute<MyEntityComponentDescriptor>()
                ?? throw new ArgumentException($"type {type} is missing MyEntityComponentDescriptor");

            if (!objectBuilderType.IsAssignableFrom(descriptor.EntityBuilderType))
                throw new InvalidOperationException($"type {type} has invalid EntityBuilderType {descriptor.EntityBuilderType}");

            manager.TypeToModMap[type] = null;

            if (descriptor.EntityBuilderSubTypeNames != null && descriptor.EntityBuilderSubTypeNames.Length != 0)
            {
                foreach (var text in descriptor.EntityBuilderSubTypeNames)
                {
                    Tuple<Type, string> tuple = new(descriptor.EntityBuilderType, text);
                    if (!manager.SubEntityScripts.TryGetValue(tuple, out var subtypeScripts))
                    {
                        subtypeScripts = new HashSet<Type>();
                        manager.SubEntityScripts.Add(tuple, subtypeScripts);
                    }

                    subtypeScripts.Add(type);
                }

                return;
            }

            if (!manager.EntityScripts.TryGetValue(descriptor.EntityBuilderType, out HashSet<Type> scripts))
            {
                scripts = new HashSet<Type>();
                manager.EntityScripts.Add(descriptor.EntityBuilderType, scripts);
            }
            scripts.Add(type);
        }
    }
}
