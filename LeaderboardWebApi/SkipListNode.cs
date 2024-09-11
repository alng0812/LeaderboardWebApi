namespace LeaderboardWebApi
{
    public class SkipListNode
    {
        public long CustomerId { get; set; }
        public decimal Score { get; set; }
        public int Rank { get; set; } // Add Rank property
        public SkipListNode[] Forward { get; set; }
        public SkipListNode(long customerId, decimal score, int level)
        {
            CustomerId = customerId;
            Score = score;
            Rank = 0; // Initialize Rank
            Forward = new SkipListNode[level + 1];
        }
    }

    public class SkipList
    {
        private readonly int _maxLevel;
        private readonly double _p;
        private SkipListNode _header;
        private int _level;
        private static readonly Random _random = new Random();
        public SkipList(int maxLevel, double p)
        {
            _maxLevel = maxLevel;
            _p = p;
            _header = new SkipListNode(0, 0, _maxLevel);
            _level = 0;
        }

        public void Insert(long customerId, decimal score)
        {
            var update = new SkipListNode[_maxLevel + 1];
            var current = _header;

            for (int i = _level; i >= 0; i--)
            {
                while (current.Forward[i] != null &&
                       (current.Forward[i].Score > score ||
                       (current.Forward[i].Score == score && current.Forward[i].CustomerId < customerId)))
                {
                    current = current.Forward[i];
                }
                update[i] = current;
            }

            current = current.Forward[0];
            Console.WriteLine($"Current node before insertion: {current?.CustomerId}, Score: {current?.Score}");

            if (current != null && current.CustomerId == customerId)
            {
                // Update score
                Console.WriteLine($"Updating existing customer: {customerId} with new score: {score}");
                current.Score = score;
            }
            else
            {
                Console.WriteLine($"Inserting new customer: {customerId} with score: {score}");
                int newLevel = RandomLevel();
                if (newLevel > _level)
                {
                    for (int i = _level + 1; i <= newLevel; i++)
                    {
                        update[i] = _header;
                    }
                    _level = newLevel;
                }

                var newNode = new SkipListNode(customerId, score, newLevel);
                for (int i = 0; i <= newLevel; i++)
                {
                    newNode.Forward[i] = update[i].Forward[i];
                    update[i].Forward[i] = newNode;
                }

                Console.WriteLine($"New node inserted: {newNode.CustomerId}, Score: {newNode.Score}");
            }

            // Update ranking logic
            UpdateRankings();

            // Print current node after insertion
            current = _header.Forward[0];
            Console.WriteLine($"Current node after insertion: {current?.CustomerId}, Score: {current?.Score}");
        }

        private void UpdateRankings()
        {
            // Recalculate rankings for all customers
            var current = _header.Forward[0];
            int rank = 1;

            while (current != null)
            {
                current.Rank = rank; // Update the rank
                current = current.Forward[0];
                rank++;
            }
        }

        public List<SkipListNode> GetRange(int start, int end)
        {
            var result = new List<SkipListNode>();
            var current = _header.Forward[0];
            int rank = 1;

            while (current != null && rank <= end)
            {
                if (rank >= start)
                {
                    result.Add(current);
                }
                current = current.Forward[0];
                rank++;
            }

            return result;
        }

        public SkipListNode GetCustomer(long customerId)
        {
            var current = _header;

            for (int i = _level; i >= 0; i--)
            {
                while (current.Forward[i] != null && current.Forward[i].CustomerId < customerId)
                {
                    current = current.Forward[i];
                }
            }

            current = current.Forward[0];
            return current != null && current.CustomerId == customerId ? current : null;
        }

        private int RandomLevel()
        {
            int level = 0;
            while (_random.NextDouble() < _p && level < _maxLevel)
            {
                level++;
            }
            // Ensure the returned level does not exceed _maxLevel
            var result = Math.Min(level, _maxLevel);
            Console.WriteLine($"Generated random level: {result}");
            return result;
        }
    }
}