using System;
using System.Collections.Generic;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;

namespace TheAwaitingGame
{
    class Program
    {
        static void Main()
        {
            var summary = BenchmarkRunner.Run<Benchmarker>();
            Console.WriteLine(summary);
        }
    }
    [MemoryDiagnoser]
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
            for(int i = 0; i < 50; i++)
            {
                var order = new Order();
                int lines = rand.Next(1, 10);
                for (int j = 0; j < lines; j++)
                {
                    order.Lines.Add(new OrderLine
                    {
                        Quantity = rand.Next(1, 20),
                        UnitPrice = 0.01M * rand.Next(1,5000)
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
        public ValueTask<decimal> ValueTaskAsync() =>  _book.GetTotalWorthValueTaskAsync(REPEATS_PER_ITEM);

        [Benchmark]
        public ValueTask<decimal> HandCrankedAsync() => _book.GetTotalWorthHandCrankedAsync(REPEATS_PER_ITEM);

        [Benchmark]
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
        public async ValueTask<decimal> GetTotalWorthValueTaskAsync(int repeats)
        {
            decimal total = 0;
            while (repeats-- > 0)
            {
                foreach (var order in Orders) total += await order.GetOrderWorthValueTaskAsync();
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

        public ValueTask<decimal> GetOrderWorthAssertCompletedAsync()
        {
            decimal total = 0;
            foreach (var line in Lines) total += line.GetLineWorthValueTaskAsync().AssertCompleted();
            return new ValueTask<decimal>(total);
        }

        public ValueTask<decimal> GetOrderWorthHandCrankedAsync()
        {
            decimal total = 0;
            using (var iter = Lines.GetEnumerator())
            {
                while(iter.MoveNext())
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
        public ValueTask<decimal> GetLineWorthHandCrankedAsync() => new ValueTask<decimal>(Quantity * UnitPrice);
    }
}