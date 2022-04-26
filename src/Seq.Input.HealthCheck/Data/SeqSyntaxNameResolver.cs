using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Events;
using Serilog.Expressions;

namespace Seq.Input.HealthCheck.Data
{
    public class SeqSyntaxNameResolver: NameResolver
    {
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public static LogEventPropertyValue? Has(LogEventPropertyValue? value)
        {
            return new ScalarValue(value != null);
        }
    
        public override bool TryResolveFunctionName(string name, [NotNullWhen(true)] out MethodInfo? implementation)
        {
            if ("Has".Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                implementation = GetType().GetMethod("Has", BindingFlags.Static | BindingFlags.Public)!;
                return true;
            }

            implementation = null;
            return false;
        }

        public override bool TryResolveBuiltInPropertyName(string alias, [NotNullWhen(true)] out string? target)
        {
            target = alias switch
            {
                "Exception" => "x",
                "Level" => "l",
                "Message" => "m",
                "MessageTemplate" => "mt",
                "Properties" => "p",
                "Timestamp" => "t",
                _ => null
            };
        
            return target != null;
        }
    }
}