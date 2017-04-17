using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using MemoryDiagnoser = BenchmarkDotNet.Diagnosers.MemoryDiagnoser;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Columns;

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
                .With(JitOptimizationsValidator.FailOnError) // Fail if not release mode
                .With(MemoryDiagnoser.Default) // use memory diagnoser
                .With(StatisticColumn.OperationsPerSecond) // add ops/s
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
        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public decimal Sync() => _book.GetTotalWorth(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public Task<decimal> TaskAsync() => _book.GetTotalWorthTaskAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public Task<decimal> TaskCheckedAsync() => _book.GetTotalWorthTaskCheckedAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> ValueTaskAsync() => _book.GetTotalWorthValueTaskAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> ValueTaskWrappedAsync() => _book.GetTotalWorthValueTaskCheckedWrappedAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> ValueTaskDecimalReferenceAsync() => _book.GetTotalWorthValueTaskCheckedDecimalReferenceAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> ValueTaskCheckedAsync() => _book.GetTotalWorthValueTaskCheckedAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> HandCrankedAsync() => _book.GetTotalWorthHandCrankedAsync(REPEATS_PER_ITEM);

        [Benchmark(OperationsPerInvoke = REPEATS_PER_ITEM)]
        public ValueTask<decimal> AssertCompletedAsync() => _book.GetTotalWorthAssertCompletedAsync(REPEATS_PER_ITEM);
    }

    public static class ValueTaskExtensions
    {
        public static T AssertCompleted<T>(this ValueTask<T> task)
        {
            if(!task.IsCompleted)
            {
                throw new InvalidOperationException();
            }
            return task.Result;
        }
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
        public async Task<decimal> GetTotalWorthTaskCheckedAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders)
                {
                    var task = order.GetOrderWorthTaskCheckedAsync();
                    total += (task.IsCompleted) ? task.Result : await task;
                }
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
        public async ValueTask<decimal> GetTotalWorthValueTaskCheckedAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders)
                {
                    var task = order.GetOrderWorthValueTaskCheckedAsync();
                    total += (task.IsCompleted) ? task.Result : await task;
                }
            }
            return total;
        }
        public async ValueTask<decimal> GetTotalWorthValueTaskCheckedWrappedAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders)
                {
                    var task = order.GetOrderWorthValueTaskCheckedAsync();
                    total += await task.AsTask();
                }
            }
            return total;
        }
        public async ValueTask<decimal> GetTotalWorthValueTaskCheckedDecimalReferenceAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders)
                {
                    var task = order.GetOrderWorthAssertCompletedDecimalReferenceAsync();
                    total += (task.IsCompleted) ? task.Result.Value : (await task).Value;
                }
            }
            return total;
        }
        public ValueTask<decimal> GetTotalWorthAssertCompletedAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders) total += order.GetOrderWorthAssertCompletedAsync().AssertCompleted();
            }
            return new ValueTask<decimal>(total);
        }
        public ValueTask<decimal> GetTotalWorthHandCrankedAsync(int repeats)
        {
            decimal total = 0;

            var orders = Orders;
            var count = orders.Count;
            var task = default(ValueTask<decimal>);

            while (repeats-- > 0)
            {
                var i = 0;
                for (; i < count; i++)
                {
                    task = orders[i].GetOrderWorthHandCrankedAsync();
                    if (!task.IsCompleted) break;
                    total += task.Result;
                }

                if (i < count)
                {
                    return ContinueAsync(total, task, repeats, i);
                }
            }
            return new ValueTask<decimal>(total);
        }
        private async ValueTask<decimal> ContinueAsync(decimal total, ValueTask<decimal> task, int repeats, int i)
        {
            total += await task;

            var orders = Orders;
            while (repeats-- > 0)
            {
                var count = orders.Count;
                i = 0;
                for (; i < count; i++)
                {
                    task = orders[i].GetOrderWorthHandCrankedAsync();
                    total += (task.IsCompleted) ? task.Result : await task;
                }
            }

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
        public async Task<decimal> GetOrderWorthTaskCheckedAsync()
        {
            decimal total = 0;
            foreach (var line in Lines)
            {
                var task = line.GetLineWorthTaskAsync();
                total += (task.IsCompleted) ? task.Result : await task;
            }
            return total;
        }
        public async ValueTask<decimal> GetOrderWorthValueTaskAsync()
        {
            decimal total = 0;
            foreach (var line in Lines) total += await line.GetLineWorthValueTaskAsync();
            return total;
        }
        public async ValueTask<decimal> GetOrderWorthValueTaskCheckedAsync()
        {
            decimal total = 0;
            foreach (var line in Lines)
            {
                var task = line.GetLineWorthValueTaskAsync();
                total += (task.IsCompleted) ? task.Result : await task;
            }
            return total;
        }

        public async ValueTask<DecimalReference> GetOrderWorthAssertCompletedDecimalReferenceAsync()
        {
            decimal total = 0;
            foreach (var line in Lines)
            {
                var task = line.GetLineWorthValueTaskDecimalReferenceAsync();
                total += (task.IsCompleted) ? task.Result.Value : (await task).Value;
            }
            return new DecimalReference(total);
        }


        public ValueTask<decimal> GetOrderWorthAssertCompletedAsync()
        {
            decimal total = 0;
            foreach (var line in Lines) total += line.GetLineWorthValueTaskAsync().AssertCompleted();
            return new ValueTask<decimal>(total);
        }

        public ValueTask<decimal> GetOrderWorthHandCrankedAsync()
        {
            decimal total = 0;

            var currentTask = default(ValueTask<decimal>);
            var lines = Lines;
            var count = lines.Count;

            var i = 0;
            for (; i < count; i++)
            {
                currentTask = lines[i].GetLineWorthValueTaskAsync();
                if (!currentTask.IsCompleted) break;
                total += currentTask.Result;
            }

            if (i < count) return ContinueAsync(total, currentTask, i);
            return new ValueTask<decimal>(total);
        }

        async ValueTask<decimal> ContinueAsync(decimal total, ValueTask<decimal> currentTask, int i)
        {
            total += await currentTask;

            var count = Lines.Count;

            for (; i < count; i++)
            {
                currentTask = Lines[i].GetLineWorthValueTaskAsync();
                if (currentTask.IsCompleted)
                    total += currentTask.Result;
                else
                    total += await currentTask;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // it fails to inline by default due to "Native estimate for function size exceeds threshold."
        public ValueTask<decimal> GetLineWorthValueTaskAsync() => new ValueTask<decimal>(Quantity * UnitPrice);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<DecimalReference> GetLineWorthValueTaskDecimalReferenceAsync() => new ValueTask<DecimalReference>(new DecimalReference(Quantity * UnitPrice));
    }
    public class DecimalReference
    {
        public decimal Value;
        public DecimalReference(decimal value)
        {
            Value = value;
        }
    }
}