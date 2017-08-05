using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiParser
{
    public class MidiTrackEvent<T> where T : AMidiTrackEvent
    {
        public T Event { get; }
        public Type EventType => typeof(T);
    }

    public abstract class AMidiTrackEvent
    {
        public uint DeltaTime { get; protected set; }
        protected TrackEvent Internal;

        internal AMidiTrackEvent(TrackEvent inte)
        {
            Internal = inte;
            DeltaTime = inte.DeltaTime;
        }
    }

    public static class MidiEvent_MidiEventType
    {
        public static Type GetEventClass(this MidiEvent.MidiEventType etype)
        {
            Type type = typeof(MidiEvent);

            switch (etype)
            {
                case MidiEvent.MidiEventType.NoteOff:
                    type = typeof(MidiNoteOff);
                    break;
                case MidiEvent.MidiEventType.NoteOn:
                    type = typeof(MidiNoteOn);
                    break;
                case MidiEvent.MidiEventType.PolyphonicUpdate:
                    type = typeof(MidiPolyphonicUpdate);
                    break;
                case MidiEvent.MidiEventType.ControllerChange:
                    type = typeof(MidiControllerChange);
                    break;
                case MidiEvent.MidiEventType.ProgramChange:
                    type = typeof(MidiProgramChange);
                    break;
                case MidiEvent.MidiEventType.ChannelKeyPressureUpdate:
                    type = typeof(MidiChannelKeyPressureUpdate);
                    break;
                case MidiEvent.MidiEventType.PitchBendUpdate:
                    type = typeof(MidiPitchBendUpdate);
                    break;
                case MidiEvent.MidiEventType.AllSoundOff:
                    type = typeof(MidiAllSoundOff);
                    break;
                case MidiEvent.MidiEventType.ResetControllers:
                    type = typeof(MidiResetControllers);
                    break;
                case MidiEvent.MidiEventType.LocalControl:
                    type = typeof(MidiLocalControl);
                    break;
                case MidiEvent.MidiEventType.AllNotesOff:
                    type = typeof(MidiAllNotesOff);
                    break;
                case MidiEvent.MidiEventType.OmniModeOff:
                    type = typeof(MidiOmniModeOff);
                    break;
                case MidiEvent.MidiEventType.OmniModeOn:
                    type = typeof(MidiOmniModeOn);
                    break;
                case MidiEvent.MidiEventType.MonoModeOn:
                    type = typeof(MidiMonoModeOn);
                    break;
                case MidiEvent.MidiEventType.PolyModeOn:
                    type = typeof(MidiPolyModeOn);
                    break;
            }

            return type;
        }
    }
    public class MidiEvent : AMidiTrackEvent
    {
        public enum MidiEventType : byte
        {
            NoteOff = 0x80,
            NoteOn = 0x90,
            PolyphonicUpdate = 0xA0,
            ControllerChange = 0xB0,
            ProgramChange = 0xC0,
            ChannelKeyPressureUpdate = 0xD0,
            PitchBendUpdate = 0xE0,

            AllSoundOff = 0x78,
            ResetControllers = 0x79,
            LocalControl = 0x7A,
            AllNotesOff = 0x7B,
            OmniModeOff = 0x7C,
            OmniModeOn = 0x7D,
            MonoModeOn = 0x7E,
            PolyModeOn = 0x7F
        }

        public readonly MidiEventType Type;
        public readonly byte Channel;

        internal static MidiEventType GetEventType(TrackEvent inte)
        {
            if (inte.ChannelType == 0)
                return (MidiEventType)Enum.ToObject(typeof(MidiEventType), Convert.ChangeType(inte.MidiType, Enum.GetUnderlyingType(typeof(TrackEvent.MidiEventType))));
            else
                return (MidiEventType)Enum.ToObject(typeof(MidiEventType), Convert.ChangeType(inte.ChannelType, Enum.GetUnderlyingType(typeof(TrackEvent.MidiChannelEventType))));
        }

        internal MidiEvent(TrackEvent inte) : base(inte)
        {
            Channel = inte.Channel;
            Type = GetEventType(inte);
        }
    }
    #region Midi Events
    public class MidiNoteOn : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Velocity;

        public MidiNoteOn(TrackEvent inte) : base(inte)
        {
            Note = inte.Data[0];
            Velocity = inte.Data[1];
        }
    }
    public class MidiNoteOff : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Velocity;

        public MidiNoteOff(TrackEvent inte) : base(inte)
        {
            Note = inte.Data[0];
            Velocity = inte.Data[1];
        }
    }
    public class MidiPolyphonicUpdate : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Pressure;

        public MidiPolyphonicUpdate(TrackEvent inte) : base(inte)
        {
            Note = inte.Data[0];
            Pressure = inte.Data[1];
        }
    }
    public class MidiControllerChange : MidiEvent
    {
        public readonly byte Controller;
        public readonly byte Value;

        public MidiControllerChange(TrackEvent inte) : base(inte)
        {
            Controller = inte.Data[0];
            Value = inte.Data[1];
        }
    }
    public class MidiProgramChange : MidiEvent
    {
        public readonly byte ProgramNumber;

        public MidiProgramChange(TrackEvent inte) : base(inte)
        {
            ProgramNumber = inte.Data[0];
        }
    }
    public class MidiChannelKeyPressureUpdate : MidiEvent
    {
        public readonly byte Pressure;

        public MidiChannelKeyPressureUpdate(TrackEvent inte) : base(inte)
        {
            Pressure = inte.Data[0];
        }
    }
    public class MidiPitchBendUpdate : MidiEvent
    {
        public readonly ushort PitchBend;

        public MidiPitchBendUpdate(TrackEvent inte) : base(inte)
        {
            PitchBend = (ushort)(inte.Data[0] | (inte.Data[1] << 7));
        }
    }

    public class MidiAllSoundOff : MidiEvent
    {
        public MidiAllSoundOff(TrackEvent inte) : base(inte) { }
    }
    public class MidiResetControllers : MidiEvent
    {
        public MidiResetControllers(TrackEvent inte) : base(inte) { }
    }
    public class MidiAllNotesOff : MidiEvent
    {
        public MidiAllNotesOff(TrackEvent inte) : base(inte) { }
    }
    public class MidiOmniModeOff : MidiEvent
    {
        public MidiOmniModeOff(TrackEvent inte) : base(inte) { }
    }
    public class MidiOmniModeOn : MidiEvent
    {
        public MidiOmniModeOn(TrackEvent inte) : base(inte) { }
    }
    public class MidiPolyModeOn : MidiEvent
    {
        public MidiPolyModeOn(TrackEvent inte) : base(inte) { }
    }
    public class MidiMonoModeOn : MidiEvent
    {
        public readonly byte Channels;

        public MidiMonoModeOn(TrackEvent inte) : base(inte) 
        {
            Channels = inte.Data[0];
        }
    }
    public class MidiLocalControl : MidiEvent
    {
        public readonly bool ConnectMode;

        public MidiLocalControl(TrackEvent inte) : base(inte)
        {
            if (inte.Data[0] == 0x00) ConnectMode = false;
            if (inte.Data[0] == 0x7F) ConnectMode = true;
        }
    }
    #endregion

    public class SysexEvent : AMidiTrackEvent
    {
        public readonly byte[] Message;

        internal SysexEvent(TrackEvent inte) : base(inte)
        {
            switch (inte.Type)
            {
                case TrackEvent.EventType.SysexF0:
                    byte[] b = new byte[inte.Data.Length + 1];
                    b[0] = 0xF0;
                    Buffer.BlockCopy(inte.Data, 0, b, 1, inte.Data.Length);
                    Message = b;
                    break;
                case TrackEvent.EventType.SysexF7:
                    Message = inte.Data;
                    break;
            }
        }
    }

    public class MetaEvent : AMidiTrackEvent
    {
        internal MetaEvent(TrackEvent inte) : base(inte)
        {
        }
    }
}
