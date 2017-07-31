using System;
using System.IO;
using System.Linq;
using static MidiParser.MidiValueReader.Chunk;
using static MidiParser.MidiValueReader;

namespace MidiParser
{
    public class MidiValueReader
    {
        private BinaryReader bstream;
        private Stream stream;

        public MidiValueReader(Stream ins) 
        {
            bstream = new BinaryReader(ins);
            stream = ins;
        }

        #region ReadValue
        public enum ValueType
        {
            Binary,
            VLQ,
            ChooseBest // ATM just binds to Binary
        }
        private struct Vprocret
        {
            public UInt32 value;
            public UInt32 inclen;
        }


        private static valueProc BinaryProc = (byte[] value) =>
        {
            if (BitConverter.IsLittleEndian)
                value = value.Reverse().ToArray(); // If we are an an LE system, flip the byte order (MIDI is BE)

            uint vl = 0;
            switch(value.Length)
            {
                case 1:
                    vl = value[0];
                    break;
                case 2:
                    vl = BitConverter.ToUInt16(value, 0);
                    break;
                case 4:
                    vl = BitConverter.ToUInt32(value, 0);
                    break;
            }

            Vprocret vp = new Vprocret()
            {
                value = vl,
                inclen = (uint)value.Length
            };

            return vp;
        };
        private static valueProc VLQProc = (byte[] value) =>
        {
            uint outv = 0;

            byte[] nval = new byte[value.Length];

            int i = 0;
            for (bool run = true; i < value.Length && run; i++)
            {
                var br = BitReader.GetBitReader(value[i], 8);
                if (br[7]) run = false;
                byte bval = (byte)(value[i] & 0x7F);
                nval[i] = bval;
            }

            if (BitConverter.IsLittleEndian)
                nval = nval.Reverse().ToArray(); // If we are an an LE system, flip the byte order (MIDI is BE)

            for (int j = 0; j < i; j++)
            {
                uint bval = (uint)value[i] & 0x7F;
                bval = bval >> (7 * j);
                outv += bval;
            }

            return new Vprocret()
            {
                value = outv,
                inclen = (uint)i
            };
        };

        public static uint LoadBinaryValue(byte[] bytes)
        {
            return BinaryProc(bytes).value;
        }
        public static uint LoadVariableLengthQuantity(byte[] bytes, out uint flen)
        {
            var v = VLQProc(bytes);
            flen = v.inclen;
            return v.value;
        }

        private delegate Vprocret valueProc(byte[] val);
        public UInt32 ReadValue(ValueType type=ValueType.Binary, int bits=32)
        {
            int bytec = bits / 8;

            UInt32 outval = 0;

            byte[] bytes = new byte[bytec];

            stream.Read(bytes, 0, bytec);

            Vprocret rvp;
            switch (type)
            {
                case ValueType.ChooseBest:
                case ValueType.Binary:
                    rvp = BinaryProc(bytes);
                    outval = rvp.value;
                    stream.Seek(bytec-rvp.inclen, SeekOrigin.Current);
                    break;
                case ValueType.VLQ:
                    rvp = VLQProc(bytes);
                    outval = rvp.value;
                    stream.Seek(bytec - rvp.inclen, SeekOrigin.Current);
                    break;
            }

            return outval;
        }
        #endregion

        #region ReadChunk

        public class Chunk
        {
            public string     Type   { get; protected internal set; }
            public ChunkType  TypeE  { get; protected internal set; }
            public uint       Length { get; protected internal set; }
            public IChunkData Data   { get; protected internal set; }

            public enum ChunkType
            {
                MThd,
                MTrk,
                Unknown
            }

            public interface IChunkData { }
            public class UnknownChunkData : IChunkData
            {
                public byte[] Data { get; protected internal set; }
            }
            public class HeaderChunkData : IChunkData
            {
                public ushort Format { get; protected internal set; }
                public ushort Tracks { get; protected internal set; }
                public ushort Division { get; protected internal set; }
            }
            public class TrackChunkData : IChunkData
            {
                public TrackEvent[] Events { get; protected internal set; }
            }
        }

        public Chunk ReadChunkNoDataProcessing()
        {
            string type = new string(bstream.ReadChars(4));
            uint len = ReadValue(ValueType.Binary, 32);
            byte[] data = bstream.ReadBytes((int)len);

            Chunk chunk = new Chunk()
            {
                Type = type,
                TypeE = Chunk.ChunkType.Unknown,
                Length = len,
                Data = new Chunk.UnknownChunkData()
                {
                    Data = data
                }

            };

            return chunk;
        }

        public Chunk ProcessChunkData(Chunk chk)
        {
            ChunkType type = ChunkType.Unknown;

            Enum.TryParse(chk.Type, out type);

            byte[] data = ((UnknownChunkData)chk.Data).Data;

            IChunkData chdataf = new UnknownChunkData() { Data = data };

            if (type == ChunkType.MThd)
            {
                var hdd = new HeaderChunkData();
                chdataf = hdd;

                byte[] ushort16 = new byte[2];

                Buffer.BlockCopy(data, 0, ushort16, 0, 2); // data offset 0
                var format = BinaryProc(ushort16).value;

                Buffer.BlockCopy(data, 2, ushort16, 0, 2); // data offset 2
                var tracks = BinaryProc(ushort16).value;

                Buffer.BlockCopy(data, 4, ushort16, 0, 2); // data offset 4
                var division = BinaryProc(ushort16).value;

                if (!( format != 0 || tracks == 1)) throw new FileLoadException("MIDI Header chunk is defined as format 0 with more than one track!");

                hdd.Format = (ushort)format;
                hdd.Tracks = (ushort)tracks;
                hdd.Division = (ushort)division;
            }

            if (type == ChunkType.MTrk)
            {
                var tkd = new TrackChunkData();
                chdataf = tkd;

                tkd.Events = TrackEvent.LoadEvents(data);
            }

            return new Chunk()
            {
                Type = chk.Type,
                TypeE = type,
                Length = chk.Length,
                Data = chdataf
            };
        }

        #endregion
    }

    public static class MidiValueReader_Chunk_ChunkTypeExtensions
    {
        public static Type GetChunkType(this ChunkType ct)
        {
            switch (ct)
            {
                case ChunkType.MThd:
                    return typeof(HeaderChunkData);
                case ChunkType.MTrk:
                    return typeof(TrackChunkData);
                case ChunkType.Unknown:
                    return typeof(UnknownChunkData);
            }
            return null;
        }
    }
}
