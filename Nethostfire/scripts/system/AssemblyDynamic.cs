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

        public static void Remove(string libName, string typeName, MethodData? methodData = null){
            var AssemblyName = typeName + (methodData.HasValue ? $".{methodData.Value.MethodName}" : "");
            ListAssemblyDynamic.TryRemove(AssemblyName, out _);
        }

        public static dynamic? Get(string libName, string typeName, MethodData? methodData = null){
            typeName = $"{libName}.{typeName}";
            var AssemblyName = typeName + (methodData.HasValue ? $".{methodData.Value.MethodName}" : "");

            dynamic? Dynamic = null;
            ListAssemblyDynamic.TryGetValue(AssemblyName, out Dynamic);
            if(methodData == null && Dynamic != null)
                return Dynamic;

            var executingAssembly = Assembly.GetExecutingAssembly();

            // AssemblyResolve
            if(Dynamic == null)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>{
                var assemblyName = new AssemblyName(args.Name).Name;
                var resourceName = executingAssembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith($"{assemblyName}.dll"));

                if(resourceName != null){
                    using var stream = executingAssembly.GetManifestResourceStream(resourceName);
                    if(stream != null){
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return Assembly.Load(data);
                    }
                }
                return null;
            };

            // Load main DLL
            var dllResourceName = executingAssembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith($"{libName}.dll"));
            
            if(dllResourceName != null){
                using var stream = executingAssembly.GetManifestResourceStream(dllResourceName);
                if(stream != null){
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Dynamic = Assembly.Load(data);
                }
            }

            if(Dynamic == null)
                throw new Nethostfire($"Could not load DLL {libName}");
            
            // Rest of the code (static methods or instantiation)
            if(methodData.HasValue && Dynamic.GetType(typeName) is Type typeStatic && typeStatic != null && typeStatic.IsAbstract && typeStatic.IsSealed){
                var metodoGenerico = typeStatic.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == methodData.Value.MethodName
                        && !m.IsGenericMethod
                        && m.GetParameters().Length == 1);

                if(metodoGenerico != null){
                    try{
                        Dynamic = metodoGenerico.IsGenericMethod ? metodoGenerico.MakeGenericMethod(typeof(object)).Invoke(null, methodData.Value.Params) : metodoGenerico.Invoke(null, methodData.Value.Params);
                    }catch{
                        //throw new Nethostfire($"Error invoking method {methodData.Value.MethodName}: {ex.InnerException?.Message}", ex.InnerException);
                    }
                }
            }else{
                var type = Dynamic.GetType(typeName);
                if(type == null)
                    throw new Nethostfire($"Type {typeName} not found in DLL {libName}");

                try{
                    // Verifica se o tipo tem um construtor sem parâmetros
                    if(type.GetConstructor(Type.EmptyTypes) == null)
                        throw new Nethostfire($"Type {typeName} does not have a parameterless constructor.");

                    Dynamic = Activator.CreateInstance(type);
                }catch (TargetInvocationException ex){
                    throw new Nethostfire($"Error instantiating {typeName}: {ex.InnerException?.Message}", ex.InnerException);
                }
            }

            if(Dynamic != null)
                ListAssemblyDynamic.TryAdd(AssemblyName, Dynamic);

            return Dynamic;
        }
    }
}