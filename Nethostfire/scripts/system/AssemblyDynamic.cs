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
            dynamic? Dynamic = null;

            if(!ListAssemblyDynamic.TryGetValue(libName, out Dynamic)){
                var assembly = Assembly.GetExecutingAssembly();

                // Lembre-se de checar se a assembly já foi carregado!!
                foreach(var resourceNames in assembly.GetManifestResourceNames().Where(item => item.Contains(libName + ".dll"))){
                    if(assembly.GetManifestResourceStream(resourceNames) is Stream stream && stream != null){
                        byte[] data = new byte[stream.Length];
                        stream?.Read(data, 0, data.Length);
                        assembly = Assembly.Load(data);
                    }
                }

                if(methodData.HasValue && assembly.GetType(typeName) is Type typeStatic && typeStatic != null && typeStatic.IsAbstract && typeStatic.IsSealed){
                    MethodInfo? metodoGenerico = typeStatic.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == methodData.Value.MethodName 
                        && !m.IsGenericMethod // Exclui a versão genérica
                        && m.GetParameters().Length == 1)
                    .FirstOrDefault();

                    if(metodoGenerico!.IsGenericMethod){
                        Dynamic = metodoGenerico.MakeGenericMethod(typeof(object)).Invoke(null, methodData.Value.Params);
                    }else{
                        Dynamic = metodoGenerico.Invoke(null, methodData.Value.Params);
                    }
                }else
                    Dynamic = assembly.CreateInstance(typeName);

                ListAssemblyDynamic.TryAdd(libName+typeName, assembly);
            }

            return Dynamic;
        }
    }
}