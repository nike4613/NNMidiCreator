using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiParser
{
    public class TrackEvent
    {
        public static TrackEvent[] LoadEvents(byte[] data)
        {
            List<TrackEvent> evts = new List<TrackEvent>();

            statuscache = 0;

            for (int i = 0; i < data.Length; i++)
            {
                //Console.WriteLine("Event PosStart: {0}", i.ToString("X"));

                byte[] fourb = new byte[4];
                Buffer.BlockCopy(data, i, fourb, 0, Math.Min(4,data.Length-i));

                uint deltatime = MidiValueReader.LoadVariableLengthQuantity(fourb, out uint fl);
                i += (int)fl;

                var evt = new TrackEvent()
                {
                    DeltaTime = deltatime
                };

                if (Enum.IsDefined(typeof(EventType),data[i++]))
                {
                    evt.Type = (EventType) Enum.ToObject(typeof(EventType), data[i - 1]);
                }
                else
                {
                    evt.Type = EventType.Midi;
                }

                switch (evt.Type)
                {
                    case EventType.Midi:
                        LoadMidiData(evt, data, ref i, data[i - 1]);
                        break;
                    case EventType.Meta:
                        LoadMetaData(evt, data, ref i);
                        break;
                    case EventType.SysexF0:
                    case EventType.SysexF7:
                        LoadSysexData(evt, data, ref i);
                        break;
                }

                evts.Add(evt);
            }

            return evts.ToArray();
        }

        private static byte statuscache = 0;
        private static void LoadMidiData(TrackEvent evt, byte[] data, ref int i, byte status)
        {
            if (status < 0x80)
            {
                if (statuscache == 0) throw new Exception("There is no running status!");
                data[--i] = status;
                status = statuscache;
            }

            statuscache = status;

            if (!Enum.IsDefined(typeof(MidiEventType), status & 0xF0))
            {
                Console.WriteLine("Midi Event type not recognised! {0}", status & 0xF0);
            }
            MidiEventType type = (MidiEventType)Enum.ToObject(typeof(MidiEventType), status & 0xF0);
            byte channel = (byte)(status & 0x0F);

            byte[] datab;

            if (typeof(MidiEventType).GetField(type.ToString()).IsDefined(typeof(OneByteAttribute), false))
                datab = new byte[1];
            else
                datab = new byte[2];

            Buffer.BlockCopy(data, i, datab, 0, datab.Length);
            i += datab.Length;

            evt.Length = (uint) datab.Length;
            evt.MidiEventValues = datab;
            evt.MidiType = type;
            evt.Channel = channel;

            if (type == MidiEventType.ControllerChange_ChannelModeEvent && datab[0] >= 0x78) // Its a channel mode event
            {
                evt.ChannelType = (MidiChannelEventType)Enum.ToObject(typeof(MidiChannelEventType), datab[0]);
                evt.MidiEventValues = new byte[] { datab[1] };
            }

            // Ensure correct interpretation of NoteOn V=0
            if (type == MidiEventType.NoteOn && datab[1] == 0)
            {
                type = MidiEventType.NoteOff;
                datab[1] = 0x40;
            }

            i--; // because at the end of the FOR loop it increments
        }
        private static void LoadSysexData(TrackEvent evt, byte[] data, ref int i)
        {
            statuscache = 0;
            byte[] fourb = new byte[4];
            Buffer.BlockCopy(data, i, fourb, 0, Math.Min(4, data.Length - i));
            uint length = MidiValueReader.LoadVariableLengthQuantity(fourb, out uint flen);
            i += (int)flen;

            byte[] ndata = new byte[length];
            Buffer.BlockCopy(data, i, ndata, 0, (int)length);
            i += ndata.Length;

            evt.Data = ndata;
            evt.Length = length;

            i--;
        }
        private static void LoadMetaData(TrackEvent evt, byte[] data, ref int i)
        {
            statuscache = 0;
            byte type = data[i++];

            byte[] fourb = new byte[4];
            Buffer.BlockCopy(data, i, fourb, 0, Math.Min(4, data.Length - i));
            uint length = MidiValueReader.LoadVariableLengthQuantity(fourb, out uint flen);
            i += (int) flen;

            byte[] ndata = new byte[length];
            Buffer.BlockCopy(data, i, ndata, 0, Math.Min((int)length,data.Length-i));

            i += (int) length;

            evt.MetaType = type;
            evt.Data = ndata;
            evt.Length = length;

            i--;
        }

        public enum EventType : byte
        {
            Midi = 0,
            SysexF0 = 0xF0,
            SysexF7 = 0xF7,
            Meta = 0xFF
        }

        private TrackEvent() { }

        public uint DeltaTime { get; private set; }

        public EventType Type { get; private set; }

        public byte MetaType { get; private set; }
        public uint Length { get; private set; }
        public byte[] Data { get; private set; }

        public enum MidiEventType
        {
            NoteOff = 0x80,
            NoteOn = 0x90,
            PolyphonicUpdate = 0xA0,
            ControllerChange_ChannelModeEvent = 0xB0,
            [OneByte] ProgramChange = 0xC0,
            [OneByte] ChannelKeyPressureUpdate = 0xD0,
            PitchBendUpdate = 0xE0
        }

        public MidiEventType MidiType { get; private set; }
        public byte Channel { get; private set; }
        public byte[] MidiEventValues { get; private set; }

        public enum MidiChannelEventType
        {
            AllSoundOff = 0x78,
            ResetControllers = 0x79,
            LocalControl = 0x7A,
            AllNotesOff = 0x7B,
            OmniModeOff = 0x7C,
            OmniModeOn = 0x7D,
            MonoModeOn = 0x7E,
            PolyModeOn = 0x7F
        }

        public MidiChannelEventType ChannelType { get; private set; }

    }

    internal class OneByteAttribute : Attribute {}
}