#if CF35
using System.Collections.Generic;
using System.Reflection;
using System;
namespace System.Linq.Expressions
{
    /// <summary>
    /// The type of expression (used as a discriminator)
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// Represents a constant value
        /// </summary>
        Constant,
        /// <summary>
        /// Represents a call to a method
        /// </summary>
        Call,
        /// <summary>
        /// Represents a lambda expression, complete with arguments
        /// </summary>
        Lambda,
        /// <summary>
        /// Represents a parameter used in an expression
        /// </summary>
        Parameter,
        /// <summary>
        /// Represents field/property access
        /// </summary>
        MemberAccess
    }

    /// <summary>
    /// Represents a node in an expression tree
    /// </summary>
    public abstract class Expression
    {
        internal static List<T> BuildList<T>(T[] values) where T : Expression
        {
            return values == null || values.Length == 0
                ? new List<T>(0) : new List<T>(values);
        }
        internal Expression(ExpressionType nodeType)
        {
            NodeType = nodeType;
        }

        /// <summary>
        /// Indicates the type of the concrete expression
        /// </summary>
        public ExpressionType NodeType { get; private set; }
        
        /// <summary>
        /// Creates a new expression node represnting a constant value
        /// </summary>
        public static ConstantExpression Constant(object value)
        {
            return Constant(value, value == null ? typeof(object) : value.GetType());
        }

        /// <summary>
        /// Creates a new expression node represnting a constant value
        /// </summary>
        public static ConstantExpression Constant(object value, Type type)
        {
            return new ConstantExpression(value, type);
        }

        /// <summary>
        /// Creates a new parameter for use in an expression tree
        /// </summary>
        public static ParameterExpression Parameter(Type type, string name)
        {
            return new ParameterExpression(type, name);
        }

        /// <summary>
        /// Creates a new expression node representing method invokation 
        /// </summary>
        public static MethodCallExpression Call(Expression instance, MethodInfo method, params Expression[] args)
        {
            return new MethodCallExpression(instance, method, args);
        }

        /// <summary>
        /// Creates a completed expression tree
        /// </summary>
        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] args)
        {
            return new LambdaExpression(body, args);
        }

        /// <summary>
        /// Creates a completed expression tree
        /// </summary>
        public static Expression<T> Lambda<T>(Expression body, params ParameterExpression[] args) where T : class
        {
            return new Expression<T>(body, args);
        }

        /// <summary>
        /// Creates a new expression node reading a value from a field
        /// </summary>
        public static MemberExpression Field(Expression instance, FieldInfo field)
        {
            return new MemberExpression(field, instance);
        }

        /// <summary>
        /// Creates a new expression node reading a value from a property
        /// </summary>
        public static MemberExpression Property(Expression instance, PropertyInfo field)
        {
            return new MemberExpression(field, instance);
        }

        /// <summary>
        /// Creates a new expression node reading a value from a property
        /// </summary>
        public static MemberExpression Property(Expression instance, MethodInfo accessor)
        {
            PropertyInfo property = null;
            if (accessor != null)
            {
                Type declaringType = accessor.DeclaringType;
                foreach (PropertyInfo prop in declaringType.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic
                    | (accessor.IsStatic ? BindingFlags.Static : BindingFlags.Instance)))
                {
                    MethodInfo method;
                    if (prop.CanRead &&
                        (   (method = prop.GetGetMethod(true)) == accessor
                        ||  (declaringType.IsInterface && method.Name == accessor.Name)))
                    {
                        property = prop;
                        break;
                    }
                }
            }
            return new MemberExpression(property, instance);
        }
    }

    /// <summary>
    /// Represents method invokation as an expression-tree node
    /// </summary>
    public class MethodCallExpression : Expression
    {
        internal MethodCallExpression(object instance, MethodInfo method, Expression[] args)
            : base(ExpressionType.Call)
        {
            Object = instance;
            Method = method;
            Arguments = BuildList(args);
        }

        /// <summary>
        /// The method to be invoked
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// The target object for the method (null if static)
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// The arguments to be passed to the method
        /// </summary>
        public List<Expression> Arguments { get; private set; }
    }

    /// <summary>
    /// Represents member access as an expression-tree node
    /// </summary>
    public class MemberExpression : Expression
    {
        /// <summary>
        /// The member to be accessed
        /// </summary>
        public MemberInfo Member { get; private set; }

        /// <summary>
        /// The target instance holding the value (null if static)
        /// </summary>
        public Expression Expression { get; private set; }
        internal MemberExpression(MemberInfo member, Expression expression)
            : base(ExpressionType.MemberAccess)
        {
            Member = member;
            Expression = expression;
        }
    }

    /// <summary>
    /// Represents a constant value as an expression-tree node
    /// </summary>
    public class ConstantExpression : Expression
    {
        internal ConstantExpression(object value, Type type) : base(ExpressionType.Constant) {
            Value = value;
            Type = type;
        }
        /// <summary>
        /// The type of value represented
        /// </summary>
        public Type Type { get; private set; }
        /// <summary>
        /// The value represented
        /// </summary>
        public object Value { get; private set; }
    }

    /// <summary>
    /// Represents a parameter used in a lambda as an expression-tree node
    /// </summary>
    public class ParameterExpression : Expression
    {
        internal ParameterExpression(Type type, string name) : base(ExpressionType.Parameter) {
            Type = type;
            Name = name;
        }
        /// <summary>
        /// The type of value represented by this parameter
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The name of the parameter
        /// </summary>
        public string Name { get; private set; }
    }

    /// <summary>
    /// Represents an expression-tree
    /// </summary>
    public class LambdaExpression : Expression
    {
        internal LambdaExpression(Expression body, ParameterExpression[] args)
            : base(ExpressionType.Lambda)
        {
            Parameters = BuildList(args);
            Body = body;
        }
        /// <summary>
        /// The root operation for the expression to perform
        /// </summary>
        public Expression Body { get; private set; }
        /// <summary>
        /// The parameters used in the expression (at any level)
        /// </summary>
        public List<ParameterExpression> Parameters { get; private set; }
    }

    /// <summary>
    /// Represents an expression-tree
    /// </summary>
    public class Expression<T> : LambdaExpression where T : class
    {
        internal Expression(Expression body, ParameterExpression[] args) : base(body, args) {}
    }
}

#endif
