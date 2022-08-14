﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0031 // Use null propagation

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if PROMISE_DEBUG && !NET_LEGACY
using System.Linq;
#endif

namespace Proto.Promises
{
    partial class Internal
    {
        // Just a random number that's not zero.
        internal const short ValidIdFromApi = 31265;

        internal static string CausalityTraceMessage
        {
            get
            {
#if PROMISE_DEBUG
                return Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                    ? " -- This exception's Stacktrace contains the causality trace of all async callbacks that ran."
                    : " -- Set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#else
                return " -- Enable DEBUG mode and set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#endif
            }
        }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#else
        internal const MethodImplOptions InlineOption = (MethodImplOptions) 256; // AggressiveInlining
#endif

        internal static void ValidateProgressValue(float value, string argName, int skipFrames)
        {
            bool isBetween01 = value >= 0f && value <= 1f;
            if (!isBetween01)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.", GetFormattedStacktrace(skipFrames + 1));
            }
        }

        // Calls to these get compiled away in RELEASE mode
        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other);
            partial void ValidateAwait(PromiseRefBase other, short promiseId);
        }

        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames);
        static partial void SetCurrentInvoker(ITraceable current);
        static partial void ClearCurrentInvoker();
        static partial void IncrementInvokeId();
#if PROMISE_DEBUG
        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames)
        {
            StackTrace stackTrace = Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                ? GetStackTrace(skipFrames + 1)
                : null;
            traceable.Trace = new CausalityTrace(stackTrace, ts_currentTrace);
        }

#if !CSHARP_7_3_OR_NEWER
        // This is only needed in older language versions that don't support ref structs.
        [ThreadStatic]
        private static long ts_invokeId;
        internal static long InvokeId { get { return ts_invokeId; } }

        static partial void IncrementInvokeId()
        {
            unchecked
            {
                ++ts_invokeId;
            }
        }
#else
        internal static long InvokeId { get { return ValidIdFromApi; } }
#endif // !CSHARP_7_3_OR_NEWER

        [ThreadStatic]
        private static CausalityTrace ts_currentTrace;
        [ThreadStatic]
        private static Stack<CausalityTrace> ts_traces;

        static partial void SetCurrentInvoker(ITraceable current)
        {
            if (ts_traces == null)
            {
                ts_traces = new Stack<CausalityTrace>();
            }
            ts_traces.Push(ts_currentTrace);
            if (current != null)
            {
                ts_currentTrace = current.Trace;
            }
        }

        static partial void ClearCurrentInvoker()
        {
            ts_currentTrace = ts_traces.Pop();
            IncrementInvokeId();
        }

        private static StackTrace GetStackTrace(int skipFrames)
        {
            return new StackTrace(skipFrames + 1, true);
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return traceable != null ? traceable.Trace.ToString() : null;
        }

        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(new StackTrace[1] { GetStackTrace(skipFrames + 1) });
        }

        internal static void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        internal static string FormatStackTrace(IEnumerable<StackTrace> stackTraces)
        {
#if NET_LEGACY
            // Format stack trace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            var _stackTraces = new List<string>();
            var separator = new string[1] { Environment.NewLine + " " };
            var sb = new System.Text.StringBuilder();
            foreach (StackTrace st in stackTraces)
            {
                if (st == null)
                {
                    continue;
                }
                string stackTrace = st.ToString();
                if (string.IsNullOrEmpty(stackTrace))
                {
                    continue;
                }
                foreach (var trace in stackTrace.Substring(1).Split(separator, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!trace.Contains("Proto.Promises"))
                    {
                        sb.Append(trace)
                            .Replace(":line ", ":")
                            .Replace("(", " (")
                            .Replace(") in", ") [0x00000] in"); // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
                        _stackTraces.Add(sb.ToString());
                        sb.Length = 0;
                    }
                }
            }
            foreach (var trace in _stackTraces)
            {
                sb.Append(trace).Append(" " + Environment.NewLine);
            }
            sb.Append(" ");
            return sb.ToString();
#else // NET_LEGACY
            // StackTrace.ToString() format issue was fixed in the new runtime.
            var stackFrames = new List<StackFrame>();
            foreach (StackTrace stackTrace in stackTraces)
            {
                stackFrames.AddRange(stackTrace.GetFrames());
            }

            var trace = stackFrames
                .Where(frame =>
                {
                    // Ignore DebuggerNonUserCode and DebuggerHidden.
                    var methodType = frame?.GetMethod();
                    return methodType != null
                        && !methodType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.DeclaringType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.IsDefined(typeof(DebuggerHiddenAttribute), false);
                })
                // Create a new StackTrace to get proper formatting.
                .Select(frame => new StackTrace(frame).ToString())
                .ToArray();

            return string.Join(Environment.NewLine, trace);
#endif // NET_LEGACY
        }

        partial interface ITraceable
        {
            CausalityTrace Trace { get; set; }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal class CausalityTrace
        {
            private readonly StackTrace _stackTrace;
            private readonly CausalityTrace _next;

            public CausalityTrace(StackTrace stackTrace, CausalityTrace higherStacktrace)
            {
                _stackTrace = stackTrace;
                _next = higherStacktrace;
            }

            public override string ToString()
            {
                if (_stackTrace == null)
                {
                    return null;
                }
                return FormatStackTrace(GetStackTraces());
            }

            private IEnumerable<StackTrace> GetStackTraces()
            {
                for (CausalityTrace current = this; current != null; current = current._next)
                {
                    if (current._stackTrace == null)
                    {
                        yield break;
                    }
                    yield return current._stackTrace;
                }
            }
        }

        internal static void ValidateOperation(Promise promise, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidOperationException("Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    GetFormattedStacktrace(skipFrames + 1));
            }
        }

        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other)
            {
                ValidateAwait(other._ref, other._id, false);
            }

            partial void ValidateAwait(PromiseRefBase other, short promiseId)
            {
                ValidateAwait(other, promiseId, true);
            }

            private void ValidateAwait(PromiseRefBase other, short promiseId, bool awaited)
            {
                if (new Promise(other, promiseId, 0).IsValid == false)
                {
                    // Awaiting or returning an invalid from the callback is not allowed.
                    if (awaited)
                        throw new InvalidOperationException("An invalid promise was awaited.", string.Empty);
                    throw new InvalidReturnException("An invalid promise was returned.", string.Empty);
                }

                // A promise cannot wait on itself.
                if (other == this)
                {
                    other.MaybeMarkAwaitedAndDispose(other.Id);
                    if (awaited)
                        throw new InvalidOperationException("A Promise cannot wait on itself.", string.Empty);
                    throw new InvalidReturnException("A Promise cannot wait on itself.", string.Empty);
                }
                if (other == null)
                {
                    return;
                }
                // This allows us to check Merge/All/Race/First Promises iteratively.
                Stack<PromiseRefBase> previouses = PreviousesForIterativeAlgorithm;
                PromiseRefBase prev = other._previous;
            Repeat:
                for (; prev != null; prev = prev._previous)
                {
                    if (prev == this)
                    {
                        other.MaybeMarkAwaitedAndDispose(other.Id);
                        previouses.Clear();
                        if (awaited)
                            throw new InvalidOperationException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                        throw new InvalidReturnException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                    }
                    prev.BorrowPassthroughs(previouses);
                }

                if (previouses.Count > 0)
                {
                    prev = previouses.Pop();
                    goto Repeat;
                }
            }

            [ThreadStatic]
            private static Stack<PromiseRefBase> ts_previousesForIterativeAlgorithm;
            private static Stack<PromiseRefBase> PreviousesForIterativeAlgorithm
            {
                get
                {
                    if (ts_previousesForIterativeAlgorithm == null)
                    {
                        ts_previousesForIterativeAlgorithm = new Stack<PromiseRefBase>();
                    }
                    return ts_previousesForIterativeAlgorithm;
                }
            }

            protected virtual void BorrowPassthroughs(Stack<PromiseRefBase> borrower) { }

            partial class MultiHandleablePromiseBase<TResult>
            {
                protected override void BorrowPassthroughs(Stack<PromiseRefBase> borrower)
                {
                    lock (_previousPromises)
                    {
                        foreach (var promiseRef in _previousPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }
                
                new protected void Dispose()
                {
                    base.Dispose();
                    lock (_previousPromises)
                    {
                        _previousPromises.Clear();
                    }
                }
            }
        }
#else // PROMISE_DEBUG
        internal static long InvokeId
        {
            [MethodImpl(InlineOption)]
            get { return ValidIdFromApi; }
        }

        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return null;
        }
#endif // PROMISE_DEBUG
    } // class Internal

    partial struct Promise
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise promise, string argName, int skipFrames);

#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }

    partial struct Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames);
#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }
}