namespace RemoteCommunication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CommunicatorServiceHandlingExtensions
    {
        private static readonly Dictionary<Type, PropertyInfo> _taskResults = new Dictionary<Type, PropertyInfo>();

#if RemoteCommunication_ProxySupport
        public class CommunicatorProxy : DispatchProxy
        {
            private static readonly Dictionary<Type, MethodInfo> _sendRequests = new Dictionary<Type, MethodInfo>();

            internal Communicator Communicator { get; set; }
            internal string TargetAddress { get; set; }
            internal string VerbPrefix { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var cancellationTokenIndex = Array.FindIndex(args, p => p != null && p.GetType() == typeof(CancellationToken));
                var token = default(CancellationToken);
                if (cancellationTokenIndex >= 0)
                {
                    token = (CancellationToken)args[cancellationTokenIndex];
                    var l = args.ToList();
                    l.RemoveAt(cancellationTokenIndex);
                    args = l.ToArray();
                }

                if (targetMethod.ReturnType == typeof(Task))
                    return Communicator.SendRequest(TargetAddress, VerbPrefix + targetMethod.Name, token, args);
                if (targetMethod.ReturnType == typeof(void))
                {
                    Communicator.SendRequest(TargetAddress, VerbPrefix + targetMethod.Name, token, args).Wait();
                    return null;
                }

                var task = false;
                var returnType = targetMethod.ReturnType;

                if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    task = true;
                    returnType = targetMethod.ReturnType.GetGenericArguments().Single();
                }

                MethodInfo sendRequest;
                lock (_sendRequests)
                    if (!_sendRequests.TryGetValue(returnType, out sendRequest))
                    {
                        var openMethod = typeof(Communicator).GetMethods().Where(m => m.Name == "SendRequest" && m.IsGenericMethod && m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))).Single();
                        sendRequest = openMethod.MakeGenericMethod(returnType);
                        _sendRequests[returnType] = sendRequest;
                    }

                var res = sendRequest.Invoke(Communicator, new object[] { TargetAddress, VerbPrefix + targetMethod.Name, token, args });

                if (task)
                    return res;

                PropertyInfo resultProperty;
                lock (_taskResults)
                    if (!_taskResults.TryGetValue(returnType, out resultProperty))
                    {
                        var taskType = typeof(Task<>).MakeGenericType(returnType);

                        resultProperty = taskType.GetProperty("Result");
                        _taskResults[returnType] = resultProperty;
                    }

                return resultProperty.GetValue(res);
            }
        }

        public static T CreateProxy<T>(this Communicator communicator, string targetAddress, string verbPrefix = null)
        {
            var res = DispatchProxy.Create<T, CommunicatorProxy>();
            var proxy = (CommunicatorProxy)((object)res);
            proxy.Communicator = communicator;
            proxy.TargetAddress = targetAddress;
            proxy.VerbPrefix = verbPrefix;

            return res;
        }
#endif

        public static void AddSingletonService<T>(this Communicator communicator, T service, string verbPrefix = null)
        {
            foreach (var m in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var prs = m.GetParameters();
                var paramsCount = prs.Length;
                var cancellationTokenIndex = Array.FindIndex(prs, p => p.ParameterType == typeof(CancellationToken));
                if (cancellationTokenIndex >= 0)
                    paramsCount--;

                if (m.ReturnType == typeof(Task))
                    communicator.AddRequestHandler(verbPrefix + m.Name, async (p, ct) =>
                    {
                        await ((Task)m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct))).ConfigureAwait(false);
                        return null;
                    }, paramsCount);
                else if (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    PropertyInfo resultProperty;
                    lock (_taskResults)
                        if (!_taskResults.TryGetValue(m.ReturnType, out resultProperty))
                        {
                            resultProperty = m.ReturnType.GetProperty("Result");
                            _taskResults[m.ReturnType] = resultProperty;
                        }

                    communicator.AddRequestHandler(verbPrefix + m.Name, async (p, ct) =>
                    {
                        var result = m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct));
                        var task = (Task)result;
                        await task.ConfigureAwait(false);
                        return resultProperty.GetValue(result);
                    }, paramsCount);
                }
                else
                    communicator.AddRequestHandler(verbPrefix + m.Name, (p, ct) => m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct)), paramsCount);
            }
        }

        private static object[] AddCancellationToken(object[] parameters, int cancellationTokenIndex, CancellationToken token)
        {
            if (cancellationTokenIndex < 0)
                return parameters;
            var l = parameters.ToList();
            l.Insert(cancellationTokenIndex, token);
            return l.ToArray();
        }
    }
}
