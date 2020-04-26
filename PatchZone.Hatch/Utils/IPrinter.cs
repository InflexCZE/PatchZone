using System;

namespace PatchZone.Hatch.Utils
{
    public interface IPrinter
    {
        void Print(string s);
        void PrintNewLine();

        void IncreaseIndent();
        void DecreaseIndent();
    }

    public static class IPrinterExtensions
    {
        public static void PrintLine(this IPrinter printer, string line)
        {
            printer.Print(line);
            printer.PrintNewLine();
        }

        public static IndentToken Indent(this IPrinter printer, int count = 1)
        {
            return new IndentToken(printer, count);
        }

        public static CombinedToken<IndentToken, DeferredCommitToken> OpenScope(this IPrinter printer, string openValue, string closeValue, bool indent = true, bool newLine = true)
        {
            if (openValue != null)
            {
                printer.Print(openValue);

                if (newLine)
                {
                    printer.PrintNewLine();
                }
            }

            var t1 = new DeferredCommitToken(printer, closeValue, newLine);
            var t2 = new IndentToken(printer, indent ? 1 : 0);
            return new CombinedToken<IndentToken, DeferredCommitToken>(t2, t1);
        }

        public struct CombinedToken<T1, T2> : IDisposable
            where T1 : struct, IDisposable
            where T2 : struct, IDisposable
        {
            private T1 _1;
            private T2 _2;

            public CombinedToken(T1 _1, T2 _2)
            {
                this._1 = _1;
                this._2 = _2;
            }

            public void Dispose()
            {
                this._1.Dispose();
                this._2.Dispose();
            }
        }

        public struct IndentToken : IDisposable
        {
            private int Indents;
            private IPrinter Printer;

            public IndentToken(IPrinter printer, int indents = 1)
            {
                this.Printer = printer;
                this.Indents = indents;

                for (int i = 0; i < indents; i++)
                {
                    this.Printer.IncreaseIndent();
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < this.Indents; i++)
                {
                    this.Printer.DecreaseIndent();
                }
            }
        }
        public struct DeferredCommitToken : IDisposable
        {
            private string Value;
            private bool NewLine;
            private IPrinter Printer;

            public DeferredCommitToken(IPrinter printer, string value, bool newLine = false)
            {
                this.Value = value;
                this.NewLine = newLine;
                this.Printer = printer;
            }

            public void Dispose()
            {
                if (this.Value != null)
                {
                    this.Printer.Print(this.Value);
                }

                if (this.NewLine)
                {
                    this.Printer.PrintNewLine();
                }
            }
        }
    }
}
