﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public static Promise _Race<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
            {
                ValidateArgument(promises, "promises", skipFrames + 1);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", GetFormattedStacktrace(skipFrames + 1));
                }
                int count;
                var passThroughs = WrapInPassThroughs(promises, out count, skipFrames + 1);
                return RacePromise0.GetOrCreate(passThroughs, count, skipFrames + 1);
            }

            public static Promise<T> _Race<T, TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
            {
                ValidateArgument(promises, "promises", skipFrames + 1);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", GetFormattedStacktrace(skipFrames + 1));
                }
                int count;
                var passThroughs = WrapInPassThroughs<T, TEnumerator>(promises, out count, skipFrames + 1);
                return RacePromise<T>.GetOrCreate(passThroughs, count, skipFrames + 1);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class RacePromise0 : PoolablePromise<RacePromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private RacePromise0() { }

                public static Promise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (RacePromise0) _pool.Pop() : new RacePromise0();

                    promise._passThroughs = promisePassThroughs;

                    promise._waitCount = (uint) count;
                    promise.Reset(skipFrames + 1);
                    // Retain this until all promises resolve/reject/cancel.
                    promise.RetainInternal();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.SetTargetAndAddToOwner(promise);
                    }

                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, Promise owner, int index)
                {
                    bool handle = _valueOrPrevious == null;
                    if (handle)
                    {
                        owner._wasWaitedOn = true;
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    if (--_waitCount == 0)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                    return handle;
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }

                protected override void Dispose()
                {
                    base.Dispose();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class RacePromise<T> : PoolablePromise<T, RacePromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private RacePromise() { }

                public static Promise<T> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (RacePromise<T>) _pool.Pop() : new RacePromise<T>();

                    promise._passThroughs = promisePassThroughs;

                    promise._waitCount = (uint) count;
                    promise.Reset(skipFrames + 1);
                    // Retain this until all promises resolve/reject/cancel.
                    promise.RetainInternal();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.SetTargetAndAddToOwner(promise);
                    }

                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, Promise owner, int index)
                {
                    bool handle = _valueOrPrevious == null;
                    if (handle)
                    {
                        owner._wasWaitedOn = true;
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    if (--_waitCount == 0)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                    return handle;
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }
            }

#if PROMISE_PROGRESS
            partial class RacePromise0 : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        Promise owner = passThrough.Owner;
                        if (owner != null)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, owner._waitDepthAndProgress.WholePart);
                        }
                    }

                    // Expect the shortest chain to finish first.
                    _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = false;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            RetainInternal();
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.CancelOrIncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = true;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                    }
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending | _suspended)
                    {
                        ReleaseInternal();
                        return;
                    }

                    _invokingProgress = false;

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }

                    ReleaseInternal();
                }
            }

            partial class RacePromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        Promise owner = passThrough.Owner;
                        if (owner != null)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, owner._waitDepthAndProgress.WholePart);
                        }
                    }

                    // Expect the shortest chain to finish first.
                    _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = false;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            RetainInternal();
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.CancelOrIncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = true;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                    }
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending | _suspended)
                    {
                        ReleaseInternal();
                        return;
                    }

                    _invokingProgress = false;

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }

                    ReleaseInternal();
                }
            }
#endif
        }
    }
}