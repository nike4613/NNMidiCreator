using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiParser.Events;

namespace MidiParser
{
    public static class MidiEventFrontend
    {
        public static AMidiTrackEvent[] GetTrackEvents(MidiValueReader.Chunk.TrackChunkData trackch)
        {
            AMidiTrackEvent[] evts = new AMidiTrackEvent[trackch.Events.Length];

            byte? channel = null;

            for (int i = 0; i < trackch.Events.Length; i++)
            {
                var evt = trackch.Events[i];

                AMidiTrackEvent oevt = null;

                switch (evt.Type)
                {
                    case TrackEvent.EventType.Midi:
                        channel = null;
                        var mety = MidiEvent.GetEventType(evt).GetEventClass();
                        var mcons = mety.GetConstructor(new Type[] { typeof(TrackEvent) });
                        oevt = (MidiEvent)mcons.Invoke(new object[] { evt });
                        break;
                    case TrackEvent.EventType.Meta:
                        var ety = MetaEvent.GetEventType(evt).GetEventClass();
                        var cons = ety.GetConstructor(new Type[] { typeof(TrackEvent) });
                        oevt = (MetaEvent)cons.Invoke(new object[] { evt });

                        if (((MetaEvent)oevt).Type == MetaEvent.MetaEventType.MidiChannelPrefix)
                            channel = ((MetaEvent)oevt).Channel;

                        ((MetaEvent)oevt).Channel = channel;
                        break;
                    case TrackEvent.EventType.SysexF0:
                    case TrackEvent.EventType.SysexF7:
                        oevt = new SysexEvent(evt);
                        ((SysexEvent)oevt).Channel = channel;
                        break;
                }

                evts[i] = oevt;
            }

            return evts;
        }
    }

    public class MidiTrack
    {
        public AMidiTrackEvent[] Events { get; private set; }

        public MidiTrack(MidiValueReader.Chunk.TrackChunkData track)
        {
            Events = MidiEventFrontend.GetTrackEvents(track);
        }

        public override string ToString()
        {
            return String.Format("MidiTrack {{{0}}}", Events.ToString<AMidiTrackEvent>());
        }
    }

}

