using OpenTibiaUnity.Core.Metaflags;
using System;
using System.Collections.Generic;

namespace OpenTibiaUnity.Core.Sprites
{
    public enum FrameGroupType : byte
    {
        Idle = 0,
        Moving = 1,
        Default = Idle,
    }

    public class FrameGroupDuration
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }


    public sealed class FrameGroupAnimator
    {
        public byte AnimationPhases { get; private set; } = 0;
        public bool Async { get; private set; } = false;
        public int LoopCount { get; private set; } = 0;
        public sbyte StartPhase { get; private set; } = -1;
        public List<FrameGroupDuration> FrameGroupDurations { get; private set; } = new List<FrameGroupDuration>();
        
        public static void SerializeLegacy(ThingType thingType, Net.OutputMessage binaryWriter, int startPhase, int phasesLimit) {
            binaryWriter.AddU8(1);
            binaryWriter.AddS32(thingType.HasAttribute(AttributesUniform.AnimateAlways) || thingType.Category == ThingCategory.Item ? 0 : 1);
            binaryWriter.AddU8(0);

            int duration;
            if (thingType.Category == ThingCategory.Effect)
                duration = 75;
            else
                duration = phasesLimit > 0 ? 1000 / phasesLimit : 40;

            for (int i = 0; i < phasesLimit; i++) {
                binaryWriter.AddS32(duration); // force legacy animation
                binaryWriter.AddS32(duration);
            }
        }

        public void Serialize(Net.OutputMessage binaryWriter, int startPhase, int phasesLimit) {
            binaryWriter.AddU8(Async ? (byte)1 : (byte)0);
            binaryWriter.AddS32(LoopCount);

            int minPhase = startPhase;
            int maxPhase = startPhase = phasesLimit;
            if (StartPhase > 0 && (StartPhase < minPhase || StartPhase > maxPhase))
                binaryWriter.AddS8((sbyte)minPhase);
            else
                binaryWriter.AddS8(StartPhase);

            for (int i = 0; i < phasesLimit; i++) {
                var frameGroupDuration = FrameGroupDurations[startPhase + i];
                binaryWriter.AddS32(frameGroupDuration.Minimum);
                binaryWriter.AddS32(frameGroupDuration.Maximum);
            }
        }

        public void Unserialize(byte animationPhases, Net.InputMessage binaryReader) {
            AnimationPhases = animationPhases;
            Async = binaryReader.GetU8() == 0;
            LoopCount = binaryReader.GetS32();
            StartPhase = binaryReader.GetS8();

            for (int i = 0; i < animationPhases; i++) {
                var duration = new FrameGroupDuration();
                duration.Minimum = binaryReader.GetS32();
                duration.Maximum = binaryReader.GetS32();

                FrameGroupDurations.Add(duration);
            }
        }
    }

    public sealed class FrameGroup
    {
        public byte Width { get; private set; }
        public byte Height { get; private set; }
        public byte ExactSize { get; private set; }
        public byte Layers { get; private set; }
        public byte PatternWidth { get; private set; }
        public byte PatternHeight { get; private set; }
        public byte PatternDepth { get; private set; }
        public byte Phases { get; private set; }
        public FrameGroupAnimator Animator { get; private set; }
        public List<uint> Sprites { get; private set; } = new List<uint>();

        public void Serialize(ThingType thingType, Net.OutputMessage binaryWriter, int fromVersion, int newVersion, sbyte startPhase, byte phasesLimit) {
            binaryWriter.AddU8(Width);
            binaryWriter.AddU8(Height);
            if (Width > 1 || Height > 1)
                binaryWriter.AddU8(ExactSize);

            binaryWriter.AddU8(Layers);
            binaryWriter.AddU8(PatternWidth);
            binaryWriter.AddU8(PatternHeight);
            if (newVersion >= 755)
                binaryWriter.AddU8(PatternDepth);
            
            binaryWriter.AddU8(phasesLimit);

            if (fromVersion < 1050) {
                if (phasesLimit > 1 && newVersion >= 1050)
                    FrameGroupAnimator.SerializeLegacy(thingType, binaryWriter, startPhase, phasesLimit);
            } else {
                if (phasesLimit > 1 && newVersion >= 1050)
                    Animator.Serialize(binaryWriter, startPhase, phasesLimit);
            }
            
            int spritesPerPhase = Width * Height * Layers * PatternWidth * PatternHeight * PatternDepth;
            int totalSprites = phasesLimit * spritesPerPhase;
            int offset = startPhase * spritesPerPhase;
            for (int j = 0; j < totalSprites; j++) {
                uint spriteId = Sprites[offset + j];
                if (newVersion >= 960)
                    binaryWriter.AddU32(spriteId);
                else
                    binaryWriter.AddU16((ushort)spriteId);
            }
        }

        public void Unserialize(Net.InputMessage binaryReader, int clientVersion) {
            Width = binaryReader.GetU8();
            Height = binaryReader.GetU8();
            if (Width > 1 || Height > 1)
                ExactSize = binaryReader.GetU8();
            else
                ExactSize = 32;

            Layers = binaryReader.GetU8();
            PatternWidth = binaryReader.GetU8();
            PatternHeight = binaryReader.GetU8();
            PatternDepth = clientVersion >= 755 ? binaryReader.GetU8() : (byte)1;
            Phases = binaryReader.GetU8();

            if (Phases > 1 && clientVersion >= 1050) {
                Animator = new FrameGroupAnimator();
                Animator.Unserialize(Phases, binaryReader);
            }

            int totalSprites = Width * Height * Layers * PatternWidth * PatternHeight * PatternDepth * Phases;
            for (int j = 0; j < totalSprites; j++)
                Sprites.Add(clientVersion >= 960 ? binaryReader.GetU32() : binaryReader.GetU16());
        }
    }
}
