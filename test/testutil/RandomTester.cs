using System;
using Serilog;

namespace TestUtil
{
    public class RandomTester
    {
        public static readonly ILogger log = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
        protected readonly int seed;
        protected readonly Random random;

        public RandomTester(int seed)
        {
            this.seed = seed;
            this.random = new Random(seed);
            log.Information("Using Random Seed: {seed}", this.seed);
        }

        public Random r()
        {
            return random;
        }

        public int intBetween(int startInclusive, int endInclusive)
        {
            if (endInclusive < startInclusive)
            {
                throw new ArgumentException("end cannot be less than start");
            }
            return random.Next(startInclusive, endInclusive + 1);
        }

        public void bytes(byte[] bytes)
        {
            random.NextBytes(bytes);
        }

        public Boolean boolean()
        {
            return random.Next(2) == 1;
        }

    }
}