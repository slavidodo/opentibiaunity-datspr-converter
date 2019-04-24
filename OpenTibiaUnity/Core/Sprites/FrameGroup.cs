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
        private System.Random m_Random = new System.Random();

        public int Duration {
            get {
                if (Minimum == Maximum) {
                    return Minimum;
                }

                return m_Random.Next(Minimum, Maximum);
            }
        }
    }


    public sealed class FrameGroupAnimator
    {
        public byte AnimationPhases { get; private set; } = 0;
        public bool Async { get; private set; } = false;
        public int LoopCount { get; private set; } = 0;
        public int StartPhase { get; private set; } = -1;
        public int CurrentPhase { get; private set; } = 0;
        public int CurrentDuration { get; private set; } = 0;
        public long LastPhaseTicks { get; private set; } = 0;
        public byte AnimationDirection { get; private set; } = 0;
        public bool IsComplete { get; private set; } = false;
        public byte CurrentLoop { get; private set; } = 0;
        public List<FrameGroupDuration> FrameGroupDurations { get; private set; } = new List<FrameGroupDuration>();

        private System.Random m_Random = new System.Random();

        public static FrameGroupAnimator Unserialize(byte animationPhases, Net.InputMessage binaryReader) {
            FrameGroupAnimator frameGroupAnimator = new FrameGroupAnimator();

            frameGroupAnimator.AnimationPhases = animationPhases;
            frameGroupAnimator.Async = binaryReader.GetU8() == 0;
            frameGroupAnimator.LoopCount = binaryReader.GetS32();
            frameGroupAnimator.StartPhase = binaryReader.GetS8();

            for (int i = 0; i < animationPhases; i++) {
                FrameGroupDuration duration = new FrameGroupDuration();
                duration.Minimum = (int)binaryReader.GetU32();
                duration.Maximum = (int)binaryReader.GetU32();

                frameGroupAnimator.FrameGroupDurations.Add(duration);
            }

            return frameGroupAnimator;
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

        public static FrameGroup Unserialize(ThingCategory category, Net.InputMessage binaryReader, int clientVersion) {
            FrameGroup frameGroup = new FrameGroup();

            frameGroup.Width = binaryReader.GetU8();
            frameGroup.Height = binaryReader.GetU8();
            if (frameGroup.Width > 1 || frameGroup.Height > 1)
                frameGroup.ExactSize = binaryReader.GetU8();
            else
                frameGroup.ExactSize = 32;

            frameGroup.Layers = binaryReader.GetU8();
            frameGroup.PatternWidth = binaryReader.GetU8();
            frameGroup.PatternHeight = binaryReader.GetU8();
            frameGroup.PatternDepth = clientVersion >= 755 ? binaryReader.GetU8() : (byte)1;
            frameGroup.Phases = binaryReader.GetU8();

            if (frameGroup.Phases > 1 && clientVersion >= 1050)
                frameGroup.Animator = FrameGroupAnimator.Unserialize(frameGroup.Phases, binaryReader);

            int totalSprites = frameGroup.Width * frameGroup.Height * frameGroup.Layers * frameGroup.PatternWidth * frameGroup.PatternHeight * frameGroup.PatternDepth * frameGroup.Phases;
            for (int j = 0; j < totalSprites; j++)
                frameGroup.Sprites.Add(clientVersion >= 960 ? binaryReader.GetU32() : binaryReader.GetU16());

            return frameGroup;
        }
    }
}
