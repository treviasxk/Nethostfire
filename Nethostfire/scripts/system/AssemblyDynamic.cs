// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Collections.Concurrent;
using System.Reflection;

namespace Nethostfire{
    struct MethodData {
        public string MethodName;
        public object[] Params;
    }

    class AssemblyDynamic{
        static ConcurrentDictionary<string, dynamic> ListAssemblyDynamic = new();
        public static dynamic? Get(string libName, string typeName, MethodData? methodData = null){
            if(ListAssemblyDynamic.TryGetValue(libName + typeName, out dynamic? Dynamic))
                return Dynamic;

        var executingAssembly = Assembly.GetExecutingAssembly();

        // AssemblyResolve
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>{
            var assemblyName = new AssemblyName(args.Name).Name;
            var resourceName = executingAssembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith($"{assemblyName}.dll") || r.Contains(assemblyName));

            if (resourceName != null)
            {
                using var stream = executingAssembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return Assembly.Load(data);
                }
            }
            return null;
        };

        // Load main DLL
        Assembly? assembly = null;
        var dllResourceName = executingAssembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith($"{libName}.dll"));

            if(dllResourceName != null){
                using var stream = executingAssembly.GetManifestResourceStream(dllResourceName);
                if(stream != null){
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    assembly = Assembly.Load(data);
                }
            }

            if(assembly == null)
                throw new Exception($"Could not load DLL {libName}");
            
            // Rest of the code (static methods or instantiation)
            if(methodData.HasValue && assembly.GetType(typeName) is Type typeStatic && typeStatic != null && typeStatic.IsAbstract && typeStatic.IsSealed){
                var metodoGenerico = typeStatic.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == methodData.Value.MethodName
                        && !m.IsGenericMethod
                        && m.GetParameters().Length == 1);

                if(metodoGenerico != null){
                    try{
                        Dynamic = metodoGenerico.IsGenericMethod ? metodoGenerico.MakeGenericMethod(typeof(object)).Invoke(null, methodData.Value.Params) : metodoGenerico.Invoke(null, methodData.Value.Params);
                    }catch(TargetInvocationException ex){
                        throw new Exception($"Error invoking method {methodData.Value.MethodName}: {ex.InnerException?.Message}", ex.InnerException);
                    }
                }
            }else{
                var type = assembly.GetType(typeName);
                if(type == null)
                    throw new Exception($"Type {typeName} not found in DLL {libName}");

                try{
                    Dynamic = Activator.CreateInstance(type);
                }catch (TargetInvocationException ex){
                    throw new Exception($"Error instantiating {typeName}: {ex.InnerException?.Message}", ex.InnerException);
                }
            }

            if(Dynamic != null)
                ListAssemblyDynamic.TryAdd(libName + typeName, Dynamic);

            return Dynamic;
        }
    }
}