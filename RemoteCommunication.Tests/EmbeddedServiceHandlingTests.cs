using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EmbeddedRemoteCommunication;

namespace RemoteCommunication.Tests
{
    public class EmbeddedServiceHandlingTests
    {
        [Fact]
        public async Task VoidMethodCallTest() => await SetupCommunicators((ct, cs, s, p) =>
        {
            p.VoidMethod();
            Assert.True(s.VoidCallIndicator);
            return Task.CompletedTask;
        });

        [Fact]
        public async Task TaskMethodCallTest() => await SetupCommunicators(async (ct, cs, s, p) =>
        {
            await p.TaskMethod();
            Assert.True(s.TaskCallIndicator);
        });

        [Fact]
        public async Task IntMethodCallTest() => await SetupCommunicators((ct, cs, s, p) =>
        {
            Assert.Equal(23, p.IntMethod(23));
            return Task.CompletedTask;
        });

        [Fact]
        public async Task TaskIntMethodCallTest() => await SetupCommunicators(async (ct, cs, s, p) =>
        {
            Assert.Equal(23, await p.TaskIntMethod(23));
        });

        [Fact]
        public async Task CancellationTest() => await SetupCommunicators(async (ct, cs, s, p) =>
        {
            var sw = new Stopwatch();
            var cancelled = false;
            using (var cts = new CancellationTokenSource())
                try
                {
                    sw.Start();
                    var t = p.CancellationMethod(55, cts.Token);
                    await s.CancellationMethodStarted.Task;
                    await Task.Delay(1000);
                    cts.Cancel();
                    await t;
                }
                catch (OperationCanceledException) { sw.Stop(); cancelled = true; }

            Assert.True(cancelled);
            Assert.False(s.CancellationMethodNotCancelled);
            Assert.True(sw.ElapsedMilliseconds < 15000);
        });

        private async Task SetupCommunicators(Func<EmbeddedRemoteCommunication.Communicator, EmbeddedRemoteCommunication.Communicator, Service, ITestService, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new EmbeddedRemoteCommunication.Communicator(new EmbeddedRemoteCommunication.NetPipeChannel(), "Tested" + id, new EmbeddedRemoteCommunication.BuiltInTypesSerializer()))
            using (var sender = new EmbeddedRemoteCommunication.Communicator(new EmbeddedRemoteCommunication.NetPipeChannel(), "Sender" + id, new EmbeddedRemoteCommunication.BuiltInTypesSerializer()))
            {
                await tested.Open();
                await sender.Open();
                var service = new Service();
                tested.AddSingletonService<ITestService>(service);
                var proxy = sender.CreateProxy<ITestService>(tested.Address);
                await test.Invoke(tested, sender, service, proxy);
            }
        }
    }
}
