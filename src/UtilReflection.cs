namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class UtilReflection
    {
        internal static FieldInfo GetFieldFromHierarchy(this Type type, string name)
        {
            FieldInfo result = null;
            Type resultType = null;

            Type curType = type;
            while (curType != null)
            {
                FieldInfo typeField = curType.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (typeField != null)
                {
                    if (result == null)
                    {
                        result = typeField;
                        resultType = curType;
                    }
                    else
                    {
                        Dbg.Err($"Found multiple examples of field named {name} in type hierarchy {type}; found in {resultType} and {curType}");
                    }
                }

                curType = curType.BaseType;
            }
            return result;
        }

        internal static IEnumerable<FieldInfo> GetSerializableFieldsFromHierarchy(this Type type)
        {
            // this probably needs to be cached

            var seenFields = new HashSet<string>();

            Type curType = type;
            while (curType != null)
            {
                foreach (var field in curType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (field.IsBackingField())
                    {
                        continue;
                    }

                    if (field.GetCustomAttribute<IndexAttribute>() != null)
                    {
                        // we don't save indices
                        continue;
                    }

                    if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        // we also don't save nonserialized
                        continue;
                    }

                    if (seenFields.Contains(field.Name))
                    {
                        Dbg.Err($"Found duplicates of field {field}; base fields will be ignored");
                        continue;
                    }

                    yield return field;
                    seenFields.Add(field.Name);
                }
                
                curType = curType.BaseType;
            }
        }

        internal static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }

        internal static bool IsUserAssembly(this Assembly asm)
        {
            var name = asm.FullName;

            // Filter out system libraries
            if (name.StartsWith("mscorlib,") || name.StartsWith("System,") || name.StartsWith("System."))
            {
                return false;
            }

            // Filter out Mono
            if (name.StartsWith("Mono."))
            {
                return false;
            }

            // Filter out nunit, almost entirely so our test results look better
            if (name.StartsWith("nunit.framework,"))
            {
                return false;
            }

            // Filter out Unity
            if (name.StartsWith("Unity.") || name.StartsWith("UnityEngine,") || name.StartsWith("UnityEngine.") || name.StartsWith("UnityEditor,") || name.StartsWith("UnityEditor."))
            {
                return false;
            }

            return true;
        }

        internal static IEnumerable<Assembly> GetAllUserAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.IsUserAssembly());
        }

        internal static IEnumerable<Type> GetAllUserTypes()
        {
            return GetAllUserAssemblies().SelectMany(a => a.GetTypes());
        }

        internal static bool ReflectionSetForbidden(FieldInfo field)
        {
            // Alright, this isn't exactly complicated right now
            if (field.DeclaringType == typeof(Dec))
            {
                return true;
            }

            return false;
        }

        internal struct IndexInfo
        {
            public Type type;
            public FieldInfo field;
        }
        internal static Dictionary<Type, IndexInfo[]> IndexInfoCached = new Dictionary<Type, IndexInfo[]>();
        internal static IndexInfo[] GetIndicesForType(Type type)
        {
            if (IndexInfoCached.TryGetValue(type, out var result))
            {
                // found it in cache, we're done
                return result;
            }

            IndexInfo[] indices = null;

            if (type.BaseType != null)
            {
                indices = GetIndicesForType(type.BaseType);
            }

            FieldInfo matchedField = null;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttribute<IndexAttribute>() != null)
                {
                    if (matchedField != null)
                    {
                        Dbg.Err($"Too many indices in type {type} (found {matchedField} and {field}); only one will be filled");
                    }

                    matchedField = field;
                }
            }

            if (matchedField != null)
            {
                IndexInfo[] indicesWorking;

                if (indices != null)
                {
                    indicesWorking = new IndexInfo[indices.Length + 1];
                    Array.Copy(indices, indicesWorking, indices.Length);
                }
                else
                {
                    indicesWorking = new IndexInfo[1];
                }

                indicesWorking[indicesWorking.Length - 1] = new IndexInfo { type = type, field = matchedField };

                indices = indicesWorking;
            }

            IndexInfoCached[type] = indices;

            return indices;
        }

        internal static bool IsBackingField(this FieldInfo field)
        {
            // I wish I could find something more authoritative on this.
            return field.Name.StartsWith("<");
        }

        internal static object CreateInstanceSafe(this Type type, string errorType, Func<string> errorPrefix)
        {
            if (type.IsAbstract)
            {
                Dbg.Err($"{errorPrefix()}: Attempting to create {errorType} of abstract type {type}");
                return null;    // thankfully all abstract types can accept being null
            }
            else if (!type.IsValueType && type.GetConstructor(new Type[] { }) == null)
            {
                // Note: Structs don't have constructors. I actually can't tell if ints do, I'm kind of bypassing that system.

                Dbg.Err($"{errorPrefix()}: Attempting to create {errorType} of type {type} without a public no-argument constructor");
                return null;    // similarly, anything that is capable of not having a public no-argument constructor can accept being null
            }
            else
            {
                try
                {
                    var result = Activator.CreateInstance(type);
                    if (result == null)
                    {
                        // This is difficult to test; there are very few things that can get CreateInstance to return null, and right now the type system doesn't support them (int? for example)
                        Dbg.Err($"{errorPrefix()}: {errorType} of type {type} was not properly created; this will cause issues");
                    }
                    return result;
                }
                catch (TargetInvocationException e)
                {
                    Dbg.Ex(e);
                    return null;
                }
            }
        }
    }
}
