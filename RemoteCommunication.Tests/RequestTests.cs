using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class RequestTests
    {
        [Fact]
        public async Task Hello()
        {
            var helloReceived = false;
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler("Hello", ct => { helloReceived = true; });
                await sender.SendRequest(tested.Address, "Hello");
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
                tested.AddRequestHandler<int, string, int>("Parameters", (p1, p2, ct) => { i = p1; st = p2; return p1 + 5; });
                var res = await sender.SendRequest<int>(tested.Address, "Parameters", 42, "Test");
                Assert.Equal(42, i);
                Assert.Equal("Test", st);
                Assert.Equal(47, res);
            });
        }

        [Fact]
        public async Task Exceptions()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler("Exception", ct => { throw new InvalidOperationException(); });
                await Assert.ThrowsAsync<FaultException>(async () => await sender.SendRequest<int>(tested.Address, "Exception"));
            });
        }

        [Fact]
        public async Task Cancellation()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<int>();
                tested.AddRequestHandler("LongWork", async ct => { tcs.SetResult(2); await Task.Delay(60000, ct).ConfigureAwait(false); });

                using (var cts = new CancellationTokenSource())
                {
                    var t = sender.SendRequest(tested.Address, "LongWork", cts.Token);
                    await tcs.Task.ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                    cts.Cancel();
                    await Assert.ThrowsAsync<OperationCanceledException>(() => t).ConfigureAwait(false);
                }
            });
        }

        [Fact]
        public async Task CancellationNonCancelling()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<int>();
                tested.AddRequestHandler("LongWork", async ct => { tcs.SetResult(2); await Task.Delay(5000).ConfigureAwait(false); });

                using (var cts = new CancellationTokenSource())
                {
                    var t = sender.SendRequest(tested.Address, "LongWork", cts.Token);
                    await tcs.Task.ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                    cts.Cancel();
                    await t.ConfigureAwait(false);
                }
            });
        }

        [Fact]
        public async Task KeepAliveTestedDead()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                var tcs = new TaskCompletionSource<int>();
                tested.AddRequestHandler("LongWork", async ct => { tcs.SetResult(2); await Task.Delay(60000).ConfigureAwait(false); });

                sender.AliveCheckInterval = TimeSpan.FromSeconds(3);
                var t = sender.SendRequest(tested.Address, "LongWork");
                await tcs.Task.ConfigureAwait(false);
                await Task.Delay(100).ConfigureAwait(false);
                tested.Dispose();
                await Assert.ThrowsAsync<TimeoutException>(() => t);
            });
        }

        [Fact]
        public async Task KeepAliveTestedAlive()
        {
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler("LongWork", ct => Task.Delay(10000));

                sender.AliveCheckInterval = TimeSpan.FromSeconds(3);
                await sender.SendRequest(tested.Address, "LongWork").ConfigureAwait(false);
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
