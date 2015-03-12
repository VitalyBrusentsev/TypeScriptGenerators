using BaseLibrary.TestClasses.Models;
using CecilScanner;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace CecilScannerTests
{
    [TestFixture]
    public class GivenApi
    {
        private CodeModel.Api _api = null;

        [TestFixtureSetUp]
        public void Initialize()
        {
            var _scanner = new Scanner();
            var assemblies = new[] { Assembly.GetExecutingAssembly().Location, typeof(BaseEntity).Assembly.Location };
            _api = _scanner.ScanApi(assemblies);
        }

        [Test]
        public void When_base_model_is_not_referenced_Then_it_is_not_included()
        {
            Assert.IsFalse(_api.Models.Any(m => m.Type.FullName == "BaseLibrary.TestClasses.Models.BaseEntity"));
        }

        [Test]
        public void When_enum_is_not_referenced_Then_it_is_not_included()
        {
            Assert.IsFalse(_api.Enums.Any(e => e.Type.FullName == "BaseLibrary.TestClasses.Enums.TestEnum"));
        }

        [Test]
        public void When_enum_is_marked_as_included_Then_it_is_included()
        {
            Assert.IsTrue(_api.Enums.Any(e => e.Type.FullName == "CecilScannerTests.TestClasses.Enums.Seasons"));
        }
                 
    }
}
