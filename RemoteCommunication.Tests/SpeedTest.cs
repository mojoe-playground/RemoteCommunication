using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RemoteCommunication.Tests
{
    public class SpeedTestFixture
    {
        public IReadOnlyList<byte[]> Data { get; }

        public SpeedTestFixture()
        {
            var data = new List<byte[]>();
            Data = data;

            var random = new Random();
            var len = 1000;
            var blocksize = 1024 * 1024;
            for (var i = 0; i < len; i++)
            {
                var block = new byte[blocksize];
                random.NextBytes(block);
                data.Add(block);
            }
        }
    }

    public class SpeedTest : IClassFixture<SpeedTestFixture>
    {
        public SpeedTest(SpeedTestFixture fixutre, ITestOutputHelper log)
        {
            Log = log;
            Fixture = fixutre;
        }

        SpeedTestFixture Fixture { get; }
        ITestOutputHelper Log { get; }

        [Fact]
        public async Task RequestMegabyte()
        {
            const int numberOfMessages = 1000;

            var random = new Random();

            var sw = new Stopwatch();
            sw.Start();
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler<byte[], byte[]>("SendData", (b, ct) => Fixture.Data[random.Next(Fixture.Data.Count)]);

                for (var i = 0; i < numberOfMessages; i++)
                    await sender.SendRequest(tested.Address, "SendData", Fixture.Data[random.Next(Fixture.Data.Count)]);
            });
            sw.Stop();
            Log.WriteLine($"Total time: {sw.Elapsed}, {numberOfMessages / sw.Elapsed.TotalSeconds:N3} MB/sec");
        }

        [Fact]
        public async Task RequestSmall()
        {
            const int numberOfMessages = 50000;

            var sw = new Stopwatch();
            sw.Start();
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddRequestHandler<int, int>("SendData", (b, ct) => -b);

                for (var i = 0; i < numberOfMessages; i++)
                    await sender.SendRequest(tested.Address, "SendData", i);
            });
            sw.Stop();
            Log.WriteLine($"Total time: {sw.Elapsed}, {numberOfMessages / sw.Elapsed.TotalSeconds:N0} message/sec");
        }

        [Fact]
        public async Task MessageMegabyte()
        {
            const int numberOfMessages = 1000;

            var random = new Random();

            var sw = new Stopwatch();
            sw.Start();
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddMessageHandler<byte[]>("SendData", b => { });

                for (var i = 0; i < numberOfMessages; i++)
                    await sender.SendMessage(tested.Address, "SendData", Fixture.Data[random.Next(Fixture.Data.Count)]);
            });
            sw.Stop();
            Log.WriteLine($"Total time: {sw.Elapsed}, {numberOfMessages / sw.Elapsed.TotalSeconds:N3} MB/sec");
        }

        [Fact]
        public async Task MultiChannelMessageMegabyte()
        {
            const int numberOfMessages = 1000;
            const int numberOfChannels = 5;

            var random = new Random();

            var sw = new Stopwatch();
            sw.Start();
            await SetupCommunicators(numberOfChannels, async (tested, sender) =>
            {
                tested.AddMessageHandler<byte[]>("SendData", b => { });

                for (var i = 0; i < numberOfMessages; i++)
                    await sender.SendMessage(tested.Address, "SendData", Fixture.Data[random.Next(Fixture.Data.Count)]);
            });
            sw.Stop();
            Log.WriteLine($"Total time: {sw.Elapsed}, {numberOfMessages / sw.Elapsed.TotalSeconds:N3} MB/sec");
        }

        [Fact]
        public async Task MessageSmall()
        {
            const int numberOfMessages = 50000;

            var sw = new Stopwatch();
            sw.Start();
            await SetupCommunicators(async (tested, sender) =>
            {
                tested.AddMessageHandler<int>("SendData", b => { });

                for (var i = 0; i < numberOfMessages; i++)
                    await sender.SendMessage(tested.Address, "SendData", i);
            });
            sw.Stop();
            Log.WriteLine($"Total time: {sw.Elapsed}, {numberOfMessages / sw.Elapsed.TotalSeconds:N0} message/sec");
        }

        private Task SetupCommunicators(Func<Communicator, Communicator, Task> test) => SetupCommunicators(1, test);
        private async Task SetupCommunicators(int numberOfChannels, Func<Communicator, Communicator, Task> test)
        {
            var id = Guid.NewGuid();
            using (var tested = new Communicator(new NetPipeChannel(numberOfChannels), "Tested" + id, new BuiltInTypesSerializer()))
            using (var sender = new Communicator(new NetPipeChannel(), "Sender" + id, new BuiltInTypesSerializer()))
            {
                await tested.Open();
                await sender.Open();
                await test.Invoke(tested, sender);
            }
        }
    }
}
