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

            for (int i = 0; i < data.Length; i++)
            {
                byte[] fourb = new byte[4];
                Buffer.BlockCopy(data, i, fourb, 0, 4);

                uint deltatime = MidiValueReader.LoadVariableLengthQuantity(fourb, out uint fl);
                i += (int)fl;

                var evt = new TrackEvent();
                evts.Add(evt);

                evt.DeltaTime = deltatime;


            }

            return new TrackEvent[0];
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
            ProgramChange = 0xC0,
            ChannelKeyPressureUpdate = 0xD0,
            PitchBendUpdate = 0xE0
        }

        public MidiEventType MidiType { get; private set; }
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
}