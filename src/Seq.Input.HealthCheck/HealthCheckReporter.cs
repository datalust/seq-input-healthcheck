// Copyright 2019 Datalust and contributors. 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Seq.Input.HealthCheck
{
    class HealthCheckReporter
    {
        readonly TextWriter _output;
        readonly JsonSerializer _serializer = JsonSerializer.Create();
        readonly object _sync = new object();

        public HealthCheckReporter(TextWriter output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Report(HealthCheckResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            lock (_sync)
            {
                _serializer.Serialize(_output, result);
                _output.WriteLine();
                _output.Flush();
            }
        }
    }
}