namespace MidiParser.Events
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

        public MidiEvent(TrackEvent inte) : base(inte)
        {
            Channel = inte.Channel;
            Type = GetEventType(inte);
        }

        public override string ToString()
        {
            return String.Format("MidiEvent {0} on Channel {1} after {2} ticks", Type.ToString(), Channel, DeltaTime);
        }
    }
    #region Midi Events
    public class MidiNoteOn : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Velocity;

        public MidiNoteOn(TrackEvent inte) : base(inte)
        {
            Note = inte.MidiEventValues[0];
            Velocity = inte.MidiEventValues[1];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Note={0}, Velocity={1}", Note, Velocity);
        }
    }
    public class MidiNoteOff : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Velocity;

        public MidiNoteOff(TrackEvent inte) : base(inte)
        {
            Note = inte.MidiEventValues[0];
            Velocity = inte.MidiEventValues[1];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Note={0}, Velocity={1}", Note, Velocity);
        }
    }
    public class MidiPolyphonicUpdate : MidiEvent
    {
        public readonly byte Note;
        public readonly byte Pressure;

        public MidiPolyphonicUpdate(TrackEvent inte) : base(inte)
        {
            Note = inte.MidiEventValues[0];
            Pressure = inte.MidiEventValues[1];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Note={0}, Pressure={1}", Note, Pressure);
        }
    }
    public class MidiControllerChange : MidiEvent
    {
        public readonly byte Controller;
        public readonly byte Value;

        public MidiControllerChange(TrackEvent inte) : base(inte)
        {
            Controller = inte.MidiEventValues[0];
            Value = inte.MidiEventValues[1];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Controller={0}, Value={1}", Controller, Value);
        }
    }
    public class MidiProgramChange : MidiEvent
    {
        public readonly byte ProgramNumber;

        public MidiProgramChange(TrackEvent inte) : base(inte)
        {
            ProgramNumber = inte.MidiEventValues[0];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Program={0}", ProgramNumber);
        }
    }
    public class MidiChannelKeyPressureUpdate : MidiEvent
    {
        public readonly byte Pressure;

        public MidiChannelKeyPressureUpdate(TrackEvent inte) : base(inte)
        {
            Pressure = inte.MidiEventValues[0];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Pressure={0}", Pressure);
        }
    }
    public class MidiPitchBendUpdate : MidiEvent
    {
        public readonly ushort PitchBend;

        public MidiPitchBendUpdate(TrackEvent inte) : base(inte)
        {
            PitchBend = (ushort)(inte.MidiEventValues[0] | (inte.MidiEventValues[1] << 7));
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", New Bend={0}", PitchBend);
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
            Channels = inte.MidiEventValues[0];
        }
    }
    public class MidiLocalControl : MidiEvent
    {
        public readonly bool ConnectMode;

        public MidiLocalControl(TrackEvent inte) : base(inte)
        {
            if (inte.MidiEventValues[0] == 0x00) ConnectMode = false;
            if (inte.MidiEventValues[0] == 0x7F) ConnectMode = true;
        }
    }
    #endregion

    public class SysexEvent : AMidiTrackEvent
    {
        public readonly byte[] Message;
        public byte? Channel { get; internal set; }

        public SysexEvent(TrackEvent inte) : base(inte)
        {
            Channel = null;

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

        public override string ToString()
        {
            return String.Format("Sysex Event after {2} ticks, Message={0}" + ((Channel != null)?", Channel={1}":""), Message.ToString<byte>(), Channel, DeltaTime);
        }
    }

    public static class MetaEvent_MetaEventType
    {
        public static Type GetEventClass(this MetaEvent.MetaEventType etype)
        {
            Type type = typeof(MetaEvent);

            switch (etype)
            {
                case MetaEvent.MetaEventType.SequenceNumber:
                    type = typeof(MetaSequenceNumber);
                    break;
                case MetaEvent.MetaEventType.TextEvent:
                    type = typeof(MetaTextEvent);
                    break;
                case MetaEvent.MetaEventType.CoppyrightNotice:
                    type = typeof(MetaCoppyrightNotice);
                    break;
                case MetaEvent.MetaEventType.TrackSequenceName:
                    type = typeof(MetaTrackSequenceName);
                    break;
                case MetaEvent.MetaEventType.InsturmentName:
                    type = typeof(MetaInsturmentName);
                    break;
                case MetaEvent.MetaEventType.Lyric:
                    type = typeof(MetaLyric);
                    break;
                case MetaEvent.MetaEventType.Marker:
                    type = typeof(MetaMarker);
                    break;
                case MetaEvent.MetaEventType.CuePoint:
                    type = typeof(MetaCuePoint);
                    break;
                case MetaEvent.MetaEventType.MidiChannelPrefix:
                    type = typeof(MetaMidiChannelPrefix);
                    break;
                case MetaEvent.MetaEventType.EndOfTrack:
                    type = typeof(MetaEndOfTrack);
                    break;
                case MetaEvent.MetaEventType.SetTempo:
                    type = typeof(MetaSetTempo);
                    break;
                case MetaEvent.MetaEventType.SmpteOffset:
                    type = typeof(MetaSmpteOffset);
                    break;
                case MetaEvent.MetaEventType.TimeSignature:
                    type = typeof(MetaTimeSignature);
                    break;
                case MetaEvent.MetaEventType.KeySignature:
                    type = typeof(MetaKeySignature);
                    break;
                case MetaEvent.MetaEventType.SequencerSpecific:
                    type = typeof(MetaSequencerSpecific);
                    break;

            }

            return type;
        }
    }
    public class MetaEvent : AMidiTrackEvent
    {
        public enum MetaEventType : byte
        {
            SequenceNumber = 0x00,
            TextEvent = 0x01,
            CoppyrightNotice = 0x02,
            TrackSequenceName = 0x03,
            InsturmentName = 0x04,
            Lyric = 0x05,
            Marker = 0x06,
            CuePoint = 0x07,
            MidiChannelPrefix = 0x20,
            EndOfTrack = 0x2F,
            SetTempo = 0x51,
            SmpteOffset = 0x54,
            TimeSignature = 0x58,
            KeySignature = 0x59,
            SequencerSpecific = 0x7F
        }

        public readonly MetaEventType Type;
        public byte? Channel { get; internal set; }

        internal static MetaEventType GetEventType(TrackEvent inte)
        {
            return (MetaEventType)Enum.ToObject(typeof(MetaEventType), inte.MetaType);
        }

        public MetaEvent(TrackEvent inte) : base(inte)
        {
            Channel = null;
            Type = GetEventType(inte);
        }

        public override string ToString()
        {
            return String.Format("MetaEvent {0}" + ((Channel != null)?" on Channel {1}":"") + " after {2} ticks", Type.ToString(), Channel, DeltaTime);
        }
    }

    #region Meta Events
    public class MetaSequenceNumber : MetaEvent
    {
        public readonly ushort SequenceNumber;

        public MetaSequenceNumber(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 2) throw new ArgumentException("Not enough data for a Sequence Number event!");

            byte[] twob = new byte[2];
            Buffer.BlockCopy(inte.Data, 0, twob, 0, 2);
            SequenceNumber = (ushort)MidiValueReader.LoadBinaryValue(twob);
        }

        public override string ToString()
        {
            return base.ToString() + ", SequenceNumber=" + SequenceNumber;
        }
    }
    // Impl only supports ASCII characters
    public class MetaTextEvent : MetaEvent
    {
        public readonly string Text;

        public MetaTextEvent(TrackEvent inte) : base(inte)
        {
            Text = Encoding.ASCII.GetString(inte.Data);
        }

        public override string ToString()
        {
            return base.ToString() + ", Text=\"" + Text + "\"";
        }
    }
    public class MetaCoppyrightNotice : MetaTextEvent
    {
        public MetaCoppyrightNotice(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaTrackSequenceName : MetaTextEvent
    {
        public MetaTrackSequenceName(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaInsturmentName : MetaTextEvent
    {
        public MetaInsturmentName(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaLyric : MetaTextEvent
    {
        public MetaLyric(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaMarker : MetaTextEvent
    {
        public MetaMarker(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaCuePoint : MetaTextEvent
    {
        public MetaCuePoint(TrackEvent inte) : base(inte)
        { }
    }

    public class MetaMidiChannelPrefix : MetaEvent
    {
        public MetaMidiChannelPrefix(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 1) throw new ArgumentException("Not enough data for a Channel Prefix event!");

            Channel = inte.Data[0];
        }
    }
    public class MetaEndOfTrack : MetaEvent
    {
        public MetaEndOfTrack(TrackEvent inte) : base(inte)
        { }
    }
    public class MetaSetTempo : MetaEvent
    {
        public readonly uint Tempo; // microseconds per quarter note

        public MetaSetTempo(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 3) throw new ArgumentException("Not enough data for a Tempo event!");

            byte[] threeb = new byte[3];
            Buffer.BlockCopy(inte.Data, 0, threeb, 0, 3);
            Tempo = MidiValueReader.LoadBinaryValue(threeb);
        }

        public override string ToString()
        {
            return base.ToString() + ", Tempo=" + Tempo + "";
        }
    }
    public class MetaSmpteOffset : MetaEvent
    {
        public readonly byte Hour;
        public readonly byte Minute;
        public readonly byte Second;
        public readonly byte Frame;
        public readonly byte FractionalFrame; // /100

        public MetaSmpteOffset(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 5) throw new ArgumentException("Not enough data for an SMPTE Offset event!");

            Hour = inte.Data[0];
            Minute = inte.Data[1];
            Second = inte.Data[2];
            Frame = inte.Data[3];
            FractionalFrame = inte.Data[4];
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Time={0}h{1}m{2}s {3}+{4}/100f", Hour, Minute, Second, Frame, FractionalFrame);
        }
    }
    public class MetaTimeSignature : MetaEvent
    {
        public readonly byte Numerator;
        public readonly byte Denominator; // negative power of two (i.e. if denom = 3 then frac = num*(2^(-denom)))
        public readonly byte ClocksPerClick;
        public readonly byte NotesPer24Clocks;

        public MetaTimeSignature(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 4) throw new ArgumentException("Not enough data for a Time Signature event!");

            Numerator = inte.Data[0];
            Denominator = inte.Data[1];
            ClocksPerClick = inte.Data[2];
            NotesPer24Clocks = inte.Data[3];
        }

        public override string ToString()
        {
            return base.ToString() + 
                String.Format(", TimeSig={0}/{1}, Midi Clocks per Met click={2}, 32nd notes per 24 clocks={3}", Numerator, Math.Pow(2,Denominator), ClocksPerClick, NotesPer24Clocks);
        }
    }
    public class MetaKeySignature : MetaEvent
    {
        public readonly bool IsMajorKey;
        public readonly KeySignature Key;  

        public enum KeySignature : SByte
        {
            AFlat = -4, A = 3, ASharp = -2,
            BFlat = -2, B = 5, BSharp = 0,
            CFlat = -7, C = 0, CSharp = 7,
            DFlat = -5, D = 2, DSharp = -3,
            EFlat = -3, E = 4, ESharp = -1,
            FFlat = 4, F = -1, FSharp = 6,
            GFlat = -6, G = 1, GSharp = -4
        }

        public MetaKeySignature(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 2) throw new ArgumentException("Not enough data for a Key Signature event!");

            Key = (KeySignature)Enum.ToObject(typeof(KeySignature), inte.Data[0]);
            IsMajorKey = inte.Data[1] == 0;
        }
        
        public override string ToString()
        {
            return base.ToString() + String.Format(", Key={0} {1}", Key.ToString(), (IsMajorKey)?"major":"minor");
        }

    }
    public static class MetaKeySignature_KeySignature
    {
        public static MetaKeySignature.KeySignature FromMinor(this MetaKeySignature.KeySignature self)
        {
            switch (self)
            {
                case MetaKeySignature.KeySignature.AFlat: return MetaKeySignature.KeySignature.B; // -4
                case MetaKeySignature.KeySignature.A: return MetaKeySignature.KeySignature.C; // 3
                //case MetaKeySignature.KeySignature.ASharp: return MetaKeySignature.KeySignature.DFlat;
                case MetaKeySignature.KeySignature.BFlat: return MetaKeySignature.KeySignature.CSharp; // -2
                case MetaKeySignature.KeySignature.B: return MetaKeySignature.KeySignature.D; // 5
                //case MetaKeySignature.KeySignature.BSharp: return MetaKeySignature.KeySignature.DSharp;
                case MetaKeySignature.KeySignature.CFlat: return MetaKeySignature.KeySignature.D; // -7
                case MetaKeySignature.KeySignature.C: return MetaKeySignature.KeySignature.EFlat; // 0
                case MetaKeySignature.KeySignature.CSharp: return MetaKeySignature.KeySignature.E; // 7
                case MetaKeySignature.KeySignature.DFlat: return MetaKeySignature.KeySignature.FFlat; // -5
                case MetaKeySignature.KeySignature.D: return MetaKeySignature.KeySignature.F; // 2
                //case MetaKeySignature.KeySignature.DSharp: return MetaKeySignature.KeySignature.GFlat;
                case MetaKeySignature.KeySignature.EFlat: return MetaKeySignature.KeySignature.FSharp; // -3
                case MetaKeySignature.KeySignature.E: return MetaKeySignature.KeySignature.G; // 4
                //case MetaKeySignature.KeySignature.ESharp: return MetaKeySignature.KeySignature.GSharp;
                //case MetaKeySignature.KeySignature.FFlat: return MetaKeySignature.KeySignature.G;
                case MetaKeySignature.KeySignature.F: return MetaKeySignature.KeySignature.AFlat; //-1
                case MetaKeySignature.KeySignature.FSharp: return MetaKeySignature.KeySignature.A; //6
                case MetaKeySignature.KeySignature.GFlat: return MetaKeySignature.KeySignature.A;//-6
                case MetaKeySignature.KeySignature.G: return MetaKeySignature.KeySignature.BFlat;//1
                //case MetaKeySignature.KeySignature.GSharp: return MetaKeySignature.KeySignature.CFlat;
            }

            return 0;
        }
    }
    public class MetaSequencerSpecific : MetaEvent
    {
        public readonly uint ManufacturerId;
        public readonly byte[] Data;

        public MetaSequencerSpecific(TrackEvent inte) : base(inte)
        {
            if (inte.Length < 1) throw new ArgumentException("Not enough data for a Sequencer Specific event!");

            int loc = 0;

            uint id = inte.Data[0]; loc++;
            if (id == 0)
            {
                byte[] two = new byte[2];
                Buffer.BlockCopy(inte.Data, 1, two, 0, 2); loc += 2;
                id = MidiValueReader.LoadBinaryValue(two);
            }

            byte[] data = new byte[inte.Length - loc];
            Buffer.BlockCopy(inte.Data, loc, data, 0, data.Length);

            ManufacturerId = id;
            Data = data;
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", Manufacturer Id={0}, Data={1}", ManufacturerId.ToString("X").PadLeft(8).Replace(' ', '0'), Data.ToString<byte>());
        }
    }
    #endregion
}
