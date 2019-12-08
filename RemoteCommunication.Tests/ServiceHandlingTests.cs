using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class ServiceHandlingTests
    {
        [Fact]
        public async Task ShutdownTest() => await SetupCommunicators(async (ct, cs, s, p) =>
        {
            var t = p.TaskIntMethod(55);
            await s.TaskIntMethodStarted.Task;
            await ct.Shutdown();
            Assert.Equal(55, await t);
        });

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
        public async Task VoidMethodVerbCallTest() => await SetupCommunicators("test", (ct, cs, s, p) =>
         {
             p.VoidMethod();
             Assert.True(s.VoidCallIndicator);
             return Task.CompletedTask;
         });

        [Fact]
        public async Task TaskMethodVerbCallTest() => await SetupCommunicators("test", async (ct, cs, s, p) =>
         {
             await p.TaskMethod();
             Assert.True(s.TaskCallIndicator);
         });

        [Fact]
        public async Task IntMethodVerbCallTest() => await SetupCommunicators("test", (ct, cs, s, p) =>
         {
             Assert.Equal(23, p.IntMethod(23));
             return Task.CompletedTask;
         });

        [Fact]
        public async Task TaskIntMethodVerbCallTest() => await SetupCommunicators("test", async (ct, cs, s, p) =>
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

        private Task SetupCommunicators(Func<Communicator, Communicator, Service, ITestService, Task> test) => SetupCommunicators(null, test);
        private async Task SetupCommunicators(string verb, Func<Communicator, Communicator, Service, ITestService, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(), "Tested" + id, new BuiltInTypesSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new BuiltInTypesSerializer()))
            {
                await tested.Open();
                await sender.Open();
                var service = new Service();
                tested.AddSingletonService<ITestService>(service, verb);
                var proxy = sender.CreateProxy<ITestService>(tested.Address, verb);
                await test.Invoke(tested, sender, service, proxy);
            }
        }
    }

    public interface ITestService
    {
        void VoidMethod();
        Task TaskMethod();
        int IntMethod(int number);
        Task<int> TaskIntMethod(int number);
        Task<int> CancellationMethod(int number, CancellationToken token);
    }

    internal class Service : ITestService
    {
        public TaskCompletionSource<bool> CancellationMethodStarted { get; set; } = new TaskCompletionSource<bool>();
        public bool CancellationMethodNotCancelled { get; set; }
        public TaskCompletionSource<bool> TaskIntMethodStarted { get; set; } = new TaskCompletionSource<bool>();
        public bool TaskCallIndicator { get; set; }
        public bool VoidCallIndicator { get; set; }

        public async Task<int> CancellationMethod(int number, CancellationToken token) { CancellationMethodStarted.SetResult(true); await Task.Delay(30000, token); CancellationMethodNotCancelled = true; return number; }
        public int IntMethod(int number) => number;
        public async Task<int> TaskIntMethod(int number) { TaskIntMethodStarted.SetResult(true); await Task.Delay(5000); return number; }
        public async Task TaskMethod() { await Task.Delay(1000); TaskCallIndicator = true; }

        public void VoidMethod() => VoidCallIndicator = true;
    }
}
