using System;
using System.Collections.Generic;

namespace BaseLibrary.TestClasses.Models
{
    public class BaseEntity
    {
        public virtual long Id { get; set; }
        public string[] Titles { get; set; }
        public IEnumerable<DateTime>[] PastEventPeriods { get; set; }
        public Enums.Colors CardColor { get; set; }
    }
}
