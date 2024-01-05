using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Events;
using Seq.Syntax.Expressions;

namespace Seq.Input.HealthCheck.Data;

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
}