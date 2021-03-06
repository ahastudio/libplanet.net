using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Libplanet.Net;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Libplanet.Tests.Net
{
    public class SwarmTest
    {
        private readonly List<Swarm> _swarms;

        public SwarmTest(ITestOutputHelper output)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()
                .WriteTo.TestOutput(output, outputTemplate: "{Timestamp:HH:mm:ss}[@{Swarm_listenUrl}][{ThreadId}] - {Message}")
                .CreateLogger()
                .ForContext<SwarmTest>();
            _swarms = new List<Swarm>
            {
                new Swarm(
                    new PrivateKey(),
                    new Uri($"inproc://swarmtest.a"),
                    3000),
                new Swarm(
                    new PrivateKey(),
                    new Uri($"inproc://swarmtest.b"),
                    3000),
                new Swarm(
                    new PrivateKey(),
                    new Uri($"inproc://swarmtest.c"),
                    3000),
            };
        }

        [Fact]
        public async Task WorksAsExpected()
        {
            Swarm a = _swarms[0];
            Swarm b = _swarms[1];
            Swarm c = _swarms[2];

            DateTime lastDistA;
            Peer aAsPeer;

            try
            {
                await b.InitContextAsync();
                await c.InitContextAsync();

                try
                {
                    await a.InitContextAsync();

                    var at = Task.Run(async () => await a.RunAsync(250));
                    await b.AddPeersAsync(new[] { a.AsPeer });
                    var bt = Task.Run(async () => await b.RunAsync(250));
                    await EnsureExchange(a, b);
                    Assert.Equal(new[] { b.AsPeer }.ToImmutableHashSet(), a.ToImmutableHashSet());
                    Assert.Equal(new[] { a.AsPeer }.ToImmutableHashSet(), b.ToImmutableHashSet());

                    await c.AddPeersAsync(new[] { a.AsPeer });
                    var ct = Task.Run(async () => await c.RunAsync(250));
                    await EnsureExchange(a, c);
                    await EnsureExchange(a, b);

                    Assert.Equal(new[] { b.AsPeer, c.AsPeer }.ToImmutableHashSet(), a.ToImmutableHashSet());
                    Assert.Equal(new[] { a.AsPeer, c.AsPeer }.ToImmutableHashSet(), b.ToImmutableHashSet());
                    Assert.Equal(new[] { a.AsPeer, b.AsPeer }.ToImmutableHashSet(), c.ToImmutableHashSet());

                    lastDistA = a.LastDistributed;
                    aAsPeer = a.AsPeer;
                }
                finally
                {
                    await a.DisposeAsync();
                }

                Assert.True(lastDistA < a.LastDistributed);

                await EnsureRecvAsync(b, aAsPeer, a.LastDistributed);
                await EnsureRecvAsync(c, aAsPeer, a.LastDistributed);
                await EnsureExchange(b, c);

                Assert.Equal(new[] { c.AsPeer }.ToImmutableHashSet(), b.ToImmutableHashSet());
                Assert.Equal(new[] { b.AsPeer }.ToImmutableHashSet(), c.ToImmutableHashSet());
            }
            finally
            {
                await b.DisposeAsync();
                await c.DisposeAsync();
            }
        }

        [Fact]
        public void WorksAsCollection()
        {
            Swarm a = _swarms[0];
            Swarm b = _swarms[1];
            Swarm c = _swarms[2];

            Assert.Empty(a);
            Assert.Empty(b);
            Assert.Empty(c);

            a.Add(b.AsPeer);
            a.Add(c.AsPeer);
            Assert.Contains(b.AsPeer, a);
            Assert.Contains(c.AsPeer, a);

            Peer[] peers = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                a.CopyTo(peers, 0);
            });

            peers = new Peer[3];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                a.CopyTo(peers, -1);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                a.CopyTo(peers, 2);
            });

            a.CopyTo(peers, 1);

            Assert.Equal(new Peer[] { null, b.AsPeer, c.AsPeer }, peers);
        }

        [Fact]
        public void CanBeComparedProperly()
        {
            var pk1 = new PrivateKey();
            var pk2 = new PrivateKey();
            var a = new Swarm(
                pk1,
                new Uri($"inproc://swarmtest.t"),
                3000);
            var b = new Swarm(
                pk1,
                new Uri($"inproc://swarmtest.t"),
                3000,
                a.LastDistributed);
            var c = new Swarm(
                pk2,
                new Uri($"inproc://swarmtest.t"),
                3000);
            var u1 = new Uri($"inproc://swarmtest.t");

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
            a.Add(c.AsPeer);

            Assert.NotEqual(a, b);
            Assert.True(a.KeyEquals(b));
        }

        private async Task EnsureRecvAsync(Swarm swarm, Peer peer = null, DateTime? lastReceived = null)
        {
            while (true)
            {
                Log.Debug($"Waiting for receive event... [{swarm.AsPeer}]");
                await swarm.DeltaReceived.WaitAsync();
                Log.Debug($"Event received. [{swarm.AsPeer}]");

                if (lastReceived == null)
                {
                    break;
                }

                DateTime? lastSeen = null;
                if (peer == null)
                {
                    lastSeen = swarm.LastReceived;
                }
                else
                {
                    if (swarm.LastSeenTimestamps.ContainsKey(peer))
                    {
                        lastSeen = swarm.LastSeenTimestamps[peer];
                    }
                    else
                    {
                        foreach (KeyValuePair<Peer, DateTime> kv in swarm.LastSeenTimestamps)
                        {
                            if (peer.PublicKey == kv.Key.PublicKey)
                            {
                                lastSeen = kv.Value;
                                break;
                            }
                        }
                    }
                }

                if ((lastSeen != null) && (lastSeen >= lastReceived))
                {
                    break;
                }
            }
        }

        private async Task EnsureExchange(Swarm a, Swarm b)
        {
            Log.Debug($"Waiting for a[{a.AsPeer}] to distribute... ");
            await a.DeltaDistributed.WaitAsync();
            Log.Debug($"Waiting for b[{b.AsPeer}] to receive a[{a.AsPeer}]...");
            await EnsureRecvAsync(b, a.AsPeer);
            Log.Debug($"Waiting for b[{b.AsPeer}] to distribute...");
            await b.DeltaDistributed.WaitAsync();
            Log.Debug($"Waiting for a[{a.AsPeer}] to receive b[{b.AsPeer}]...");
            await EnsureRecvAsync(a, b.AsPeer);
        }
    }
}
