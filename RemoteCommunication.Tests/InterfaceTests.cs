using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xunit;
using System.Runtime.Serialization;

namespace RemoteCommunication.Tests
{
    public class InterfaceTests
    {
        [Fact]
        public async Task XmlInterface()
        {
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + Guid.NewGuid(), new XmlSerializer<ITest>(new XmlSerializer(typeof(ConcreteTest), new XmlRootAttribute(nameof(ITest))))))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + Guid.NewGuid(), new XmlSerializer<ITest>(new XmlSerializer(typeof(OtherTest), new XmlRootAttribute(nameof(ITest))))))
            {
                tested.AddRequestHandler<ITest>("Result", ct => new ConcreteTest { Bool = true });
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<ITest>("Message", t => tcs.SetResult(t.Bool));

                await tested.Open();
                await sender.Open();

                await sender.SendMessage(tested.Address, "Message", new OtherTest { Bool = true });
                Assert.True(await tcs.Task);

                var r = await sender.SendRequest<ITest>(tested.Address, "Result");
                Assert.True(r.Bool);
            }
        }

        [Fact]
        public async Task DataContractInterface()
        {
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + Guid.NewGuid(), new DataContractSerializer<ITest>(new DataContractSerializer(typeof(ConcreteTest), nameof(ITest), "http://test"))))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + Guid.NewGuid(), new DataContractSerializer<ITest>(new DataContractSerializer(typeof(OtherTest), nameof(ITest), "http://test"))))
            {
                tested.AddRequestHandler<ITest>("Result", ct => new ConcreteTest { Bool = true });
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<ITest>("Message", t => tcs.SetResult(t.Bool));

                await tested.Open();
                await sender.Open();

                await sender.SendMessage(tested.Address, "Message", new OtherTest { Bool = true });
                Assert.True(await tcs.Task);

                var r = await sender.SendRequest<ITest>(tested.Address, "Result");
                Assert.True(r.Bool);
            }
        }

        [Fact]
        public async Task ObjectInterface()
        {
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + Guid.NewGuid(), new ObjectSerializer<ITest, ConcreteTest>(), new BuiltInTypesSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + Guid.NewGuid(), new ObjectSerializer<ITest, OtherTest>(), new BuiltInTypesSerializer()))
            {
                tested.AddRequestHandler<ITest>("Result", ct => new ConcreteTest { Bool = true });
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<ITest>("Message", t => tcs.SetResult(t.Bool));

                await tested.Open();
                await sender.Open();

                await sender.SendMessage(tested.Address, "Message", new OtherTest { Bool = true });
                Assert.True(await tcs.Task);

                var r = await sender.SendRequest<ITest>(tested.Address, "Result");
                Assert.True(r.Bool);
            }
        }
    }

    public interface ITest
    {
        bool Bool { get; set; }
    }

    public class ConcreteTest : ITest
    {
        public bool Bool { get; set; }
    }

    public class OtherTest : ITest
    {
        public bool Bool { get; set; }
    }
}
