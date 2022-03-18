using System;
using System.Collections.Generic;
using System.IO;
namespace Seq.Input.HealthCheck.Util
{
    // adapted from: https://github.com/datalust/seq-app-httprequest/blob/e23b2328141352f5b4851c7502801f5bfe6511f7/src/Seq.App.HttpRequest/Settings/HeaderSettingFormat.cs
    // changes to parse for C# 8.0 language level
    static class HeaderSettingFormat
    {
        public static List<(string, string)> FromSettings(string authenticationHeader, string otherHeaders)
        {
            var headers = new List<(string, string)>();
            
            if (!string.IsNullOrWhiteSpace(authenticationHeader))
                headers.Add(Parse(authenticationHeader));
            
            if (!string.IsNullOrWhiteSpace(otherHeaders))
            {
                var reader = new StringReader(otherHeaders);
                var line = reader.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    headers.Add(Parse(line));
                    line = reader.ReadLine();
                }
            }

            return headers;
        }

        internal static (string, string) Parse(string header)
        {
            var components = header.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length != 2)
            {
                throw new ArgumentException($"The header must be specified in `Name: Value` format. (Affected line was '{header}')");
            }

            return (components[0].Trim(), components[1].Trim());
        }
    }
}
