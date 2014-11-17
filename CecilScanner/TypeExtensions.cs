using Mono.Cecil;

namespace CecilScanner
{
    public static class TypeExtensions
    {
        public static string GetSafeFullName(this TypeReference type)
        {
            return type.FullName.Replace("/", ".");
        }
    }
}
