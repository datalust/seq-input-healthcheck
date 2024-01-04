using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Input.HealthCheck.Data;
using Xunit;

namespace Seq.Input.HealthCheck.Tests.Data
{
    public class JsonDataExtractorTests
    {
        [Fact]
        public void ExtractsNestedJsonValues()
        {
            var json = JsonConvert.SerializeObject(new { Person = new { Name = "Nick" } });
            var extractor = new JsonDataExtractor("Person.Name");
            var value = extractor.ExtractData(new StringReader(json));
            var jv = Assert.IsType<JValue>(value);
            Assert.Equal("Nick", jv.Value);
        }

        [Fact]
        public void ExtractsNestedJsonStructures()
        {
            var json = JsonConvert.SerializeObject(new { Person = new { Name = "Nick" } });
            var extractor = new JsonDataExtractor("Person");
            var value = extractor.ExtractData(new StringReader(json));
            var jo = Assert.IsType<JObject>(value);
            var jv = (JValue)jo["Name"];
            Assert.Equal("Nick", jv.Value);
        }

        [Fact]
        public void PropertiesBuiltInExtractsWholeDocument()
        {
            var json = JsonConvert.SerializeObject(new { Person = new { Name = "Nick" } });
            var extractor = new JsonDataExtractor("@Properties");
            var value = extractor.ExtractData(new StringReader(json));
            var rt = JsonConvert.SerializeObject(value);
            Assert.Equal(json, rt);
        }
    }
}
