using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiParser
{
    public class BitReader : IEnumerable<bool>, IReadOnlyList<bool>
    {
        private static Dictionary<UInt64, BitReader> readers = new Dictionary<ulong, BitReader>();

        public static BitReader GetBitReader(UInt64 num, int bits)
        {
            BitReader r = null;

            if (readers.ContainsKey(num))
            {
                readers.TryGetValue(num, out r);
            }
            else
            {
                r = new BitReader()
                {
                    num = num
                };
            }

            r.bits = bits;
            return r;
        }

        private UInt64 num;
        private int bits;

        private BitReader() {}

        public int Bits => bits;

        public int Count => bits;

        public bool this[int index] => GetBit(index);

        public bool GetBit(int bit)
        {
            if (bit >= bits) throw new ArgumentException("'bit' is larger than the avaliable bits!");

            var v = (ulong)Math.Pow(2, bit) & num;

            return v > 0;
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return new BitEnumerator() { self = this };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class BitEnumerator : IEnumerator<bool>
        {
            internal BitReader self = null;
            int curi = -1;

            public bool Current => self.GetBit(curi);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                return ++curi < self.bits;
            }

            public void Reset()
            {
                curi = 0;
            }
        }
    }
}
