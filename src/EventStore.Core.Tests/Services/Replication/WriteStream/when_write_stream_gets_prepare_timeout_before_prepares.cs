using System;
using System.Collections.Generic;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.RequestManager.Managers;
using EventStore.Core.Tests.Fakes;
using EventStore.Core.Tests.Helpers;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Replication.WriteStream
{
    [TestFixture, Ignore("WriteStream operation is not 2-phase now, it does not expect PrepareAck anymore.")]
    public class when_write_stream_gets_prepare_timeout_before_prepares : RequestManagerSpecification
    {
        protected override TwoPhaseRequestManagerBase OnManager(FakePublisher publisher)
        {
            return new WriteStreamTwoPhaseRequestManager(publisher, 3, 3, PrepareTimeout, CommitTimeout, false);
        }

        protected override IEnumerable<Message> WithInitialMessages()
        {
            yield return new ClientMessage.WriteEvents(InternalCorrId, ClientCorrId, Envelope, true, "test123", ExpectedVersion.Any, new[] { DummyEvent() }, null);
        }

        protected override Message When()
        {
            return new StorageMessage.RequestManagerTimerTick(DateTime.UtcNow + PrepareTimeout + TimeSpan.FromMinutes(1));
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
            Assert.That(Envelope.Replies.ContainsSingle<ClientMessage.WriteEventsCompleted>(
                x => x.CorrelationId == ClientCorrId && x.Result == OperationResult.PrepareTimeout));
        }
    }
}