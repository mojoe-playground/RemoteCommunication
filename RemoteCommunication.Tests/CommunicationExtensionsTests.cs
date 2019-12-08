using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class CommunicationExtensionsTests
    {
        [Fact]
        public async Task AddMessageHandler()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler("Msg", () => { tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg");
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddMessageHandlerP1()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int>("Msg", (p1) => { r1 = p1; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5);
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddMessageHandlerP2()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int, int>("Msg", (p1, p2) => { r1 = p1; r2 = p2; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5, 3);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddMessageHandlerP3()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int, int, int>("Msg", (p1, p2, p3) => { r1 = p1; r2 = p2; r3 = p3; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5, 3, 9);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        [Fact]
        public async Task AddMessageHandlerTask()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler("Msg", async () => { await Task.Delay(100); tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg");
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddMessageHandlerTaskP1()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int>("Msg", async (p1) => { await Task.Delay(100); r1 = p1; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5);
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddMessageHandlerTaskP2()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int, int>("Msg", async (p1, p2) => { await Task.Delay(100); r1 = p1; r2 = p2; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5, 3);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddMessageHandlerTaskP3()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddMessageHandler<int, int, int>("Msg", async (p1, p2, p3) => { await Task.Delay(100); r1 = p1; r2 = p2; r3 = p3; tcs.SetResult(true); });
                await sender.SendMessage(tested.Address, "Msg", 5, 3, 9);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        [Fact]
        public async Task AddRequestHandler()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler("Req", ct => { tcs.SetResult(true); });
                await sender.SendRequest(tested.Address, "Req");
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddRequestHandlerTask()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler("Req", ct => { tcs.SetResult(true); return Task.CompletedTask; });
                await sender.SendRequest(tested.Address, "Req");
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddRequestHandlerRes()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int>("Req", ct => { tcs.SetResult(true); return 5; });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req"));
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskRes()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int>("Req", ct => { tcs.SetResult(true); return Task.FromResult(5); });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req"));
                await tcs.Task;
            });
        }

        [Fact]
        public async Task AddRequestHandlerP1()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int>("Req", (p1, ct) => { r1 = p1; tcs.SetResult(true); });
                await sender.SendRequest(tested.Address, "Req", 5);
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP1()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int>("Req", (p1, ct) => { r1 = p1; tcs.SetResult(true); return Task.CompletedTask; });
                await sender.SendRequest(tested.Address, "Req", 5);
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddRequestHandlerP2()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int>("Req", (p1, p2, ct) => { r1 = p1; r2 = p2; tcs.SetResult(true); });
                await sender.SendRequest(tested.Address, "Req", 5, 3);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP2()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int>("Req", (p1, p2, ct) => { r1 = p1; r2 = p2; tcs.SetResult(true); return Task.CompletedTask; });
                await sender.SendRequest(tested.Address, "Req", 5, 3);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddRequestHandlerP3()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int>("Req", (p1, p2, p3, ct) => { r1 = p1; r2 = p2; r3 = p3; tcs.SetResult(true); });
                await sender.SendRequest(tested.Address, "Req", 5, 3, 9);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP3()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int>("Req", (p1, p2, p3, ct) => { r1 = p1; r2 = p2; r3 = p3; tcs.SetResult(true); return Task.CompletedTask; });
                await sender.SendRequest(tested.Address, "Req", 5, 3, 9);
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        [Fact]
        public async Task AddRequestHandlerP1Res()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int>("Req", (p1, ct) => { tcs.SetResult(true); r1 = p1; return 5; });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5));
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP1Res()
        {
            var r1 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int>("Req", (p1, ct) => { tcs.SetResult(true); r1 = p1; return Task.FromResult(5); });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5));
                await tcs.Task;
                Assert.Equal(5, r1);
            });
        }

        [Fact]
        public async Task AddRequestHandlerP2Res()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int>("Req", (p1, p2, ct) => { tcs.SetResult(true); r1 = p1; r2 = p2; return 5; });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5, 3));
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP2Res()
        {
            var r1 = 0;
            var r2 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int>("Req", (p1, p2, ct) => { tcs.SetResult(true); r1 = p1; r2 = p2; return Task.FromResult(5); });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5, 3));
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
            });
        }

        [Fact]
        public async Task AddRequestHandlerP3Res()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int, int>("Req", (p1, p2, p3, ct) => { tcs.SetResult(true); r1 = p1; r2 = p2; r3 = p3; return 5; });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5, 3, 9));
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        [Fact]
        public async Task AddRequestHandlerTaskP3Res()
        {
            var r1 = 0;
            var r2 = 0;
            var r3 = 0;
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tested.AddRequestHandler<int, int, int, int>("Req", (p1, p2, p3, ct) => { tcs.SetResult(true); r1 = p1; r2 = p2; r3 = p3; return Task.FromResult(5); });
                Assert.Equal(5, await sender.SendRequest<int>(tested.Address, "Req", 5, 3, 9));
                await tcs.Task;
                Assert.Equal(5, r1);
                Assert.Equal(3, r2);
                Assert.Equal(9, r3);
            });
        }

        private async Task SetupCommunicators(Func<Communicator, Communicator, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new BuiltInTypesSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new BuiltInTypesSerializer()))
            {
                await tested.Open();
                await sender.Open();
                await test.Invoke(tested, sender);
            }
        }
    }
}
