using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiParser
{
    public class MidiFile
    {
        private bool isready = false;
        private Stream stream;
        public Stream Stream
        {
            get
            {
                return stream;
            }
            set
            {
                if (!isready) stream = value;
                else throw new ArgumentException("Stream can only be set once!");

                isready = true;
            }
        }

        public MidiFile()
        {
        }

        public MidiFile(Stream s)
        {
            Stream = s;
        }

        private List<MidiValueReader.Chunk> tchunks = new List<MidiValueReader.Chunk>();
        private MidiValueReader.Chunk.HeaderChunkData header = null;

        public TimeDivision Division { get; private set; }
        public class TimeDivision
        {
            public readonly DivisionMeaning Meaning;
            public enum DivisionMeaning : byte
            {
                UnitsPerQuarterNote = 0,
                UnitsPerSmpteFrame = 1
            }

            public readonly ushort DeltaUnits;

            public readonly SmpteFps FramesPerSecond;
            public enum SmpteFps
            {
                Fps24 = -24,
                Fps25 = -25,
                Fps30Drop = -29,
                Fps30 = -30
            }

            internal TimeDivision(DivisionMeaning mean, ushort delta, SmpteFps fps)
            {
                Meaning = mean;
                DeltaUnits = delta;
                FramesPerSecond = fps;
            }
            internal TimeDivision(DivisionMeaning mean, ushort delta)
            {
                Meaning = mean;
                DeltaUnits = delta;
            }
        }

        public MidiFormat Format { get; private set; }
        public enum MidiFormat : UInt16
        {
            SingleTrack = 0,
            ParallelTracks = 1,
            SerialTracks = 2
        }

        public ushort TrackCount { get; private set; }

        private int TwosNegative(uint num, int bits)
        {
            --bits;
            int valuemask = (1 << bits) - 1;
            int value = Convert.ToInt32(num) & valuemask;
            if (BitReader.GetBitReader(num, bits+1)[bits]) value -= 1 << (bits - 1);

            return value;
        }

        public void Load()
        { if (!isready) throw new ArgumentException("Stream has not been set!");
            var midr = new MidiValueReader(stream);

            while (stream.Position < stream.Length)
            {
                var chnk = midr.ReadChunkNoDataProcessing();
                chnk = midr.ProcessChunkData(chnk);

                if (chnk.TypeE == MidiValueReader.Chunk.ChunkType.MThd) header = (MidiValueReader.Chunk.HeaderChunkData)chnk.Data;
                else tchunks.Add(chnk);
            }

            Format = (MidiFormat)Enum.ToObject(typeof(MidiFormat), header.Format);
            TrackCount = header.Tracks;

            var bitread = BitReader.GetBitReader(header.Division, 16);
            if (bitread[15])
            {
                var highmask = ((1 << 7) - 1) << 8; // should always be 0b111111100000000 or 0x7f00
                var lowmask = 0xff;

                Division = new TimeDivision(TimeDivision.DivisionMeaning.UnitsPerSmpteFrame,
                    Convert.ToUInt16(header.Division & lowmask), 
                    (TimeDivision.SmpteFps)Enum.ToObject(typeof(TimeDivision.SmpteFps), TwosNegative((header.Division & Convert.ToUInt32(highmask)) >> 8, 7)));
            }
            else
            {
                Division = new TimeDivision(TimeDivision.DivisionMeaning.UnitsPerQuarterNote,
                    header.Division);
            }
        }
    }
}
