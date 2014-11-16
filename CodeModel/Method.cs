using System.Collections.Generic;

namespace CodeModel
{
    public class Method: Member
    {
        public string OriginalName { get; set; }
        public string HttpVerb { get; set; }
        public IEnumerable<Parameter> Parameters { get; set; }
        public IEnumerable<Attribute> Attributes { get; set; }
        public bool IsValid { get; set; }
        public string ValidationError { get; set; }
    }
}
