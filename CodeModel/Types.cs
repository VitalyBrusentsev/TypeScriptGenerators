namespace CodeModel
{
    public static class Types
    {
        public static readonly Type Any = new Type { FullName = "any", IsPrimitive = true, TSElementName = "any" };
        public static readonly Type Boolean = new Type { FullName = "boolean", IsPrimitive = true, TSElementName = "boolean" };
        public static readonly Type Number = new Type { FullName = "number", IsPrimitive = true, TSElementName = "number" };
        public static readonly Type String = new Type { FullName = "string", IsPrimitive = true, TSElementName = "string" };
        public static readonly Type Void = new Type { FullName = "void", IsPrimitive = true, TSElementName = "void" };
    }
}
