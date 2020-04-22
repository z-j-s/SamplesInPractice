﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using WeihanLi.Common.Helpers;

namespace AopSample
{
    public class AspectDelegate
    {
        private static readonly ConcurrentDictionary<string, Action<MethodInvocationContext>> _aspectDelegates = new ConcurrentDictionary<string, Action<MethodInvocationContext>>();

        public static void InvokeAspectDelegate(MethodInvocationContext context)
        {
            var action = _aspectDelegates.GetOrAdd($"{context.ProxyMethod.DeclaringType}.{context.ProxyMethod}", m =>
            {
                var builder = PipelineBuilder.Create<MethodInvocationContext>(x => x.Invoke());

                if (context.MethodBase != null)
                {
                    foreach (var aspect in context.MethodBase.GetCustomAttributes<AbstractAspect>(true))
                    {
                        builder.Use(aspect.Invoke);
                    }
                }
                else if (context.ProxyMethod != null)
                {
                    foreach (var aspect in context.ProxyMethod.GetCustomAttributes<AbstractAspect>(true))
                    {
                        builder.Use(aspect.Invoke);
                    }
                }
                return builder.Build();
            });
            action.Invoke(context);

            // check for return value
            if (context.ProxyMethod.ReturnType != typeof(void))
            {
                if (context.ReturnValue == null && context.ProxyMethod.ReturnType.IsValueType)
                {
                    context.ReturnValue = Activator.CreateInstance(context.ProxyMethod.ReturnType);
                }
            }
        }
    }
}
