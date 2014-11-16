using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace CecilScanner
{
    public class Resolver : BaseAssemblyResolver
    {
        private readonly IDictionary<string, AssemblyDefinition> cache;
        public Resolver()
        {
            this.cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
        }
        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assemblyDefinition = null;
            if (this.cache.TryGetValue(name.FullName, out assemblyDefinition))
                return assemblyDefinition;
            try  //< -------- My addition to the code.
            {
                assemblyDefinition = base.Resolve(name);
                this.cache[name.FullName] = assemblyDefinition;
            }
            catch { } //< -------- My addition to the code.
            return assemblyDefinition;
        }
        public void Register(AssemblyDefinition assembly)
        {
            string fullName = assembly.Name.FullName;
            this.cache[fullName] = assembly;
        }
    }
}
