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

using System;

namespace Proto.Promises
{
    public abstract partial class Promise
    {
        /// <summary>
        /// Capture a value and add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress)
        {
            SubscribeProgress(progressCaptureValue, onProgress, 1);
            return this;
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and the cancelation reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel, ReasonContainer> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegateCapture<TCaptureCancel>.GetOrCreate(cancelCaptureValue, onCanceled, 1));
            }
            return this;
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// </summary>
        public Promise Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(finallyCaptureValue, onFinally, 1));
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolve<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolve<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolvePromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolvePromise<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureVoidVoid<TCapture>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinue0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureVoidResult<TCapture, TResult>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureVoidResult<TCapture, Promise>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureVoidResult<TCapture, Promise<TResult>>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }

    /// <summary>
    /// A <see cref="Promise{T}"/> represents the eventual result of an asynchronous operation.
    /// The primary way of interacting with a <see cref="Promise{T}"/> is through its then method,
    /// which registers callbacks to be invoked with its resolve value when the <see cref="Promise{T}"/> is resolved,
    /// or the reason why the <see cref="Promise{T}"/> cannot be resolved.
    /// </summary>
    public abstract partial class Promise<T> : Promise
    {
        /// <summary>
        /// Capture a value and add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public new Promise<T> Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress)
        {
            SubscribeProgress(progressCaptureValue, onProgress, 1);
            return this;
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and the cancelation reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public new Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel, ReasonContainer> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegateCapture<TCaptureCancel>.GetOrCreate(cancelCaptureValue, onCanceled, 1));
            }
            return this;
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// </summary>
        public new Promise<T> Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(finallyCaptureValue, onFinally, 1));
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolve<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolve<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolvePromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolvePromise<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureArgVoid<TCapture, T>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinue0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureArgResult<TCapture, T, TResult>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureArgResult<TCapture, T, Promise>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueCaptureArgResult<TCapture, T, Promise<TResult>>.GetOrCreate(continueCaptureValue, onContinue);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}