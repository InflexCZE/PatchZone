using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ModestTree;
using PatchZone.Core.Utils;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using PatchZone.Utils;
using Service;

namespace PatchZone.Patcher
{
    static partial class ServicePatcher
    {
        private const BindingFlags InstanceBinding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static Type ProxyServiceType = typeof(ProxyService<,>);

        public static object InstantiatePatch(Type proxyType, List<object> baseCascade)
        {
            var (baseTypes, baseMethodSets) = BuildBaseCascadeInfo(baseCascade);

            var patchTargets = new Dictionary<MethodInfo, MethodInfo>();
            foreach(var method in proxyType.GetMethods(InstanceBinding))
            {
                var patchInfo = method.GetCustomAttribute<LogicProxyAttribute>();
                if(patchInfo == null)
                    continue;

                var patchTarget = GetPatchTarget();
                patchTargets.Add(patchTarget, method);

                MethodInfo GetPatchTarget()
                {
                    var t = method.FindMatchingMethod(baseMethodSets, true, (real, general) =>
                    {
                        if(real == general)
                            return true;

                        if(real.IsClass && general == typeof(object))
                            return true;

                        if(real.IsByRef && (general == typeof(IntPtr) || general == typeof(void*)))
                            return true;

                        return false;
                    });

                    //Patch real implementation, not V-table stub
                    return t.GetDeclaredMember();
                }
            }

            var (fullInterface, proxyServiceBaseType) = GetProxyServiceInfo(proxyType);
            var proxyServiceInstanceProperty = proxyServiceBaseType.BaseType.GetProperty(nameof(ProxyService<object, object>.Instance));
            
            var redirectedMethods = new Dictionary<MethodInfo, MethodInfo>();
            foreach(var (target, redirectionTarget) in patchTargets)
            {
                var copy = CreateDuplicate(target);
                redirectedMethods.Add(target, copy);

                var args = target.GetEffectiveParameterTypes();
                var redirectionStub = new DynamicMethod(target.Name + "_" + proxyType.Name + "_DetourStub", target.ReturnType, args);
                var IL = redirectionStub.GetILGenerator();

                if(redirectionTarget.IsStatic == false)
                {
                    IL.Emit(OpCodes.Call, proxyServiceInstanceProperty.GetMethod);
                }

                var offset = target.IsStatic ? 0 : 1;
                for(int i = 0; i < args.Length - offset; i++)
                {
                    IL.Emit(OpCodes.Ldarg, i + offset);
                }

                IL.Emit(OpCodes.Call, redirectionTarget);
                IL.Emit(OpCodes.Ret);

                RedirectMethod(target, redirectionStub);
            }

