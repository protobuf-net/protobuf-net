#if !SILVERLIGHT
using System;
using System.Linq.Expressions;
using System.Reflection;
namespace ProtoBuf.ServiceModel.Client
{
    /// <summary>
    /// Provides extension methods for interacting with RPC via expressions, rather than
    /// code-generation or reflection.
    /// </summary>
    public static class ProtoClientExtensions
    {
        /// <summary>
        /// Performs a synchronous RPC operation on the given service.
        /// </summary>
        /// <typeparam name="TService">The service being used.</typeparam>
        /// <typeparam name="TResult">The result of the RPC call.</typeparam>
        /// <param name="client">The client to use to invoke the RPC call.</param>
        /// <param name="operation">An expression indicating the operation to perform.</param>
        /// <returns>The value of the RPC call.</returns>
        public static TResult Invoke<TService, TResult>(
            this ProtoClient<TService> client,
            Expression<Func<TService, TResult>> operation) where TService : class
        {
            Action updateArgs;
            object[] args;
            MethodInfo method = ResolveMethod<TService>(operation, out updateArgs, out args);
            TResult result = (TResult) client.Invoke(method, args);
            if (updateArgs != null) updateArgs();
            return result;
        }

        /// <summary>
        /// Performs a synchronous RPC operation on the given service.
        /// </summary>
        /// <typeparam name="TService">The service being used.</typeparam>
        /// <param name="client">The client to use to invoke the RPC call.</param>
        /// <param name="operation">An expression indicating the operation to perform.</param>
        public static void Invoke<TService>(
            this ProtoClient<TService> client,
            Expression<Action<TService>> operation) where TService : class
        {
            Action updateArgs;
            object[] args;
            MethodInfo method = ResolveMethod<TService>(operation, out updateArgs, out args);
            client.Invoke(method, args);
            if (updateArgs != null) updateArgs();
        }

        /// <summary>
        /// Performs an asynchronous RPC operation on the given service.
        /// </summary>
        /// <typeparam name="TService">The service being used.</typeparam>
        /// <typeparam name="TResult">The result of the RPC call.</typeparam>
        /// <param name="client">The client to use to invoke the RPC call.</param>
        /// <param name="operation">An expression indicating the operation to perform.</param>
        /// <param name="callback">A delegate that is invoked when the operation is complete. The
        /// callback is additionally given an `Action` that can be invoked to obtain the return
        /// value of the call, or to raise any exception
        /// associated with the call.</param>
        public static void InvokeAsync<TService, TResult>(this ProtoClient<TService> client, Expression<Func<TService, TResult>> operation, Action<Func<TResult>> callback) where TService : class
        {
            Action updateArgs;
            object[] args;
            MethodInfo method = ResolveMethod<TService>(operation, out updateArgs, out args);

            client.InvokeAsync(method, result =>
            {
                if (updateArgs != null) updateArgs();
                callback(() => (TResult)result() );
            });
        }
        /// <summary>
        /// Performs an asynchronous RPC operation on the given service.
        /// </summary>
        /// <typeparam name="TService">The service being used.</typeparam>
        /// <param name="client">The client to use to invoke the RPC call.</param>
        /// <param name="operation">An expression indicating the operation to perform.</param>
        /// <param name="callback">A delegate that is invoked when the operation is complete. The
        /// callback is additionally given an `Action` that can be invoked to raise any exception
        /// associated with the call.</param>
        public static void InvokeAsync<TService>(this ProtoClient<TService> client, Expression<Action<TService>> operation, Action<Action> callback) where TService : class
        {
            Action updateArgs;
            object[] args;
            MethodInfo method = ResolveMethod<TService>(operation, out updateArgs, out args);

            client.InvokeAsync(method, result =>
            {
                if (updateArgs != null) updateArgs();
                callback(() => { result(); });
            });
        }

        private static MethodInfo ResolveMethod<TService>(Expression operation, out Action callback, out object[] args)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            var lambda = operation as LambdaExpression;
            if (lambda == null) throw new ArgumentException("LambdaExpression expected", "operation");
            if (lambda.Parameters.Count != 1 || lambda.Parameters[0].Type != typeof(TService))
            {
                throw new ArgumentException("The lambda was expected to take a single argument of type " + typeof(TService));
            }
            var call = lambda.Body as MethodCallExpression;
            if (call == null || call.Object != lambda.Parameters[0]) throw new ArgumentNullException("Methods must invoked directly on the supplied service instance");

            args = new object[call.Arguments.Count];
            object[] argsCpy = args; // used for capture
            ParameterInfo[] parameters = call.Method.GetParameters();
            callback = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    FieldInfo field;
                    object target;
                    if (!TryResolveField(call.Arguments[i], out field, out target))
                    {
                        throw new InvalidOperationException("Cannot guarantee ref/out behaviour due to expression complexity; consider simplifying the expression.");
                    }
                    args[i] = RpcUtils.IsRequestArgument(parameters[i]) ? field.GetValue(target) : null;
                    int iCpy = i; // for capture
                    callback += delegate { field.SetValue(target, argsCpy[iCpy]); };
                }
                else
                {
                    args[i] = Evaluate(call.Arguments[i]);
                }
            }
            return call.Method;
        }

        /// <summary>
        /// Checks that the expression is a field-based member-access operation; if so, it attempts
        /// to resolve the instance hosting the field. This is used as the basis of by-ref arguments.
        /// </summary>
        private static bool TryResolveField(Expression operation, out FieldInfo field, out object target)
        {
            MemberExpression me;
            if (operation.NodeType == ExpressionType.MemberAccess
                && (field = (me = (MemberExpression)operation).Member as FieldInfo) != null)
            {
                target = Evaluate(me.Expression);
                return true;
            }
            field = null;
            target = null;
            return false;
        }
        private static object Evaluate(Expression operation)
        {
            object value;
            if (!TryEvaluate(operation, out value))
            {
#if CF35
                throw new NotSupportedException("The expression is too complicated to be evaluated on this framework; try simplifying the expression.");
#else
                // use compile / invoke as a fall-back
                value = Expression.Lambda(operation).Compile().DynamicInvoke();
#endif
            }
            return value;
        }
        private static bool TryEvaluate(Expression operation, out object value)
        {
            if (operation == null)
            {   // used for static fields, etc
                value = null;
                return true;
            }
            switch (operation.NodeType)
            {
                case ExpressionType.Constant:
                    value = ((ConstantExpression)operation).Value;
                    return true;
                case ExpressionType.MemberAccess:
                    MemberExpression me = (MemberExpression)operation;
                    object target;
                    if (TryEvaluate(me.Expression, out target))
                    { // instance target
                        switch (me.Member.MemberType)
                        {
                            case MemberTypes.Field:
                                value = ((FieldInfo)me.Member).GetValue(target);
                                return true;
                            case MemberTypes.Property:
                                value = ((PropertyInfo)me.Member).GetValue(target, null);
                                return true;
                        }
                    }
                    break;
            }
            value = null;
            return false;
        }
    }
}
#endif