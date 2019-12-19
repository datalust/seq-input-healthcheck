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
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;

namespace Seq.Input.HealthCheck.Util
{
    static class UrlHelper
    {
        public static string AppendParameter(string targetUrl, string name, string value)
        {
            if (targetUrl == null) throw new ArgumentNullException(nameof(targetUrl));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return QueryHelpers.AddQueryString(targetUrl, new Dictionary<string, string>
            {
                [name] = value
            });
        }
    }
}
