using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

class RuntimeTypeGenerator
{
    public Type GetType(string messageType)
    {
        var knownType = Type.GetType(messageType, false);
        if (knownType != null)
        {
            return knownType;
        }

        var parts = messageType.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var nameAndNamespace = parts[0];
        var assembly = parts.Length > 1 ? parts[1] : unknownAssemblyName;

        ModuleBuilder moduleBuilder;
        lock (assemblies)
        {
            if (!assemblies.TryGetValue(assembly, out moduleBuilder))
            {
                var assemblyName = new AssemblyName(assembly);
                var assemblyBuilder =
                    AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(assembly);
                assemblies[assembly] = moduleBuilder;
            }
        }

        Type result;
        lock (types)
        {
            if (!types.TryGetValue(messageType, out result))
            {
                var nestedParts = nameAndNamespace.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);

                var typeBuilder = GetRootTypeBuilder(moduleBuilder, nestedParts[0]);

                for (var i = 1; i < nestedParts.Length; i++)
                {
                    var path = string.Join("+", nestedParts.Take(i + 1));
                    typeBuilder = GetNestedTypeBuilder(typeBuilder, nestedParts[i], path);
                }

                result = typeBuilder.CreateTypeInfo();
                types[messageType] = result;
            }
        }

        return result;
    }

    TypeBuilder GetRootTypeBuilder(ModuleBuilder moduleBuilder, string name)
    {
        if (typeBuilders.TryGetValue(name, out var builder))
        {
            return builder;
        }

        builder = moduleBuilder.DefineType(name, TypeAttributes.Public);
        typeBuilders[name] = builder;
        builder.CreateTypeInfo();
        return builder;
    }

    TypeBuilder GetNestedTypeBuilder(TypeBuilder typeBuilder, string name, string path)
    {
        if (typeBuilders.TryGetValue(path, out var builder))
        {
            return builder;
        }

        builder = typeBuilder.DefineNestedType(name);
        typeBuilders[path] = builder;
        builder.CreateTypeInfo();
        return builder;
    }

    Dictionary<string, ModuleBuilder> assemblies = new Dictionary<string, ModuleBuilder>();
    Dictionary<string, Type> types = new Dictionary<string, Type>();
    Dictionary<string, TypeBuilder> typeBuilders = new Dictionary<string, TypeBuilder>();
    const string unknownAssemblyName = "NServiceBus.Transport.Bridge.UnknownAssembly";
}