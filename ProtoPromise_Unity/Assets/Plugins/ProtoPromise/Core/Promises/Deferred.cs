﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0618 // Type or member is obsolete
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to report progress and reject or cancel the attached <see cref="Promise"/>.
        /// <para/>You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to resolve the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly // Deferreds behave more like write-only, but this prevents the compiler from emitting defensive copies when passing to a function with the `in` keyword.
#endif
            struct DeferredBase : ICancelable, IProgress<float>, IEquatable<DeferredBase>
        {
            private readonly Internal.IDeferredPromise _ref;
            private readonly short _promiseId;
            private readonly int _deferredId;

            void IProgress<float>.Report(float value)
            {
                ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("DeferredBase.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise((Internal.PromiseRefBase) _this, _promiseId, 0);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref as Internal.PromiseRefBase;
                    return _this != null && _this.GetIsValid(_promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return Internal.DeferredPromiseHelper.GetIsValidAndPending(_ref, _deferredId);
                }
            }

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal DeferredBase(Internal.IDeferredPromise promise, short promiseId, int deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Deferred ToDeferred()
            {
                return new Deferred(
                    (Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>) _ref,
                    _promiseId,
                    _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Returns an invalid <see cref="Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Deferred AsDeferred()
            {
                return new Deferred(_ref as Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>, _promiseId, _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred ToDeferred<T>()
            {
                return new Promise<T>.Deferred(
                    (Internal.PromiseRefBase.DeferredPromise<T>) _ref,
                    _promiseId,
                    _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Returns an invalid <see cref="Promise{T}.Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred AsDeferred<T>()
            {
                return new Promise<T>.Deferred(_ref as Internal.PromiseRefBase.DeferredPromise<T>, _promiseId, _deferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this == null || !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("DeferredBase.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (Internal.DeferredPromiseHelper.TryIncrementDeferredIdAndUnregisterCancelation(_this, _deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("DeferredBase.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (Internal.DeferredPromiseHelper.TryIncrementDeferredIdAndUnregisterCancelation(_this, _deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("DeferredBase.ReportProgress: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return Internal.DeferredPromiseHelper.TryReportProgress(_ref, _deferredId, progress);
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="DeferredBase"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(DeferredBase other)
            {
                return this == other;
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is DeferredBase deferred && Equals(deferred);
#else
                return obj is DeferredBase && Equals((DeferredBase) obj);
#endif
            }

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());
            }

            /// <summary>Returns a value indicating whether two <see cref="DeferredBase"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(DeferredBase lhs, DeferredBase rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            /// <summary>Returns a value indicating whether two <see cref="DeferredBase"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(DeferredBase lhs, DeferredBase rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("DeferredBase.State is no longer valid. Use IsValidAndPending.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public State State
            {
                get
                {
                    throw new InvalidOperationException("DeferredBase.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("DeferredBase.Retain is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Retain()
            {
                throw new InvalidOperationException("DeferredBase.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("DeferredBase.Release is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Release()
            {
                throw new InvalidOperationException("DeferredBase.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct DeferredBase

        /// <summary>
        /// An instance of this is used to report progress and resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : ICancelable, IProgress<float>, IEquatable<Deferred>
        {
            internal readonly Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult> _ref;
            internal readonly short _promiseId;
            internal readonly int _deferredId;


            void IProgress<float>.Report(float value)
            {
                ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise(_this, _promiseId, 0);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
                    return _this != null && _this.GetIsValid(_promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
                    return _this != null && _this.DeferredId == _deferredId;
                }
            }

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult> promise, short promiseId, int deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult> promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRefBase.DeferredPromiseCancel<Internal.VoidResult>.GetOrCreate(cancelationToken)
                    : Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.ResolveDirectVoid();
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve()
            {
                return Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>.TryResolveVoid(_ref, _deferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this == null || !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return Internal.DeferredPromiseHelper.TryReportProgress(_ref, _deferredId, progress);
            }

            /// <summary>
            /// Cast to <see cref="DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator DeferredBase(Deferred rhs)
            {
                return new DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);
            }

            /// <summary>
            /// Cast <see cref="DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(DeferredBase rhs)
            {
                return rhs.ToDeferred();
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Deferred"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
            {
                return this == other;
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());
            }

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.State is no longer valid. Use IsValidAndPending.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public State State
            {
                get
                {
                    throw new InvalidOperationException("Deferred.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("Deferred.Retain is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Retain()
            {
                throw new InvalidOperationException("Deferred.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.Release is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Release()
            {
                throw new InvalidOperationException("Deferred.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct Deferred
    } // struct Promise

    public partial struct Promise<T>
    {
        /// <summary>
        /// An instance of this is used to report progress and resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : ICancelable, IProgress<float>, IEquatable<Deferred>
        {
            internal readonly Internal.PromiseRefBase.DeferredPromise<T> _ref;
            internal readonly short _promiseId;
            internal readonly int _deferredId;

            void IProgress<float>.Report(float value)
            {
                ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public Promise<T> Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise<T>(_this, _promiseId, 0);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
                    return _this != null && _this.GetIsValid(_promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
                    return _this != null && _this.DeferredId == _deferredId;
                }
            }

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRefBase.DeferredPromise<T> promise, short promiseId, int deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled.
            /// </summary>
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                Internal.PromiseRefBase.DeferredPromise<T> promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRefBase.DeferredPromiseCancel<T>.GetOrCreate(cancelationToken)
                    : Internal.PromiseRefBase.DeferredPromise<T>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/> with <paramref name="value"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve(T value)
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.ResolveDirect(value);
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/> with <paramref name="value"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve(T value)
            {
                return Internal.PromiseRefBase.DeferredPromise<T>.TryResolve(_ref, _deferredId, value);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this == null || !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(_deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return Internal.DeferredPromiseHelper.TryReportProgress(_ref, _deferredId, progress);
            }

            /// <summary>
            /// Cast to <see cref="Promise.DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.DeferredBase(Deferred rhs)
            {
                return new Promise.DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);
            }

            /// <summary>
            /// Cast <see cref="Promise.DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(Promise.DeferredBase rhs)
            {
                return rhs.ToDeferred<T>();
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Deferred"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
            {
                return this == other;
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());
            }

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.State is no longer valid. Use IsValidAndPending.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public Promise.State State
            {
                get
                {
                    throw new InvalidOperationException("Deferred.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("Deferred.Retain is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Retain()
            {
                throw new InvalidOperationException("Deferred.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.Release is no longer valid.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public void Release()
            {
                throw new InvalidOperationException("Deferred.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct Deferred
    } // struct Promise<T>
} // namespace Proto.Promises