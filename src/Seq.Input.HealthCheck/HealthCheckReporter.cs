using System;
using System.IO;
using Newtonsoft.Json;

namespace Seq.Input.HealthCheck
{
    class HealthCheckReporter
    {
        readonly TextWriter _output;

        readonly JsonSerializer _serializer = JsonSerializer.Create(
            new JsonSerializerSettings {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

        public HealthCheckReporter(TextWriter output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Report(HealthCheckResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            _serializer.Serialize(_output, result);
            _output.WriteLine();
            _output.Flush();
        }
    }
}