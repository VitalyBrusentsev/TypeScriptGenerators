using BaseLibrary.TestClasses.Models;
using CecilScanner;
using CodeModel;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CecilScannerTests
{
    [TestFixture]
    public class GivenModel
    {
        private CodeModel.Api _api = null;
        private Model _employeeType = null;
        private IDictionary<string, Member> _properties = null;

        [TestFixtureSetUp]
        public void Initialize()
        {
            var _scanner = new Scanner();
            var assemblies = new[] { Assembly.GetExecutingAssembly().Location, typeof(BaseEntity).Assembly.Location };
            _api = _scanner.ScanApi(assemblies);
            _employeeType = _api.Models.Single(m => m.Type.FullName == "CecilScannerTests.TestClasses.Models.Employee");
            _properties = _employeeType.Properties.ToDictionary(p => p.Name);
        }

        private void AssertTypeDimensions(string name, string type, int arrayDimensions)
        {
            var propertyType = _properties[name].Type;
            Assert.AreEqual(type, propertyType.TSElementName);
            Assert.AreEqual(arrayDimensions, propertyType.ArrayDimensions);
        }

        [Test]
        public void When_base_model_is_not_referenced_Then_it_is_not_included()
        {
            Assert.IsFalse(_api.Models.Any(m => m.Type.FullName == "BaseLibrary.TestClasses.Models.BaseEntity"));
        }
        

        [Test]
        public void When_subclassed_Then_inherited_properties_should_be_included()
        {
            Assert.AreEqual(12, _properties.Count());
            AssertTypeDimensions("Id", "number", 0);
        }

        [Test]
        public void When_property_types_are_primitives_Then_generated_types_are_supported()
        {
            AssertTypeDimensions("Name", "string", 0);
            AssertTypeDimensions("IsContractor", "boolean", 0);
            AssertTypeDimensions("LastReviewPerformance", "number", 0);
            AssertTypeDimensions("UnrelatedProperty", "any", 0);
        }

        [Test]
        public void When_property_type_is_datetime_Then_generated_type_is_string()
        {
            AssertTypeDimensions("Birthday", "string", 0);
        }

        [Test]
        public void When_property_types_are_arrays_Then_generated_types_are_arrays()
        {
            Assert.IsTrue(_properties["Ratings"].Type.IsEnum);
            AssertTypeDimensions("Ratings", "BaseLibrary.TestClasses.Enums.Rating", 1);
            AssertTypeDimensions("Titles", "string", 1);
            AssertTypeDimensions("PastEventPeriods", "string", 2);
        }

        [Test]
        public void When_property_type_is_dictionary_Then_generated_type_is_any()
        {
            AssertTypeDimensions("DictionaryProperty", "any", 0);
        }

        [Test]
        public void When_property_type_is_dictionary_array_Then_generated_type_is_any_array()
        {
            var t = _properties["ArrayOfDictionaries"].Type;
            Assert.AreEqual("any", t.TSElementName);
            Assert.AreEqual(1, t.ArrayDimensions);
            Assert.IsFalse(t.IsProjectDefined);
            Assert.IsTrue(t.IsPrimitive);
        }
    }
}
