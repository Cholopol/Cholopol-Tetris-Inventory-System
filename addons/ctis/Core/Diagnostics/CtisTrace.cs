using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if DEBUG
using Microsoft.DiagnosticsHub;
#endif

namespace Ctis.Core;

/// <summary>
/// Profiling utility for Visual Studio performance profiler User Marks with zero overhead in Release builds.
/// </summary>
public static class CtisTrace
{
    /// <summary>
    /// Measures and marks a code block execution range on the profiler timeline.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityScope Scope(string name)
    {
#if DEBUG
        return new ActivityScope(new UserMarkRange(name));
#else
        return default;
#endif
    }

    /// <summary>
    /// Emits an instantaneous User Mark on the profiler timeline.
    /// </summary>
    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mark(string name)
    {
#if DEBUG
        UserMarks.EmitMessage(name);
#endif
    }

    /// <summary>
    /// Zero-allocation ref struct for scoped activity tracing.
    /// </summary>
    public readonly ref struct ActivityScope
    {
#if DEBUG
        private readonly UserMarkRange? _range;

        public ActivityScope(UserMarkRange range)
        {
            _range = range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _range?.Dispose();
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
#endif
    }
}
