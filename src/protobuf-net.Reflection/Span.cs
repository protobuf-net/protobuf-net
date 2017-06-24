using System;

namespace ProtoBuf.Reflection
{
    /// <summary>
    /// A Span is like a Token, except simpler; it can represent *multiple* tokens (either on a single line, or on
    /// multiple lines), but has no concept of what the data represents - it essentially just tracks the range
    /// </summary>
    internal struct Span : IEquatable<Span>
    {
        public override string ToString() => $"({StartLine},{StartColumn},{EndLine},{EndColumn})={Value}";
        public override bool Equals(object obj) => obj is Span && Equals((Span)obj);
        public bool Equals(Span other) => this.StartLine == other.StartLine && this.EndLine == other.EndLine
            && this.StartColumn == other.StartColumn && this.EndColumn == other.EndColumn;
        public override int GetHashCode()
        {
            int hash = -13 * StartColumn;
            hash = (17 * hash) + (-13 * EndColumn);
            hash = (17 * hash) + (-13 * StartLine);
            hash = (17 * hash) + (-13 * EndLine);
            return hash;
        }
        public readonly int StartLine, EndLine, StartColumn, EndColumn;
        public readonly string Value;
        public Span(int startLine, int startColumn, string value = null) : this(startLine, startColumn, startLine, startColumn + (value?.Length ?? 0), value) { }
        public Span(int startLine, int startColumn, int endLine, int endColumn, string value = null)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Value = value;
        }
        public bool HasValue => StartLine > EndLine || (StartLine == EndLine && StartColumn < EndColumn);
        public static Span operator +(Span x, Span y) => x.Merge(y);
        public static Span After(Token token) => new Span(token.LineNumber, token.ColumnNumber + (token.Value?.Length ?? 0), null);
        public static implicit operator Span(Token token) => new Span(token.LineNumber, token.ColumnNumber, token.Value);

        public Span Merge(Span other, string value = null)
        {
            int sl, sc, el, ec;
            if (StartLine == other.StartLine)
            {
                sl = StartLine;
                sc = Math.Min(StartColumn, other.StartColumn);
            }
            else if (StartLine < other.StartLine)
            {
                sl = StartLine;
                sc = StartColumn;
            }
            else
            {
                sl = other.StartLine;
                sc = other.StartColumn;
            }
            if (EndLine == other.EndLine)
            {
                el = EndLine;
                ec = Math.Max(EndColumn, other.EndColumn);
            }
            else if (EndLine < other.EndLine)
            {
                el = other.EndLine;
                ec = other.EndColumn;
            }
            else
            {
                el = EndLine;
                ec = EndColumn;
            }
            return new Span(sl, sc, el, ec, value);
        }

        internal Span WithValue(string value) => new Span(StartLine, StartColumn, EndLine, EndColumn, value);
    }
}
