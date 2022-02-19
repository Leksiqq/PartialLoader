using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigCatsDataClient
{
    public class Chunk
    {
        public int Id { get; internal set; }
        public int Count { get; internal set; }
        public TimeSpan Elapsed { get; internal set; }
        public int CountAll { get; internal set; }
        public TimeSpan ElapsedAll { get; internal set; }
    }
}
