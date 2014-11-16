using System.Collections.Generic;

namespace CodeModel
{
    public class Enum
    {
        public Type Type { get; set; }
        public IEnumerable<EnumMember> Members { get; set; }
    }
}
