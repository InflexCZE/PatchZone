using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PatchZone.Utils
{
    class DynamicTypeBuilder
    {
        private static ModuleBuilder DynamicModuleCache;
        private static AssemblyBuilder DynamicAssemblyCache;

        public static TypeBuilder CreateDynamicType(string name, Type baseType = null)
        {
            if (DynamicAssemblyCache == null)
            {
                DynamicAssemblyCache = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("PatchZone.Dynamic"), AssemblyBuilderAccess.Run);
                DynamicModuleCache = DynamicAssemblyCache.DefineDynamicModule("PatchZone.Dynamic.Module");
            }

            var type = DynamicModuleCache.DefineType(name);
            type.SetParent(baseType ?? typeof(object));
            return type;
        }

        public static DynamicTypeBuilder Create(string name)
        {
            return new DynamicTypeBuilder(CreateDynamicType(name));
        }

        public TypeBuilder Type { get; }

        public DynamicTypeBuilder(TypeBuilder type)
        {
            this.Type = type;
        }

        public FieldBuilder DeclareField(Type type, string name = null)
        {
            if(name == null)
            {
                name = type.Name + '_' + this.Type.DeclaredFields.Count();
            }

            return this.Type.DefineField(name, type, FieldAttributes.Public);
        }
        
        public void DeclareCtorAndInitFields(IEnumerable<FieldBuilder> fields)
        {
            var fieldTypes = fields.Select(x => x.FieldType).ToArray();
            var constructor = this.Type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, fieldTypes);
            var IL = constructor.GetILGenerator();
            
            //Init base
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Call, this.Type.BaseType.GetConstructor(System.Type.EmptyTypes));

            int i = 0;
            foreach(var field in fields)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldarg, i + 1);
                IL.Emit(OpCodes.Stfld, field);
                i++;
            }

            IL.Emit(OpCodes.Ret);
        }

        public object Instantiate(object[] args)
        {
            var runtimeType = this.Type.CreateType();
            return Activator.CreateInstance(runtimeType, args);
        }
    }
}
