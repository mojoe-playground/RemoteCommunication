using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class MessageTests
    {
        [Fact]
        public async Task Hello()
        {
            var helloReceived = false;
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddMessageHandler("Hello", () => helloReceived = true);
                await sender.SendMessage(tested.Address, "Hello");
                Thread.Sleep(1000);
                Assert.True(helloReceived);
            });
        }

        [Fact]
        public async Task Parameters()
        {
            var i = 0;
            var st = string.Empty;

            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddMessageHandler<int, string>("Parameters", (p1, p2) => { i = p1; st = p2; });
                await sender.SendMessage(tested.Address, "Parameters", 42, "Test");
                Thread.Sleep(1000);
                Assert.Equal(42, i);
                Assert.Equal("Test", st);
            });
        }
        
        private async Task SetupCommunicators(Func<Communicator, Communicator, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new SimpleValueSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new SimpleValueSerializer()))
            {
                await tested.Open();
                await test.Invoke(tested, sender);
            }
        }
    }
}
