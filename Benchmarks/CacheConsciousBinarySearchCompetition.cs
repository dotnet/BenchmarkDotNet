using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class CacheConsciousBinarySearchCompetition
    {
        private const int K = 24, N = (1 << K) - 1, IterationCount = 10000000;
        private readonly Random random = new Random();

        private Tree originalTree;
        private int[] bfs;

        public CacheConsciousBinarySearchCompetition()
        {
            originalTree = new Tree(Enumerable.Range(0, N).Select(x => 2 * x).ToArray());
            bfs = originalTree.Bfs();
        }

        [BenchmarkMethod]
        public void SimpleSearch()
        {
            SingleRun(originalTree);
        }

        [BenchmarkMethod]
        public void CacheConsciousSearch1()
        {
            SingleRun(new CacheConsciousTree(bfs, 1));
        }

        [BenchmarkMethod]
        public void CacheConsciousSearch2()
        {
            SingleRun(new CacheConsciousTree(bfs, 2));
        }

        [BenchmarkMethod]
        public void CacheConsciousSearch3()
        {
            SingleRun(new CacheConsciousTree(bfs, 3));
        }

        [BenchmarkMethod]
        public void CacheConsciousSearch4()
        {
            SingleRun(new CacheConsciousTree(bfs, 4));
        }

        [BenchmarkMethod]
        public void CacheConsciousSearch5()
        {
            SingleRun(new CacheConsciousTree(bfs, 5));
        }
        
        private int SingleRun(ITree tree)
        {
            int searchedCount = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                int x = random.Next(N * 2);
                if (tree.Contains(x))
                    searchedCount++;
            }
            return searchedCount;
        }

        interface ITree
        {
            bool Contains(int x);
        }

        class Tree : ITree
        {
            private readonly int[] a;

            public Tree(int[] a)
            {
                this.a = a;
            }

            public bool Contains(int x)
            {
                int l = 0, r = N - 1;
                while (l <= r)
                {
                    int m = (l + r) / 2;
                    if (a[m] == x)
                        return true;
                    if (a[m] > x)
                        r = m - 1;
                    else
                        l = m + 1;
                }
                return false;
            }

            public int[] Bfs()
            {
                int[] bfs = new int[N], l = new int[N], r = new int[N];
                int tail = 0, head = 0;
                l[head] = 0;
                r[head++] = N - 1;
                while (tail < head)
                {
                    int m = (l[tail] + r[tail]) / 2;
                    bfs[tail] = a[m];
                    if (l[tail] < m)
                    {
                        l[head] = l[tail];
                        r[head++] = m - 1;
                    }
                    if (m < r[tail])
                    {
                        l[head] = m + 1;
                        r[head++] = r[tail];
                    }
                    tail++;
                }
                return bfs;
            }
        }

        class CacheConsciousTree : ITree
        {
            private readonly int[] a;
            private readonly int level;

            public CacheConsciousTree(int[] bfs, int level)
            {
                this.level = level;
                int size = (1 << level) - 1, counter = 0;
                a = new int[N];
                var was = new bool[N];
                var queue = new int[size];
                for (int i = 0; i < N; i++)
                    if (!was[i])
                    {
                        int head = 0;
                        queue[head++] = i;
                        for (int tail = 0; tail < head; tail++)
                        {
                            a[counter++] = bfs[queue[tail]];
                            was[queue[tail]] = true;
                            if (queue[tail] * 2 + 1 < N && head < size)
                                queue[head++] = queue[tail] * 2 + 1;
                            if (queue[tail] * 2 + 2 < N && head < size)
                                queue[head++] = queue[tail] * 2 + 2;
                        }
                    }
            }

            public bool Contains(int x)
            {
                int u = 0, deep = 0, leafCount = 1 << (level - 1);
                int root = 0, rootOffset = 0;
                while (deep < K)
                {
                    int value = a[root + u];
                    if (value == x)
                        return true;
                    if (++deep % level != 0)
                    {
                        if (value > x)
                            u = 2 * u + 1;
                        else
                            u = 2 * u + 2;
                    }
                    else
                    {
                        int subTreeSize = (1 << Math.Min(level, K - deep)) - 1;
                        if (value > x)
                            rootOffset = rootOffset * leafCount * 2 + (u - leafCount + 1) * 2;
                        else
                            rootOffset = rootOffset * leafCount * 2 + (u - leafCount + 1) * 2 + 1;
                        root = (1 << deep) - 1 + rootOffset * subTreeSize;
                        u = 0;
                    }
                }
                return false;
            }
        }
    }
}