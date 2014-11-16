using System.Collections.Generic;

namespace CodeModel
{
    public class Controller
    {
        public Type Type { get; set; }
        public IEnumerable<Method> Methods { get; set; }
    }
}
