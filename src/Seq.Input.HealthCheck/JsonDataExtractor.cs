using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Filters.Expressions;
using Serilog.Formatting.Compact.Reader;
using Serilog.Formatting.Json;

namespace Seq.Input.HealthCheck
{
    public class JsonDataExtractor
    {
        static JsonValueFormatter ValueFormatter = new JsonValueFormatter("$type");
        static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None
        });

        readonly Func<JToken, JToken> _extract;

        public JsonDataExtractor(string expression)
        {
            if (expression == "@Properties")
            {
                _extract = v => v;
            }
            else
            {
                var expr = FilterLanguage.CreateFilter(expression);
                _extract = v => {
                    if (!(v is JObject obj))
                        throw new ArgumentException("Data value extraction requires a JSON object response.");

                    if (!obj.ContainsKey("@t"))
                        obj["@t"] = DateTime.UtcNow.ToString("o");

                    var le = LogEventReader.ReadFromJObject(obj);

                    var value = expr(le);
                    if (value == null)
                        return JValue.CreateNull();

                    if (!(value is LogEventPropertyValue lepv))
                        return JToken.FromObject(value);

                    var sw = new StringWriter();
                    ValueFormatter.Format(lepv, sw);
                    return Serializer.Deserialize<JToken>(
                        new JsonTextReader(new StringReader(sw.ToString())));
                };
            }
        }

        public JToken ExtractData(TextReader json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var document = Serializer.Deserialize<JToken>(new JsonTextReader(json));
            return _extract(document);
        }
    }
}
