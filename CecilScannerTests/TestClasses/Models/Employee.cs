using System;
using System.Collections.Generic;

namespace CecilScannerTests.TestClasses.Models
{
    public class Employee: BaseLibrary.TestClasses.Models.BaseEntity
    {
        public override long Id { get; set; }
        public string Name { get; set; }
        public bool IsContractor { get; set; }
        public DateTime Birthday { get; set; }
        public List<BaseLibrary.TestClasses.Enums.Rating> Ratings { get; set; }
        public double LastReviewPerformance { get; set; }
        public System.Text.StringBuilder UnrelatedProperty { get; set; }
        public IDictionary<string, double> DictionaryProperty { get; set; }
        public IDictionary<string, object>[] ArrayOfDictionaries { get; set; }
    }
}
