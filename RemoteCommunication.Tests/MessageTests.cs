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
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler("Hello", () => tcs.SetResult(true));
                await sender.SendMessage(tested.Address, "Hello");
                Assert.Equal(tcs.Task, await Task.WhenAny(tcs.Task, Task.Delay(5000)));
                Assert.True(tcs.Task.Result);
            });
        }

        [Fact]
        public async Task Parameters()
        {
            var i = 0;
            var st = string.Empty;

            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>(sender);
                tested.AddMessageHandler<int, string>("Parameters", (p1, p2) => { i = p1; st = p2; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Parameters", 42, "Test");
                Assert.Equal(tcs.Task, await Task.WhenAny(tcs.Task, Task.Delay(5000)));
                Assert.Equal(42, i);
                Assert.Equal("Test", st);
            });
        }

        private async Task SetupCommunicators(Func<Communicator, Communicator, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new BuiltInTypesSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new BuiltInTypesSerializer()))
            {
                await tested.Open();
                await test.Invoke(tested, sender);
            }
        }
    }
}