            object interfaceInstance;
            if(redirectedMethods.Count == 0 && baseTypes[0].Implements(fullInterface))
            {
                interfaceInstance = baseCascade[0];
            }
            else
            {
                var implementationTargets = new List<(MethodInfo Interface, int BaseIndex, MemberInfo ImplementationTarget)>();
                foreach(var method in fullInterface.GetAllInterfaceMethods())
                {
                    for(var i = 0; i < baseTypes.Length; i++)
                    {
                        var baseType = baseTypes[i];
                        var implementation = baseType.TryFindImplementation(method, out var isInterfaceImplementation);

                        if(implementation != null)
                        {
                            if(redirectedMethods.TryGetValue(implementation, out var redirection))
                            {
                                //Direct call of duplicated method
                                implementation = redirection;
                            }
                            else if(implementation.IsAssemblyPublic())
                            {
                                //Direct call of implementing method is best choice, if accessible
                                //=> No op
                            }
                            else if(isInterfaceImplementation && method.IsAssemblyPublic())
                            {
                                //Direct interface call is second best choice
                                implementation = method;
                            }
                            else
                            {
                                //Fall-back to delegate proxy call to expose internal method
                                //=> No op
                            }
                            
                            implementationTargets.Add((method, i, implementation));
                            goto matchFound;
                        }
                    }

                    if(method.IsProperty())
                    {
                        string dataTargetName = method.Name.Substring("get_".Length);
                        for(var i = 0; i < baseTypes.Length; i++)
                        {
                            var baseType = baseTypes[i];
                            var dataTarget = TryFindDataTarget(baseType, method, dataTargetName);
                            
                            if(dataTarget != null)
                            {
                                implementationTargets.Add((method, i, dataTarget));
                                goto matchFound;
                            }
                        }
                    }

                    throw new Exception("Could not find implementation or data target for " + method);
                    matchFound:;
                }

                var proxy = DynamicTypeBuilder.Create($"{fullInterface.Name}_" + proxyType.Name + "_Proxy");
                proxy.Type.AddInterfaceImplementation(fullInterface);

                var baseIndexToField = new Dictionary<int, FieldBuilder>();
                foreach(var (_, baseIndex, _) in implementationTargets)
                {
                    if(baseIndexToField.ContainsKey(baseIndex))
                        continue;
                    
                    var field = proxy.DeclareField(baseCascade[baseIndex].GetType());
                    baseIndexToField.Add(baseIndex, field);
                }

                var proxyDelegates = new List<(Delegate Instance, FieldBuilder Field)>();
                foreach(var (interfaceTarget, baseIndex, implementationTarget) in implementationTargets)
                {
                    var method = proxy.Type.DefineMethod(interfaceTarget.Name, MethodAttributes.Public | MethodAttributes.Virtual);
                    var IL = method.GetILGenerator();

                    var argTypes = interfaceTarget.GetParameters().Select(x => x.ParameterType).ToArray();
                    method.SetParameters(argTypes);

                    var returnType = interfaceTarget.ReturnType;
                    method.SetReturnType(returnType);

                    proxy.Type.DefineMethodOverride(method, interfaceTarget);

                    if(implementationTarget is MethodInfo methodTarget)
                    {
                        EmitCallTarget(methodTarget);
                    }
                    else if(implementationTarget is FieldInfo fieldTarget)
                    {
                        var il = IL;
                        var isInstanceField = fieldTarget.IsStatic == false;

                        DynamicMethod proxyStub = null;
                        if(fieldTarget.IsAssemblyPublic())
                        {
                            if(isInstanceField)
                            {
                                LoadBaseInstance();
                            }
                        }
                        else
                        {
                            //Note: Nasty hack follows!
                            //Private field, dynamic type visibility rules, hacky solution, read below
                            var declaringType = fieldTarget.DeclaringType;
                            var args = argTypes.Prepend(new[] {declaringType}).ToArray();
                            var stubName = fieldTarget.Name + '_' + returnType.Name +  "_ProxyAccessor";
                            proxyStub = new DynamicMethod(stubName, returnType, args, declaringType, true);
                            il = proxyStub.GetILGenerator();

                            if(isInstanceField)
                            {
                                il.Emit(OpCodes.Ldarg_0);
                            }
                        }

                        if (returnType == typeof(void))
                        {
                            var op = isInstanceField ? OpCodes.Stfld : OpCodes.Stsfld;
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(op, fieldTarget);
                        }
                        else
                        {
                            var op = isInstanceField ? OpCodes.Ldfld : OpCodes.Ldsfld;
                            il.Emit(op, fieldTarget);
                        }

                        if(proxyStub != null)
                        {
                            il.Emit(OpCodes.Ret);
                            EmitCallTarget(proxyStub);
                        }
                    }
                    else
                    {
                        throw new Exception("Unexpected implementation target " + implementationTarget.GetType());
                    }
                    
                    IL.Emit(OpCodes.Ret);

                    void LoadBaseInstance()
                    {
                        IL.Emit(OpCodes.Ldarg_0);
                        IL.Emit(OpCodes.Ldfld, baseIndexToField[baseIndex]);
                    }

                    void PassArgs()
                    {
                        for (int i = 1; i <= argTypes.Length; i++)
                        {
                            IL.Emit(OpCodes.Ldarg, i);
                        }
                    }

                    void EmitCallTarget(MethodInfo callTarget)
                    {
                        //TODO: For some reason Mono is super unhappy when calling IL.Emit(Call, DynamicMethod)
                        //      => Proxy via delegate which is apparently fine
                        bool forceDelegateProxy = callTarget is DynamicMethod;

                        if (forceDelegateProxy || callTarget.IsAssemblyPublic() == false)
                        {
                            //Note: Nasty hack follows!
                            //Dynamic type has to follow all visibility rules normal assembly would have to so it can't call private methods directly
                            //Runtime delegates can reflect, call and expose private members, but can't instantiate interface
                            //=> Call proxy delegate inside proxy type to expose private method via public interface. We need to go deeper :)
                            var delegateType = DelegateCreator.NewDelegateType(callTarget, unboundInstanceCall: true);
                            //var proxyDelegate = Delegate.CreateDelegate(delegateType, callTarget);
                            var proxyDelegate = callTarget.CreateDelegate(delegateType);

                            var delegateField = proxy.DeclareField(delegateType);

                            IL.Emit(OpCodes.Ldarg_0);
                            IL.Emit(OpCodes.Ldfld, delegateField);

                            proxyDelegates.Add((proxyDelegate, delegateField));
                            callTarget = delegateType.GetMethod("Invoke");
                        }

                        LoadBaseInstance();
                        PassArgs();

                        var isInterfaceCall = callTarget.DeclaringType?.IsInterface ?? false;
                        IL.Emit(isInterfaceCall ? OpCodes.Callvirt : OpCodes.Call, callTarget);
                    }
                }

                proxy.DeclareCtorAndInitFields
                (
                    proxyDelegates.Select(x => x.Field).Concat(
                    baseIndexToField.Select(x => x.Value))
                );

                interfaceInstance = proxy.Instantiate
                (
                    proxyDelegates.Select(x => x.Instance).Concat(
                    baseIndexToField.Select(x => baseCascade[x.Key]))
                    .ToArray()
                );
            }

