﻿#pragma warning disable IDE0051 // Remove unused private members

using Proto.Promises.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static partial class InternalHelper
    {
        // We initialize the config as early as possible. Ideally we would just do this in static constructors of Promise(<T>) and Promise.Config,
        // but since this is in a separate assembly, that's not possible.
        // Also, using static constructors would slightly slow down promises in IL2CPP where it would have to check if it already ran on every call.
#if UNITY_2019_2_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#elif UNITY_2019_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        internal static void InitializePromiseConfig()
        {
            PromiseBehaviour.Init();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        [AddComponentMenu("")] // Hide this in the add component menu.
        internal sealed class PromiseBehaviour : MonoBehaviour
        {
            internal static bool s_isApplicationQuitting = false;

            private static PromiseBehaviour s_instance;

            private readonly PromiseSynchronizationContext _syncContext = new PromiseSynchronizationContext();
            private Queue<UnhandledException> _currentlyReportingExceptions = new Queue<UnhandledException>();
            private Queue<UnhandledException> _unhandledExceptions = new Queue<UnhandledException>();

            internal static void Init()
            {
                // Even though we try to initialize this as early as possible, it is possible for other code to run before this.
                // So we need to be careful to not overwrite non-default values.

#pragma warning disable 0612 // Type or member is obsolete
                if (Promise.Config.WarningHandler == null)
                {
                    // Set default warning handler to route to UnityEngine.Debug.
                    Promise.Config.WarningHandler = UnityEngine.Debug.LogWarning;
                }
#pragma warning restore 0612 // Type or member is obsolete

                // Create a PromiseBehaviour instance before any promise actions are made.
                // Unity will throw if this is not ran on the main thread.
                new GameObject("Proto.Promises.Unity.PromiseBehaviour")
                    .AddComponent<PromiseBehaviour>()
                    .SetSynchronizationContext();
            }

            private void SetSynchronizationContext()
            {
                if (Promise.Config.ForegroundContext == null)
                {
                    Promise.Config.ForegroundContext = _syncContext;
                }
                if (Promise.Config.UncaughtRejectionHandler == null)
                {
                    // Intercept uncaught rejections and report them in Update instead of directly sending them to UnityEngine.Debug.LogException
                    // so that we can minimize the extra stack frames in the logs that we don't care about.
                    Promise.Config.UncaughtRejectionHandler = HandleRejection;
                }
            }

            private void Start()
            {
                if (s_instance != null)
                {
                    UnityEngine.Debug.LogWarning("There can only be one instance of PromiseBehaviour. Destroying new instance.");
                    Destroy(this);
                    return;
                }
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
                s_instance = this;
                StartCoroutine(UpdateRoutine());
            }

            // This should never be called except when the application is shutting down.
            // Users would have to go out of their way to find and destroy the PromiseBehaviour instance.
            private void OnDestroy()
            {
                if (s_isApplicationQuitting)
                {
                    return;
                }
                if (s_instance == this)
                {
                    UnityEngine.Debug.LogWarning("PromiseBehaviour destroyed! Removing PromiseSynchronizationContext from Promise.Config.ForegroundContext.");
                    s_instance = null;
                    if (Promise.Config.ForegroundContext == _syncContext)
                    {
                        Promise.Config.ForegroundContext = null;
                    }
                    if (Promise.Config.UncaughtRejectionHandler == HandleRejection)
                    {
                        Promise.Config.UncaughtRejectionHandler = null;
                    }
                    _syncContext.Execute(); // Clear out any pending callbacks.
                }
            }

            private void HandleRejection(UnhandledException exception)
            {
                lock (_unhandledExceptions)
                {
                    _unhandledExceptions.Enqueue(exception);
                }
            }

            // Execute SynchronizationContext callback in Coroutine rather than in Update.
            private IEnumerator UpdateRoutine()
            {
                // We end up missing the first frame here, but that's not a big deal.
                while (true)
                {
                    yield return null;
                    try
                    {
                        _syncContext.Execute();
                    }
                    // In case someone clears `Promise.Config.UncaughtRejectionHandler`, we catch the AggregateException here and log it so that the coroutine won't stop.
                    catch (AggregateException e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            private void Update()
            {
                // Pop and pass to UnityEngine.Debug here so Unity won't add extra stackframes that we don't care about.
                object locker = _unhandledExceptions;
                lock (locker)
                {
                    var temp = _unhandledExceptions;
                    _unhandledExceptions = _currentlyReportingExceptions;
                    _currentlyReportingExceptions = temp;
                }

                while (_currentlyReportingExceptions.Count > 0)
                {
                    // Unfortunately, Unity does not provide a means to completely eliminate the stack trace at the point of calling `Debug.Log`, so the log will always have at least 1 extra stack frame.
                    // This implementation minimizes it to 1 extra stack frame always (because `Update()` is called from Unity's side, and they do not include their own internal stack traces).
                    UnityEngine.Debug.LogException(_currentlyReportingExceptions.Dequeue());
                }
            }

            private void OnApplicationQuit()
            {
                s_isApplicationQuitting = true;
            }
        }
    }
}