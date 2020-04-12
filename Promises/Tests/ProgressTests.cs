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

#if PROMISE_PROGRESS
using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class ProgressTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = null;
        }

        [TearDown]
        public void Teardown()
        {
            Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p)
                .Catch(() => { });

            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenAPromiseIsRejectedAndContinueBeingInvokedWhenAChainedPromisesProgressIsUpdated()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            deferred.Promise
                .Catch(() => deferred2.Promise)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            var cancelable = deferred.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => progress = p);

            deferred.ReportProgress(0.25f);
            cancelable.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled2()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            var cancelable = deferred.Promise
                .ThenDuplicate()
                .Then(() => deferred2.Promise)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            cancelable.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled3()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => deferred2.Promise)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            deferred2.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MultipleOnProgressAreInvokedProperly()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;
            bool firstInvoked = false;
            bool secondInvoked = false;

            var cancelable = deferred.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => { firstInvoked = true; progress = p; });

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(firstInvoked);
            Assert.IsFalse(secondInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;
            cancelable.Cancel();

            deferred2.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => { secondInvoked = true; progress = p; });

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;

            deferred.Resolve();
            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void OnProgressWillNotBeInvokedWith1UntilPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.ReportProgress(1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnProgressIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(default(Action<float>));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(default(Action<float>));
            });

            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillOnlyBeInvokedWithAValueBetween0And1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Promise.Progress(p =>
            {
                Assert.GreaterOrEqual(p, 0f);
                Assert.LessOrEqual(p, 1f);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(1.1f));

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        // A wait promise is a promise that waits on another promise.
        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain0()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain2()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain3()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif