using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using MemoryDiagnoser = BenchmarkDotNet.Diagnosers.MemoryDiagnoser;

namespace TheAwaitingGame
{
    class Program
    {
        static void Main()
        {
            // tell BenchmarkDotNet not to force GC.Collect after benchmark iteration 
            // (single iteration contains of multiple (usually millions) of invocations)
            // it can influence the allocation-heavy Task<T> benchmarks
            var gcMode = new GcMode { Force = false };

            var customConfig = ManualConfig
                .Create(DefaultConfig.Instance) // copies all exporters, loggers and basic stuff
                .With(MemoryDiagnoser.Default) // use memory diagnoser
                .With(Job.Default.With(gcMode));

#if NET462
            // enable the Inlining Diagnoser to find out what does not get inlined
            // uncomment it first, it produces a lot of output
            //customConfig = customConfig.With(new BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser(logFailuresOnly: true, filterByNamespace: true));
#endif

            var summary = BenchmarkRunner.Run<Benchmarker>(customConfig);
            Console.WriteLine(summary);
        }
    }

    public class Benchmarker
    {
        static OrderBook _book;
        public Benchmarker()
        {
            // touch the static field to ensure .cctor has run
            GC.KeepAlive(_book);
        }
        static Benchmarker()
        {
            var rand = new Random(12345);

            var book = new OrderBook();
            for (int i = 0; i < 50; i++)
            {
                var order = new Order();
                int lines = rand.Next(1, 10);
                for (int j = 0; j < lines; j++)
                {
                    order.Lines.Add(new OrderLine
                    {
                        Quantity = rand.Next(1, 20),
                        UnitPrice = 0.01M * rand.Next(1, 5000)
                    });
                }
                book.Orders.Add(order);
            }
            _book = book;
        }

        const int REPEATS_PER_ITEM = 250;
        [Benchmark]
        public decimal Sync() => _book.GetTotalWorth(REPEATS_PER_ITEM);

        [Benchmark]
        public Task<decimal> TaskAsync() => _book.GetTotalWorthTaskAsync(REPEATS_PER_ITEM);

        [Benchmark]
        public ValueTask<decimal> ValueTaskAsync() => _book.GetTotalWorthValueTaskAsync(REPEATS_PER_ITEM);

        [Benchmark]
        public ValueTask<decimal> HandCrankedAsync() => _book.GetTotalWorthHandCrankedAsync(REPEATS_PER_ITEM);
    }
    class OrderBook
    {
        public List<Order> Orders { get; } = new List<Order>();

        public decimal GetTotalWorth(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders) total += order.GetOrderWorth();
            }
            return total;
        }
        public async Task<decimal> GetTotalWorthTaskAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders) total += await order.GetOrderWorthTaskAsync();
            }
            return total;
        }
        public async ValueTask<decimal> GetTotalWorthValueTaskAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders) total += await order.GetOrderWorthValueTaskAsync();
            }
            return total;
        }
        public ValueTask<decimal> GetTotalWorthHandCrankedAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                var iter = Orders.GetEnumerator();
                while (iter.MoveNext())
                {
                    var task = iter.Current.GetOrderWorthHandCrankedAsync();
                    if (!task.IsCompleted) return ContinueAsync(total, task, repeats, iter);
                    total += task.Result;
                }
            }
            return new ValueTask<decimal>(total);
        }

        private async ValueTask<decimal> ContinueAsync(decimal total, ValueTask<decimal> pending, int repeats, List<Order>.Enumerator iter)
        {
            total += await pending;
            do
            {
                while (iter.MoveNext())
                {
                    pending = iter.Current.GetOrderWorthHandCrankedAsync();
                    if (!pending.IsCompleted) return await ContinueAsync(total, pending, repeats, iter);
                    total += pending.Result;
                }
                iter = Orders.GetEnumerator();
            } while (repeats-- > 0);
            return total;
        }
    }
    class Order
    {
        public List<OrderLine> Lines { get; } = new List<OrderLine>();

        public decimal GetOrderWorth()
        {
            decimal total = 0;
            foreach (var line in Lines) total += line.GetLineWorth();
            return total;
        }
        public async Task<decimal> GetOrderWorthTaskAsync()
        {
            decimal total = 0;
            foreach (var line in Lines) total += await line.GetLineWorthTaskAsync();
            return total;
        }
        public async ValueTask<decimal> GetOrderWorthValueTaskAsync()
        {
            decimal total = 0;
            foreach (var line in Lines) total += await line.GetLineWorthValueTaskAsync();
            return total;
        }

        public ValueTask<decimal> GetOrderWorthHandCrankedAsync()
        {
            decimal total = 0;
            using (var iter = Lines.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    var task = iter.Current.GetLineWorthHandCrankedAsync();
                    if (!task.IsCompleted) return ContinueAsync(total, task, iter);
                    total += task.Result;
                }
            }
            return new ValueTask<decimal>(total);
        }

        async ValueTask<decimal> ContinueAsync(decimal total, ValueTask<decimal> pending, List<OrderLine>.Enumerator iter)
        {
            total += await pending;
            while (iter.MoveNext())
            {
                pending = iter.Current.GetLineWorthHandCrankedAsync();
                if (!pending.IsCompleted) return await ContinueAsync(total, pending, iter);
                total += pending.Result;
            }
            return total;
        }
    }
    public class OrderLine
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GetLineWorth() => Quantity * UnitPrice;
        public Task<decimal> GetLineWorthTaskAsync() => Task.FromResult(Quantity * UnitPrice);
        public ValueTask<decimal> GetLineWorthValueTaskAsync() => new ValueTask<decimal>(Quantity * UnitPrice);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // it fails to inline by default due to "Native estimate for function size exceeds threshold."
        public ValueTask<decimal> GetLineWorthHandCrankedAsync() => new ValueTask<decimal>(Quantity * UnitPrice);
    }
}