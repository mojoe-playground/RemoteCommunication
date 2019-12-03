using System;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class XmlSerializerTests
    {
        [Fact]
        public void InternalTypeTest()
        {
            Assert.Throws<InvalidOperationException>(() => new XmlSerializer<InternalRequest>());
        }

        [Fact]
        public Task SerializationDeserialization() =>
            SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler<Request, Response>("GetLength", (r, ct) => Task.FromResult(new Response { Length = r.Name.Length }));
                Assert.Equal(4, (await sender.SendRequest<Response>(tested.Address, "GetLength", new Request { Name = "Test" })).Length);
            });

        private async Task SetupCommunicators(Func<Communicator, Communicator, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new XmlSerializer<Request>(), new XmlSerializer<Response>()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new XmlSerializer<Request>(), new XmlSerializer<Response>()))
            {
                await tested.Open();
                await sender.Open();
                await test.Invoke(tested, sender);
            }
        }

        internal class InternalRequest
        {
            public string Name { get; set; }
        }

        public class Request
        {
            public string Name { get; set; }
        }

        public class Response
        {
            public int Length { get; set; }
        }
    }
}
