using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ModestTree;

namespace PatchZone.Utils
{
    static class ReflectionExtensions
    {
        public const BindingFlags BindAnything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static bool IsProperty(this MethodInfo method)
        {
            return method.IsHideBySig && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"));
        }

        public static MethodInfo FindMatchingMethod(this MethodInfo target, MethodInfo[][] candidates, bool throwOnFail, Func<Type, Type, bool> IsTypeMatching = null)
        {
            for(int i = 0; i < candidates.Length; i++)
            {
                var shouldThrowOnFail = throwOnFail && ((i + 1) == candidates.Length);
                var match = FindMatchingMethod(target, candidates[i], shouldThrowOnFail, IsTypeMatching);

                if(match != null)
                {
                    return match;
                }
            }

            return null;
        }
        
        public static MethodInfo FindMatchingMethod(this MethodInfo target, MethodInfo[] candidates, bool throwOnFail, Func<Type, Type, bool> IsTypeMatching = null)
        {
            if(IsTypeMatching == null)
            {
                IsTypeMatching = (a, b) => a == b;
            }

            var targetName = target.Name;
            var methodParams = target.GetParameters();
            var matches = candidates
                .Where(x => x.Name == targetName)
                .Where(x => IsTypeMatching(target.ReturnType, x.ReturnType))
                .Where(x =>
                {
                    var xParams = x.GetParameters();
                    if(xParams.Length != methodParams.Length)
                        return false;

                    for(int i = 0; i < xParams.Length; i++)
                    {
                        if(IsTypeMatching(xParams[i].ParameterType, methodParams[i].ParameterType) == false)
                        {
                            return false;
                        }
                    }

                    return true;
                });

            using(var enumerator = matches.GetEnumerator())
            {
                if(enumerator.MoveNext() == false)
                {
                    if(throwOnFail)
                    {
                        throw new Exception("No match for " + target);
                    }

                    return null;
                }

                var match = enumerator.Current;

                if(enumerator.MoveNext())
                {
                    throw new AmbiguousMatchException("Ambiguous match for " + target);
                }

                return match;
            }
        }

        public static bool Implements(this Type type, Type @interface)
        {
            return type.Interfaces().Contains(@interface);
        }

        public static IEnumerable<MethodInfo> GetAllInterfaceMethods(this Type @interface)
        {
            var typesToVisit = new Stack<Type>();
            var visitedTypes = new HashSet<Type>();

            visitedTypes.Add(@interface);
            typesToVisit.Push(@interface);
            while (typesToVisit.Count > 0)
            {
                var current = typesToVisit.Pop();
                foreach (var method in current.GetMethods())
                {
                    yield return method;
                }

                foreach (var next in current.GetInterfaces())
                {
                    if (visitedTypes.Add(next))
                    {
                        typesToVisit.Push(next);
                    }
                }
            }
        }

        public static MethodInfo TryFindImplementation(this Type target, MethodInfo method, out bool isInterfaceImplementation)
        {
            var interfaceType = method.DeclaringType;
            
            if(interfaceType.IsInterface == false)
            {
                throw new Exception("Expected interface method");
            }

            if(target.Implements(interfaceType))
            {
                isInterfaceImplementation = true;
                var map = target.GetInterfaceMap(interfaceType);
                var methodIndex = Array.IndexOf(map.InterfaceMethods, method);
                return map.TargetMethods[methodIndex].GetDeclaredMember();
            }

            var match = method.FindMatchingMethod(target.GetMethods(BindAnything), throwOnFail: false);
            
            if(match != null)
            {
                match = match.GetDeclaredMember();
            }

            isInterfaceImplementation = false;
            return match;
        }

        public static bool IsAssemblyPublic(this MemberInfo target)
        {
            if((target is FieldInfo || target is MethodBase) == false)
            {
                throw new Exception("Unexpected member type " + target.GetType());
            }

            if
            (
                target is FieldInfo field && field.IsPrivate ||
                target is MethodBase method && method.IsPrivate
            )
            {
                return false;
            }

            var type = target.DeclaringType;
            while (type != null)
            {
                if (type.IsNotPublic)
                    return false;

                type = type.DeclaringType;
            }

            return true;
        }

        public static Type[] GetEffectiveParameterTypes(this MethodBase method)
        {
            var parameters = method.GetParameters().Select(x => x.ParameterType);

            if (method.IsStatic == false)
            {
                parameters = parameters.Prepend(new []{method.DeclaringType});
            }

            return parameters.ToArray();
        }
    }
}
