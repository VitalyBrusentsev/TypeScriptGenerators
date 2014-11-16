using System.Collections.Generic;

namespace CodeModel
{
    public class Model
    {
        public Type Type { get; set; }
        public IEnumerable<Member> Properties { get; set; }
    }
}
