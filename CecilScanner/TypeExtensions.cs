using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace CecilScanner
{
    public static class TypeExtensions
    {
        public static string GetSafeFullName(this TypeReference type)
        {
            return type.FullName.Replace("/", ".");
        }

        private static readonly List<PropertyDefinition> Empty = new List<PropertyDefinition>();
        public static List<PropertyDefinition> GetPropertiesInternal(TypeDefinition type)
        {
            if (type == null)
            {
                return Empty;
            }
            var ownProperties = type.Properties.ToList();
            var baseType = type.BaseType;
            if (baseType != null)
            {
                return GetPropertiesWithInheritance(baseType.Resolve())
                    .Union(ownProperties)
                    .ToList();
            }
            return ownProperties;
        }

        public static PropertyDefinition[] GetPropertiesWithInheritance(this TypeDefinition type)
        {
            var properties = GetPropertiesInternal(type);
            // Apply overrides (from the base to inherited)
            var nameLookup = new Dictionary<string, PropertyDefinition>(properties.Count());
            foreach (var property in properties)
            {
                nameLookup[property.Name] = property;
            }
            return nameLookup.Values.ToArray();
        }


        public static bool IsWebAPI(this TypeDefinition type)
        {
            var baseType = type.BaseType;
            if (baseType == null)
            {
                return false;
            }

            if (IsController(baseType))
            {
                return true;
            }

            if (baseType.Scope.Name == type.Scope.Name)
            {
                return IsWebAPI(baseType.Resolve());
            }

            // Do not reference web DLLs, just perform a string match
            return IsController(baseType);
        }

        private static bool IsController(TypeReference type)
        {
            return (type.Name == "ApiController" || type.Name == "Controller");
        }
    }
}
