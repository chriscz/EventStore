using System;
using System.Collections.Generic;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.RequestManager.Managers;
using EventStore.Core.Tests.Fakes;
using EventStore.Core.Tests.Helpers;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Replication.TransactionCommit
{
    [TestFixture]
    public class when_transaction_commit_gets_prepare_timeout_before_prepares : RequestManagerSpecification
    {
        protected override TwoPhaseRequestManagerBase OnManager(FakePublisher publisher)
        {
            return new TransactionCommitTwoPhaseRequestManager(publisher, 3, 3, PrepareTimeout, CommitTimeout, false);
        }

        protected override IEnumerable<Message> WithInitialMessages()
        {
            yield return new ClientMessage.TransactionCommit(InternalCorrId, ClientCorrId, Envelope, true, 4, null);
        }

        protected override Message When()
        {
            return new StorageMessage.RequestManagerTimerTick(DateTime.UtcNow + PrepareTimeout + TimeSpan.FromMinutes(5));
        }

        [Test]
        public void failed_request_message_is_published()
        {
            Assert.That(Produced.ContainsSingle<StorageMessage.RequestCompleted>(
                x => x.CorrelationId == InternalCorrId && x.Success == false));
        }

        [Test]
        public void the_envelope_is_replied_to_with_failure()
        {
            Assert.That(Envelope.Replies.ContainsSingle<ClientMessage.TransactionCommitCompleted>(
                x => x.CorrelationId == ClientCorrId && x.Result == OperationResult.PrepareTimeout));
        }
    }
}