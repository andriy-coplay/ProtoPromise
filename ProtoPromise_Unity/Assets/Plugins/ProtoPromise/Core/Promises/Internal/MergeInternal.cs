﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System.Diagnostics;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                internal override void Handle(PromiseRefBase handler) { throw new System.InvalidOperationException(); }

                new protected void Reset(ushort depth)
                {
                    base.Reset(depth);
#if PROMISE_PROGRESS
                    _smallFields._currentProgress = default(Fixed32);
#endif
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class MergePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                private MergePromise() { }

                internal override void MaybeDispose()
                {
                    MaybeDisposeNonVirt();
                }

                private void MaybeDisposeNonVirt()
                {
                    if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal static MergePromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    var promise = ObjectPool.TryTake<MergePromise<TResult>>()
                        ?? new MergePromise<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, totalProgress, depth);
                    return promise;
                }

                internal static MergePromise<TResult> GetOrCreate(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,

#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value,
                    PromiseResolvedDelegate<TResult> onPromiseResolved,
                    int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    var promise = MergePromiseT.GetOrCreate(value, onPromiseResolved);
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, totalProgress, depth);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    _waitCount = pendingAwaits;
                    unchecked
                    {
                        _retainCounter = pendingAwaits + 1;
                    }
                    Reset(depth);
                    SetupProgress(completedProgress, totalProgress);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
#if PROMISE_DEBUG
                        lock (_previousPromises)
                        {
                            _previousPromises.Push(passThrough.Owner);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(this);
                        if (_rejectContainer != null)
                        {
                            // This was rejected or canceled potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeMarkAwaitedAndDispose(p.Id);
                                p.Dispose();
                                MaybeDispose();
                            }
                        }
                    }
                }

                protected override void Handle(PromisePassThrough passThrough)
                {
                    var handler = passThrough.Owner;
                    var state = handler.State;
                    if (state != Promise.State.Resolved) // Rejected/Canceled
                    {
                        if (Interlocked.CompareExchange(ref _rejectContainer, handler._rejectContainer, null) == null)
                        {
                            handler.SuppressRejection = true;
                            State = state;
                            HandleNextInternal();
                        }
                        InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                    }
                    else // Resolved
                    {
                        IncrementProgress(passThrough);
                        if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0
                            && Interlocked.CompareExchange(ref _rejectContainer, RejectContainer.s_completionSentinel, null) == null)
                        {
                            State = state;
                            HandleNextInternal();
                        }
                    }
                    MaybeDisposeNonVirt();
                }

                partial void IncrementProgress(PromisePassThrough passThrough);
                partial void SetupProgress(ulong completedProgress, ulong totalProgress);

                private sealed partial class MergePromiseT : MergePromise<TResult>
                {
                    private MergePromiseT() { }

                    internal override void MaybeDispose()
                    {
                        if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                        {
                            Dispose();
                            _onPromiseResolved = null;
                            ObjectPool.MaybeRepool(this);
                        }
                    }

                    internal static MergePromiseT GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                        in
#endif
                        TResult value, PromiseResolvedDelegate<TResult> onPromiseResolved)
                    {
                        var promise = ObjectPool.TryTake<MergePromiseT>()
                            ?? new MergePromiseT();
                        promise._onPromiseResolved = onPromiseResolved;
                        promise._result = value;
                        return promise;
                    }

                    protected override void Handle(PromisePassThrough passThrough)
                    {
                        var handler = passThrough.Owner;
                        var state = handler.State;
                        if (state != Promise.State.Resolved) // Rejected/Canceled
                        {
                            if (Interlocked.CompareExchange(ref _rejectContainer, handler._rejectContainer, null) == null)
                            {
                                handler.SuppressRejection = true;
                                State = state;
                                HandleNextInternal();
                            }
                            InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                        }
                        else // Resolved
                        {
                            IncrementProgress(passThrough);
                            _onPromiseResolved.Invoke(handler, ref _result, passThrough.Index);
                            if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0
                                && Interlocked.CompareExchange(ref _rejectContainer, RejectContainer.s_completionSentinel, null) == null)
                            {
                                State = state;
                                HandleNextInternal();
                            }
                        }
                        MaybeDispose();
                    }
                }
            }

#if PROMISE_PROGRESS
            partial class MergePromise<TResult>
            {
                partial void SetupProgress(ulong completedProgress, ulong totalProgress)
                {
                    _unscaledProgress = new UnsignedFixed64(completedProgress);
                    _progressScaler = (double) (Depth + 1u) / (double) totalProgress;
                }

                partial void IncrementProgress(PromisePassThrough passThrough)
                {
                    var wasReportingPriority = Fixed32.ts_reportingPriority;
                    Fixed32.ts_reportingPriority = true;

                    uint dif = passThrough.GetProgressDifferenceToCompletion();
                    var progress = IncrementProgress(dif);
                    ReportProgress(progress, Depth);

                    Fixed32.ts_reportingPriority = wasReportingPriority;
                }

                private Fixed32 NormalizeProgress(UnsignedFixed64 unscaledProgress)
                {
                    ThrowIfInPool(this);
                    var scaledProgress = Fixed32.GetScaled(unscaledProgress, _progressScaler);
                    _smallFields._currentProgress = scaledProgress;
                    return scaledProgress;
                }

                protected override PromiseRefBase IncrementProgress(long amount, ref Fixed32 progress, ushort depth)
                {
                    ThrowIfInPool(this);
                    // This essentially acts as a pass-through to normalize the progress.
                    progress = IncrementProgress(amount);
                    return this;
                }

                private Fixed32 IncrementProgress(long amount)
                {
                    var unscaledProgress = _unscaledProgress.InterlockedIncrement(amount);
                    return NormalizeProgress(unscaledProgress);
                }
            }
#endif
        }
    }
}