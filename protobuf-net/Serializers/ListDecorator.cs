#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    sealed class ListDecorator : ProtoDecoratorBase
    {
        private readonly Type declaredType, concreteType;
        private readonly bool isList;
        private readonly MethodInfo add;
        public ListDecorator(Type declaredType, Type concreteType, IProtoSerializer tail) : base(tail)
        {
            if (declaredType == null) throw new ArgumentNullException("declaredType");
            this.declaredType = declaredType;
            isList = typeof(IList).IsAssignableFrom(declaredType);
            Type[] types = { tail.ExpectedType };
            add = declaredType.GetMethod("Add", types);
            if(add == null)
            {
                types[0] = typeof(object);
                add = declaredType.GetMethod("Add", types);
            }
            if (add == null && isList)
            {
                add = typeof(IList).GetMethod("Add", types);
            }
            if (add == null) throw new InvalidOperationException();
        }

        public override Type ExpectedType { get { return declaredType;  } }
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return true; } }
        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            using (Compiler.Local position = new Compiler.Local(ctx, typeof(int)))
            {
                Compiler.CodeLabel @continue = ctx.DefineLabel();
                ctx.MarkLabel(@continue);

                throw new NotImplementedException(); // incomplete
            }
        }
        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            throw new NotImplementedException();
        }
        public override void Write(object value, ProtoWriter dest)
        {
            foreach (object subItem in (IEnumerable)value)
            {
                if (subItem == null) { throw new NullReferenceException(); }
                Tail.Write(subItem, dest);
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            if (value == null) value = Activator.CreateInstance(concreteType);
            if (isList)
            {
                IList list = (IList)value;
                do
                {
                    list.Add(Tail.Read(null, source));
                } while (source.TryReadFieldHeader(field));
            }
            else
            {
                object[] args = new object[1];
                do
                {
                    args[0] = Tail.Read(null, source);
                    add.Invoke(value, args);
                } while (source.TryReadFieldHeader(field));
            }
            
            return value;
        }

    }
}
#endif