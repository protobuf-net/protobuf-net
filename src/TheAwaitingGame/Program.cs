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
    }
    public class OrderLine
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GetLineWorth() => Quantity * UnitPrice;
        public Task<decimal> GetLineWorthTaskAsync() => Task.FromResult(Quantity * UnitPrice);
        public ValueTask<decimal> GetLineWorthValueTaskAsync() => new ValueTask<decimal>(Quantity * UnitPrice);
    }
}