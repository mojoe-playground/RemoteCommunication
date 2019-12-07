namespace RemoteCommunication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CommunicatorExtensions
    {
        public static void AddMessageHandler(this Communicator communicator, string verb, Action handler) => communicator.AddMessageHandler(verb, p => handler(), 0);
        public static void AddMessageHandler<T>(this Communicator communicator, string verb, Action<T> handler) => communicator.AddMessageHandler(verb, p => handler((T)p[0]), 1);
        public static void AddMessageHandler<T1, T2>(this Communicator communicator, string verb, Action<T1, T2> handler) => communicator.AddMessageHandler(verb, p => handler((T1)p[0], (T2)p[1]), 2);
        public static void AddMessageHandler<T1, T2, T3>(this Communicator communicator, string verb, Action<T1, T2, T3> handler) => communicator.AddMessageHandler(verb, p => handler((T1)p[0], (T2)p[1], (T3)p[2]), 3);

        public static void AddMessageHandler(this Communicator communicator, string verb, Func<Task> handler) => communicator.AddMessageHandler(verb, async p => await handler().ConfigureAwait(false), 0);
        public static void AddMessageHandler<T>(this Communicator communicator, string verb, Func<T, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T)p[0]).ConfigureAwait(false), 1);
        public static void AddMessageHandler<T1, T2>(this Communicator communicator, string verb, Func<T1, T2, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T1)p[0], (T2)p[1]).ConfigureAwait(false), 2);
        public static void AddMessageHandler<T1, T2, T3>(this Communicator communicator, string verb, Func<T1, T2, T3, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T1)p[0], (T2)p[1], (T3)p[2]).ConfigureAwait(false), 3);

        public static void AddRequestHandler(this Communicator communicator, string verb, Action<CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler(c); return Task.FromResult<object>(null); }, 0);
        public static void AddRequestHandler<TRes>(this Communicator communicator, string verb, Func<CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler(c), 0);
        public static void AddRequestHandler<T>(this Communicator communicator, string verb, Action<T, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T)p[0], c); return Task.FromResult<object>(null); }, 1);
        public static void AddRequestHandler<T, TRes>(this Communicator communicator, string verb, Func<T, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T)p[0], c), 1);
        public static void AddRequestHandler<T1, T2>(this Communicator communicator, string verb, Action<T1, T2, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T1)p[0], (T2)p[1], c); return Task.FromResult<object>(null); }, 2);
        public static void AddRequestHandler<T1, T2, TRes>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T1)p[0], (T2)p[1], c), 2);
        public static void AddRequestHandler<T1, T2, T3>(this Communicator communicator, string verb, Action<T1, T2, T3, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T1)p[0], (T2)p[1], (T3)p[2], c); return Task.FromResult<object>(null); }, 3);
        public static void AddRequestHandler<T1, T2, T3, TRes>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T1)p[0], (T2)p[1], (T3)p[2], c), 3);

        public static void AddRequestHandler(this Communicator communicator, string verb, Func<CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler(c).ConfigureAwait(false); return null; }, 0);
        public static void AddRequestHandler<TRes>(this Communicator communicator, string verb, Func<CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler(c).ConfigureAwait(false), 0);
        public static void AddRequestHandler<T>(this Communicator communicator, string verb, Func<T, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T)p[0], c).ConfigureAwait(false); return null; }, 1);
        public static void AddRequestHandler<T, TRes>(this Communicator communicator, string verb, Func<T, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T)p[0], c).ConfigureAwait(false), 1);
        public static void AddRequestHandler<T1, T2>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T1)p[0], (T2)p[1], c).ConfigureAwait(false); return null; }, 2);
        public static void AddRequestHandler<T1, T2, TRes>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T1)p[0], (T2)p[1], c).ConfigureAwait(false), 2);
        public static void AddRequestHandler<T1, T2, T3>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T1)p[0], (T2)p[1], (T3)p[2], c).ConfigureAwait(false); return null; }, 3);
        public static void AddRequestHandler<T1, T2, T3, TRes>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T1)p[0], (T2)p[1], (T3)p[2], c).ConfigureAwait(false), 3);

    }
}
