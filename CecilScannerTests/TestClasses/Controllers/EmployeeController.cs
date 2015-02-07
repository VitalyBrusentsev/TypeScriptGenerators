using CecilScannerTests.TestClasses.Models;
using System;
using System.Collections.Generic;

namespace CecilScannerTests.TestClasses.Controllers
{
    public class EmployeeController: Controller
    {
        public Employee Get(long id)
        {
            return new Employee();
        }

        public IList<Employee> GetEmployees(DayOfWeek[] selectedDays)
        {
            return new List<Employee>();
        }
    }
}
