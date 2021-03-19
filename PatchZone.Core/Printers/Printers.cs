﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PatchZone.Hatch.Utils;

namespace PatchZone.Core.Printers
{
    //TODO: Refactor & Split to files

    public class StringBuilderPrinter : IPrinter
    {
        public StringBuilder SB { get; }
        public int CurrentIndentLevel { get; set; }

        private bool InsertIndent;

        public StringBuilderPrinter(StringBuilder sb = null)
        {
            this.InsertIndent = true;
            this.SB = sb ?? new StringBuilder();
        }

        public void Print(string s)
        {
            if (this.InsertIndent)
            {
                this.InsertIndent = false;
                this.SB.Append(' ', this.CurrentIndentLevel * 4);
            }

            this.SB.Append(s);
        }

        public void PrintNewLine()
        {
            this.SB.AppendLine();
            this.InsertIndent = true;
        }

        public void IncreaseIndent()
        {
            this.CurrentIndentLevel++;
        }

        public void DecreaseIndent()
        {
            Debug.Assert(this.CurrentIndentLevel > 0);
            this.CurrentIndentLevel--;
        }

        private static StringBuilderPrinter m_cachedInstance;

        internal static StringBuilderPrinter Borrow()
        {
            var instance = Interlocked.Exchange(ref m_cachedInstance, null);
            return instance ?? new StringBuilderPrinter();
        }

        internal static string GetStringAndReturn(StringBuilderPrinter instance)
        {
            var result = instance.SB.ToString();
            Return(instance);
            return result;
        }

        internal static void Return(StringBuilderPrinter instance)
        {
            instance.SB.Clear();
            Interlocked.Exchange(ref m_cachedInstance, instance);
        }
    }

    public class FilePrinter : IPrinter, IDisposable
    {
        private readonly StreamWriter File;

        private bool InsertIndent;
        private int CurrentIndentLevel;

        public bool AutoCommit
        {
            get => this.File.AutoFlush;
            set => this.File.AutoFlush = value;
        }

        public FilePrinter(string path)
        {
            this.File = new StreamWriter(System.IO.File.Create(path));
        }

        public void Print(string s)
        {
            if (this.InsertIndent)
            {
                this.InsertIndent = false;
                File.Write(GetIndentString(this.CurrentIndentLevel));
            }

            File.Write(s);
        }

        public void PrintNewLine()
        {
            File.WriteLine();
            File.Flush();
            this.InsertIndent = true;
        }

        public void IncreaseIndent()
        {
            this.CurrentIndentLevel++;
        }

        public void DecreaseIndent()
        {
            Debug.Assert(this.CurrentIndentLevel > 0);
            this.CurrentIndentLevel--;
        }

        public void Dispose()
        {
            this.File.Dispose();
        }

        private static string[] IndentStringCache;
        private static string GetIndentString(int level)
        {
        Retry:
            var args = Volatile.Read(ref IndentStringCache);

            if (args == null || args.Length <= level)
            {
                var newSize = ((level / 25) + 1) * 25;
                var newCache = new string[newSize];

                int i = 0;
                if (args != null)
                {
                    i = args.Length;
                    Array.Copy(args, newCache, i);
                }

                for (; i < newCache.Length; i++)
                {
                    newCache[i] = new string(' ', i * 4);
                }

                if (Interlocked.CompareExchange(ref IndentStringCache, newCache, args) == args)
                {
                    args = newCache;
                }
                else
                {
                    goto Retry;
                }
            }

            return args[level];
        }
    }

    public class NullPrinter : IPrinter
    {
        public void Print(string s)
        { }

        public void PrintNewLine()
        { }

        public void IncreaseIndent()
        { }

        public void DecreaseIndent()
        { }
    }

    public class CompoundPrinter : IPrinter, IDisposable
    {
        private readonly IPrinter _1, _2;

        public CompoundPrinter(IPrinter _1, IPrinter _2)
        {
            this._1 = _1;
            this._2 = _2;
        }

        public void Print(string s)
        {
            _1.Print(s);
            _2.Print(s);
        }

        public void PrintNewLine()
        {
            _1.PrintNewLine();
            _2.PrintNewLine();
        }

        public void IncreaseIndent()
        {
            _1.IncreaseIndent();
            _2.IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            _1.DecreaseIndent();
            _2.DecreaseIndent();
        }

        public void Dispose()
        {
            if (_1 is IDisposable _1d)
            {
                _1d.Dispose();
            }

            if (_2 is IDisposable _2d)
            {
                _2d.Dispose();
            }
        }

        public static IPrinter Make(params IPrinter[] printers)
        {
            if (printers.Length == 0)
            {
                return new NullPrinter();
            }

            if (printers.Length == 1)
            {
                return printers[0];
            }

            var current = new CompoundPrinter(printers[0], printers[1]);

            for (int i = 2; i < printers.Length; i++)
            {
                current = new CompoundPrinter(current, printers[i]);
            }

            return current;
        }
    }

    public class PrefixPrinter : IPrinter, IDisposable
    {
        public string Prefix { get; set; }

        private bool InsertPrefix;
        private readonly IPrinter InnerPrinter;

        public PrefixPrinter(IPrinter innerPrinter, string prefix = "")
        {
            this.Prefix = prefix;
            this.InnerPrinter = innerPrinter;
        }

        public void Print(string s)
        {
            if (this.InsertPrefix)
            {
                this.InsertPrefix = false;
                this.InnerPrinter.Print(this.Prefix);
            }

            this.InnerPrinter.Print(s);
        }

        public void PrintNewLine()
        {
            this.InsertPrefix = true;
            this.InnerPrinter.PrintNewLine();
        }

        public void IncreaseIndent()
        {
            this.InnerPrinter.IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            this.InnerPrinter.DecreaseIndent();
        }

        public void Dispose()
        {
            if (this.InnerPrinter is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
