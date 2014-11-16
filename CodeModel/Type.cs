namespace CodeModel
{
    public class Type
    {
        public bool IsEnum { get; set; }
        public bool IsArray { get; set; }
        public bool IsProjectDefined { get; set; }
        public bool IsPrimitive { get; set; }
        public string FullName { get; set; }
        public string TSElementName { get; set; }
    }
}
