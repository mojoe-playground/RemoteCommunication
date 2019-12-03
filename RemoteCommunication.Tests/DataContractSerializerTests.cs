using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class DataContractSerializerTests
    {
        [Fact]
        public void InternalTypeTest()
        {
            Assert.Throws<InvalidDataContractException>(() => new DataContractSerializer<InternalRequest>());
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
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new DataContractSerializer<Request>(), new DataContractSerializer<Response>()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new DataContractSerializer<Request>(), new DataContractSerializer<Response>()))
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

        [DataContract]
        public class Request
        {
            [DataMember]
            public string Name { get; set; }
        }

        [DataContract]
        public class Response
        {
            [DataMember]
            public int Length { get; set; }
        }
    }
}
