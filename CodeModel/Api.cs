using System.Collections.Generic;

namespace CodeModel
{
    public class Api
    {
        public IEnumerable<Controller> Controllers { get; set; }
        public IEnumerable<Model> Models { get; set; }
        public IEnumerable<Enum> Enums { get; set; }
    }
}