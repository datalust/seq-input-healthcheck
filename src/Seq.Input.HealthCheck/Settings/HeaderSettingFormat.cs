using System;
using System.Collections.Generic;
using System.IO;
namespace Seq.Input.HealthCheck.Util;

static class HeaderSettingFormat
{
    public static List<(string, string)> FromSettings(string? authenticationHeader, string? otherHeaders)
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
        var colon = header.IndexOf(":", StringComparison.Ordinal);
        if (colon is 0 or -1)
            throw new ArgumentException("The header must be specified in `Name: Value` format.");
        return (header[..colon].Trim(), header[(colon + 1)..].Trim());
    }
}