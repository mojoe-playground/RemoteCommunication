﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using OldCommunicator = EmbeddedRemoteCommunication.v04.Communicator;
using OldNetPipeChannel = EmbeddedRemoteCommunication.v04.NetPipeChannel;
using OldSimpleValueSerializer = EmbeddedRemoteCommunication.v04.SimpleValueSerializer;
using OldExtensions = EmbeddedRemoteCommunication.v04.CommunicatorExtensions;
using OldFaultException = EmbeddedRemoteCommunication.v04.FaultException;

namespace RemoteCommunication.Tests.Compatibility
{
    public class CompatibilityWithV04
    {
        [Fact]
        public async Task OldServerNewClient()
        {
            var id = Guid.NewGuid();
            using (var server = new OldCommunicator(new OldNetPipeChannel(), "Server" + id, new OldSimpleValueSerializer()))
            using (var client = new Communicator(new NetPipeChannel(), "Client" + id, new SimpleValueSerializer()))
            {
                await server.Open();
                await client.Open();

                var tcs = new TaskCompletionSource<int>();

                OldExtensions.AddRequestHandler<int, string, double, decimal>(server, "Req", (i, s, d, ct) => 0.55m);
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                OldExtensions.AddRequestHandler<int, string, double, decimal>(server, "Fault", async (i, s, d, ct) => throw new InvalidOperationException());
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                OldExtensions.AddRequestHandler<int, string, double, decimal>(server, "Cancel", async (i, s, d, ct) => { tcs.SetResult(2); await Task.Delay(60000, ct); return 1.1m; });
                OldExtensions.AddMessageHandler<byte[], object>(server, "Req", (b, o) => { });

                await client.SendMessage(server.Address, "Req", new byte[] { 2, 56, 2 }, null);
                Assert.Equal(0.55m, await client.SendRequest<decimal>(server.Address, "Req", 234, "Hello", 0.2));
                await Assert.ThrowsAsync<FaultException>(async () => await client.SendRequest<decimal>(server.Address, "Fault", 33, "bbs", 12.4));
                using (var cts = new CancellationTokenSource())
                {
                    var t = client.SendRequest(server.Address, "Cancel", cts.Token, 44, "23324wds", 0.22);
                    await tcs.Task;
                    cts.Cancel();
                    await Assert.ThrowsAsync<OperationCanceledException>(() => t);
                }
            }
        }
        [Fact]
        public async Task NewServerOldClient()
        {
            var id = Guid.NewGuid();
            using (var server = new Communicator(new NetPipeChannel(), "Server" + id, new SimpleValueSerializer()))
            using (var client = new OldCommunicator(new OldNetPipeChannel(), "Client" + id, new OldSimpleValueSerializer()))
            {
                await server.Open();
                await client.Open();

                var tcs = new TaskCompletionSource<int>();

                CommunicatorExtensions.AddRequestHandler<int, string, double, decimal>(server, "Req", (i, s, d, ct) => 0.55m);
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                CommunicatorExtensions.AddRequestHandler<int, string, double, decimal>(server, "Fault", async (i, s, d, ct) => throw new InvalidOperationException());
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                CommunicatorExtensions.AddRequestHandler<int, string, double, decimal>(server, "Cancel", async (i, s, d, ct) => { tcs.SetResult(2); await Task.Delay(60000, ct); return 1.1m; });
                CommunicatorExtensions.AddMessageHandler<byte[], object>(server, "Req", (b, o) => { });

                await client.SendMessage(server.Address, "Req", new byte[] { 2, 56, 2 }, null);
                Assert.Equal(0.55m, await client.SendRequest<decimal>(server.Address, "Req", 234, "Hello", 0.2));
                await Assert.ThrowsAsync<OldFaultException>(async () => await client.SendRequest<decimal>(server.Address, "Fault", 33, "bbs", 12.4));
                using (var cts = new CancellationTokenSource())
                {
                    var t = client.SendRequest(server.Address, "Cancel", cts.Token, 44, "23324wds", 0.22);
                    await tcs.Task;
                    cts.Cancel();
                    await Assert.ThrowsAsync<OperationCanceledException>(() => t);
                }
            }
        }
    }
}
