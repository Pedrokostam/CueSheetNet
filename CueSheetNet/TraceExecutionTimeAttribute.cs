using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CueSheetNet;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TraceExecutionTimeAttribute : MethodAspect
{
    public string? StepName { get; set; }
    public int Level { get; set; } = 0;
    public int ErrorLevel { get; set; } = 4;
    public TraceExecutionTimeAttribute(string name, int level = 0, int errorLevel = 4)
    {
        StepName = name;
        Level = level;
        ErrorLevel = errorLevel;
    }
    public TraceExecutionTimeAttribute()
    {

    }
    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {

        var loggerProperty = builder.Target.DeclaringType.AllProperties
            .OfName("Loggerio")
            .Single();

        builder.Override(
            nameof(OverrideMethod),
            new { loggerProperty });
    }

    public override void BuildEligibility(IEligibilityBuilder<IMethod> builder)
    {
        base.BuildEligibility(builder);
        //builder.MustNotBeStatic();
    }

    [Template]
    private dynamic? OverrideMethod(IProperty loggerProperty)
    {
        var logger = loggerProperty.IsStatic ? meta.ThisType.Loggerio : meta.This.Loggerio;
        if (!logger.IsEnabled((LogLevel)Level))
        {
            return meta.Proceed();
        }

        var stepName = StepName ?? meta.Target.Method.Name;

        logger.StartedStep(stepName);

        var startTimestamp = Stopwatch.GetTimestamp();
        Exception? error = null;
        try
        {
            return meta.Proceed();
        }
        catch (Exception e)
        {
            error = e;
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var lvl = error is null ? (LogLevel)Level : (LogLevel)ErrorLevel;
            logger.FinishedStep(stepName, elapsed, lvl, error);
            //if (elapsed.TotalMilliseconds > 0)
            //{
            //    logger.FinishedStepMs(stepName, elapsed.TotalMilliseconds, lvl, error);
            //}
            //else
            //{
            //    logger.FinishedStepUs(stepName, elapsed.TotalMicroseconds, lvl, error);
            //}
        }
    }
}