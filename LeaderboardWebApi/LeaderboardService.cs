using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace LeaderboardWebApi
{
    public class LeaderboardService
    {
        private readonly SkipList _skipList = new SkipList(16, 0.5);
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ConcurrentQueue<(long customerId, decimal score)> _updateQueue = new ConcurrentQueue<(long, decimal)>();
        private readonly Timer _updateTimer;
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public LeaderboardService()
        {
            // Timer used to process batch updates
            _updateTimer = new Timer(ProcessUpdates, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        private void ProcessUpdates(object state)
        {
            _lock.EnterWriteLock();
            try
            {
                while (_updateQueue.TryDequeue(out var update))
                {
                    _skipList.Insert(update.customerId, update.score);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public decimal UpdateScore(long customerId, decimal score)
        {
            // Score must be in the range of [-1000, +1000]
            if (score < -1000 || score > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Score must be in the range of [-1000, +1000].");
            }
            // Directly update the score
            var currentScore = _skipList.GetCustomer(customerId)?.Score ?? 0; // Get the current score
            var newScore = currentScore + score; // Calculate the new score
            // Use the queue for batch updates
            _updateQueue.Enqueue((customerId, newScore));
            return newScore; // Return the updated score
        }

        public IEnumerable<Customer> GetCustomersByRank(int start, int end)
        {
            var cacheKey = $"Leaderboard_{start}_{end}";
            if (_cache.TryGetValue(cacheKey, out List<Customer> cachedCustomers))
            {
                return cachedCustomers;
            }

            _lock.EnterReadLock();
            try
            {
                var nodes = _skipList.GetRange(start, end);
                var result = new List<Customer>();

                foreach (var node in nodes)
                {
                    result.Add(new Customer { CustomerId = node.CustomerId, Score = node.Score, Rank = node.Rank });
                }

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30)); // Set cache expiration time
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<Customer> GetCustomerNeighbors(long customerId, int high, int low)
        {
            _lock.EnterReadLock();
            try
            {
                var customerNode = _skipList.GetCustomer(customerId);
                var neighbors = new List<Customer>();

                // Get higher-ranked neighbors
                var current = customerNode;
                for (int i = 0; i < high && current != null; i++)
                {
                    current = current.Forward[0]; // Move to the next node
                    if (current != null)
                    {
                        neighbors.Add(new Customer { CustomerId = current.CustomerId, Score = current.Score, Rank = current.Rank });
                    }
                }

                // Add the current customer
                if (customerNode != null)
                {
                    neighbors.Add(new Customer { CustomerId = customerNode.CustomerId, Score = customerNode.Score, Rank = customerNode.Rank });
                }

                // Get lower-ranked neighbors
                current = customerNode;
                for (int i = 0; i < low && current != null; i++)
                {
                    current = current.Forward[0]; // Move to the next node
                    if (current != null)
                    {
                        neighbors.Add(new Customer { CustomerId = current.CustomerId, Score = current.Score, Rank = current.Rank });
                    }
                }

                return neighbors;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}