            var proxyInstance = Activator.CreateInstance(proxyType);
            proxyType.GetProperty(nameof(ProxyService<object,object>.Vanilla)).SetValue(proxyInstance, interfaceInstance);
            return proxyInstance;
        }

        private static (Type ProxyInterface, Type ProxyServiceType) GetProxyServiceInfo(Type proxyType)
        {
            var parent = proxyType.BaseType;
            while(true)
            {
                if(parent == null)
                {
                    throw new Exception(proxyType.FullName + " needs to derive from " + ProxyServiceType);
                }

                if(parent.IsGenericType)
                {
                    if(parent.GetGenericTypeDefinition() == ProxyServiceType)
                    {
                        return (parent.GetGenericArguments()[1], parent);
                    }
                }

                parent = parent.BaseType;
            }
        }

        private static (Type[], MethodInfo[][]) BuildBaseCascadeInfo(List<object> patchTargets)
        {
            var baseTypes = patchTargets.Select(x => x.GetType()).ToArray();
            var baseMethodSets = baseTypes.Select(x => x.GetMethods(InstanceBinding)).ToArray();

            return (baseTypes, baseMethodSets);
        }

        private static MemberInfo TryFindDataTarget(Type implType, MethodInfo target, string targetName)
        {
            Type targetType;
            var isGetter = target.ReturnType != typeof(void);

            if(isGetter)
            {
                targetType = target.ReturnType;
            }
            else
            {
                targetType = target.GetParameters()[0].ParameterType;
            }

            List<MemberInfo> strongMatches = null, weakMatches = null;
            foreach (var member in implType.GetMembers(ReflectionExtensions.BindAnything))
            {
                if (member is FieldInfo field)
                {
                    ProcessMember(field.FieldType, field.Name);
                }

                if (member is PropertyInfo property)
                {
                    ProcessMember(property.PropertyType, property.Name);
                }

                void ProcessMember(Type type, string name)
                {
                    if (type != targetType)
                        return;

                    if (name == targetName)
                    {
                        Add(ref strongMatches);
                    }
                    else if (name.Equals(targetName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(ref weakMatches);
                    }
                }

                void Add(ref List<MemberInfo> list)
                {
                    if (list == null)
                    {
                        list = new List<MemberInfo>();
                    }

                    list.Add(member);
                }
            }

            if (strongMatches?.Count == 1)
            {
                return strongMatches[0];
            }

            if (strongMatches?.Count > 1)
            {
                throw new Exception($"Ambiguous strong matches for property target {target}");
            }

            if (weakMatches?.Count == 1)
            {
                return weakMatches[0];
            }

            if (strongMatches?.Count > 1)
            {
                throw new Exception($"Ambiguous weak matches for property target {target}");
            }

            return null;
        }
    }
}
