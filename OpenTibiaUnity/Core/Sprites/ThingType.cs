using OpenTibiaUnity.Core.Metaflags;
using System;
using System.Collections.Generic;

namespace OpenTibiaUnity.Core.Sprites
{
    public class LightInfo
    {
        public ushort intensity = 0;
        public ushort color = 0;
    }

    public sealed class MarketData
    {
        public string name;
        public ushort category;
        public ushort restrictLevel;
        public ushort restrictProfession;
        public ushort showAs;
        public ushort tradeAs;
    }

    public sealed class Vector2Int
    {
        public ushort x = 0;
        public ushort y = 0;

        public Vector2Int(ushort _x, ushort _y) { x = _x; y = _y; }
    }

    public sealed class ThingType
    {
        public ThingCategory Category { get; set; }
        public ushort ID { get; set; }
        public Dictionary<byte, object> Attributes { get; private set; } = new Dictionary<byte, object>();
        public Dictionary<FrameGroupType, FrameGroup> FrameGroups { get; private set; } = new Dictionary<FrameGroupType, FrameGroup>();

        public bool HasAttribute(byte attr) {
            return Attributes.TryGetValue(attr, out object _);
        }

        public void Serialize(Net.OutputMessage binaryWriter, int fromVersion, int newVersion) {
            if (newVersion <= 730)
                Serialize730(binaryWriter);
            else if (newVersion <= 750)
                Serialize750(binaryWriter);
            else if (newVersion <= 772)
                Serialize772(binaryWriter);
            else if (newVersion <= 854)
                Serialize854(binaryWriter);
            else if (newVersion <= 986)
                Serialize986(binaryWriter);
            else // Tibia 10
                Serialize1010(binaryWriter, newVersion);

            // the whole idea is how to animate outfits correctly in different versions
            if (Category != ThingCategory.Creature) {
                if (ID == 424 && Category == ThingCategory.Item) {
                    Console.WriteLine("Phases: " + FrameGroups[0].Phases);
                }

                FrameGroups[0].Serialize(this, binaryWriter, fromVersion, newVersion, 0, FrameGroups[0].Phases);
                return;
            }
            
            if (fromVersion < 1057) {
                if (newVersion < 1057) {
                    FrameGroups[0].Serialize(this, binaryWriter, fromVersion, newVersion, 0, FrameGroups[0].Phases);
                } else {
                    // current uses phases, newer uses frame groups
                    if (FrameGroups[0].Phases == 1 || HasAttribute(AttributesUniform.AnimateAlways)) {
                        binaryWriter.AddU8(1);
                        binaryWriter.AddU8((int)FrameGroupType.Idle);
                        FrameGroups[0].Serialize(this, binaryWriter, fromVersion, newVersion, 0, FrameGroups[0].Phases);
                        return;
                    }

                    binaryWriter.AddU8(2);
                    binaryWriter.AddU8((int)FrameGroupType.Idle);
                    FrameGroups[0].Serialize(this, binaryWriter, fromVersion, newVersion, 0, 1);
                    binaryWriter.AddU8((int)FrameGroupType.Idle);
                    FrameGroups[0].Serialize(this, binaryWriter, fromVersion, newVersion, 1, (byte)(FrameGroups[0].Phases - 1));
                }
            } else {
                if (newVersion < 1057) {
                    throw new Exception("It's not possible to convert a client >= 1057 to a client < 1057");
                } else {
                    binaryWriter.AddU8((byte)FrameGroups.Count);
                    foreach (var pair in FrameGroups) {
                        binaryWriter.AddU8((byte)pair.Key);
                        pair.Value.Serialize(this, binaryWriter, fromVersion, newVersion, 0, pair.Value.Phases);
                    }
                }
            }
        }

        public void Unserialize(Net.InputMessage binaryReader, int clientVersion) {
            int lastAttr = 0, previousAttr = 0, attr = 0;
            bool done;
            try {
                if (clientVersion <= 730)
                    done = Unserialize730(binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 750)
                    done = Unserialize750(binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 772)
                    done = Unserialize772(binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 854)
                    done = Unserialize854(binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 986)
                    done = Unserialize986(binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else // Tibia 10
                    done = Unserialize1010(binaryReader, ref lastAttr, ref previousAttr, ref attr);
            } catch (Exception e) {
                throw new Exception(string.Format("Parsing Failed ({0}). (attr: 0x{1:X2}, previous: 0x{2:X2}, last: 0x{3:X2})", e, attr, previousAttr, lastAttr));
            }

            if (!done)
                throw new Exception("Couldn't parse thing [category: " + Category + ", ID: " + ID + "].");
            
            bool hasFrameGroups = Category == ThingCategory.Creature && clientVersion >= 1057;
            byte groupCount = hasFrameGroups ? binaryReader.GetU8() : (byte)1U;
            for (int i = 0; i < groupCount; i++) {
                FrameGroupType groupType = FrameGroupType.Default;
                if (hasFrameGroups)
                    groupType = (FrameGroupType)binaryReader.GetU8();

                FrameGroup frameGroup = new FrameGroup();
                frameGroup.Unserialize(binaryReader, clientVersion);
                FrameGroups[groupType] = frameGroup;
            }
        }

        private void ThrowUnknownFlag(int attr) {
            throw new ArgumentException(string.Format("Unknown flag (ID = {0}, Category = {1}): {2}", ID, Category, attr));
        }

        private void Serialize730(Net.OutputMessage binaryWriter) {
            foreach (var pair in Attributes) {
                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes730.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes730.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes730.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes730.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes730.Stackable);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes730.MultiUse);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes730.ForceUse);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes730.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes730.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes730.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes730.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes730.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes730.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes730.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes730.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes730.Pickupable);
                        break;
                    case AttributesUniform.Light:
                        binaryWriter.AddU8(Attributes730.Light);
                        LightInfo data = (LightInfo)pair.Value;
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.FloorChange:
                        binaryWriter.AddU8(Attributes730.FloorChange);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes730.FullGround);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes730.FullGround);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Offset:
                        binaryWriter.AddU8(Attributes730.Offset);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes730.MinimapColor);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes730.Rotateable);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes730.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes730.AnimateAlways);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes730.AnimateAlways);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                }
            }

            binaryWriter.AddU8(Attributes730.Last);
        }

        private bool Unserialize730(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes730.Last) {
                switch (attr) {
                    case Attributes730.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes730.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes730.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes730.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes730.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes730.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes730.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes730.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes730.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes730.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes730.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes730.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes730.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes730.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes730.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes730.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes730.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes730.FloorChange:
                        Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes730.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes730.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes730.Offset:
                        Attributes[AttributesUniform.Offset] = new Vector2Int(8, 8);
                        break;
                    case Attributes730.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes730.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes730.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes730.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes730.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;

                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private void Serialize750(Net.OutputMessage binaryWriter) {
            foreach (var pair in Attributes) {
                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes750.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes750.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes750.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes750.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes750.Stackable);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes750.MultiUse);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes750.ForceUse);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes750.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes750.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes750.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes750.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes750.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes750.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes750.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes750.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes750.Pickupable);
                        break;
                    case AttributesUniform.Light:
                        var data = (LightInfo)Attributes[AttributesUniform.Light];
                        binaryWriter.AddU8(Attributes750.Light);
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.FloorChange:
                        binaryWriter.AddU8(Attributes750.FloorChange);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes750.FullGround);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes750.Elevation);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Offset:
                        binaryWriter.AddU8(Attributes750.Offset);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes750.MinimapColor);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes750.Rotateable);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes750.LyingCorpse);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.AddU8(Attributes750.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.AddU8(Attributes750.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.AddU8(Attributes750.HookEast);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes750.AnimateAlways);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes750.LensHelp);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;

                    default:
                        break;
                }
            }

            binaryWriter.AddU8(Attributes750.Last);
        }

        private bool Unserialize750(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes750.Last) {
                switch (attr) {
                    case Attributes750.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes750.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes750.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes750.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes750.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes750.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes750.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes750.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes750.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes750.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes750.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes750.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes750.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes750.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes750.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes750.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes750.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes750.FloorChange:
                        Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes750.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes750.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes750.Offset:
                        Attributes[AttributesUniform.Offset] = new Vector2Int(8, 8);
                        break;
                    case Attributes750.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes750.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes750.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes750.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes750.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes750.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes750.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes750.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;

                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private void Serialize772(Net.OutputMessage binaryWriter) {
            foreach (var pair in Attributes) {
                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes772.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.GroundBorder:
                        binaryWriter.AddU8(Attributes772.GroundBorder);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes772.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes772.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes772.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes772.Stackable);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes772.MultiUse);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes772.ForceUse);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes772.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes772.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes772.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes772.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes772.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes772.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes772.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes772.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes772.Pickupable);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.AddU8(Attributes772.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.AddU8(Attributes772.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.AddU8(Attributes772.HookEast);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes772.Rotateable);
                        break;
                    case AttributesUniform.Light:
                        var data = (LightInfo)pair.Value;
                        binaryWriter.AddU8(Attributes772.LensHelp);
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.FloorChange:
                        binaryWriter.AddU8(Attributes772.FloorChange);
                        break;
                    case AttributesUniform.Offset:
                        var offset = (Vector2Int)pair.Value;
                        binaryWriter.AddU8(Attributes772.LensHelp);
                        binaryWriter.AddU16((ushort)offset.x);
                        binaryWriter.AddU16((ushort)offset.y);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes772.Elevation);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes772.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes772.AnimateAlways);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes772.MinimapColor);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes772.LensHelp);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes772.FullGround);
                        break;
                }
            }

            binaryWriter.AddU8(Attributes772.Last);
        }

        private bool Unserialize772(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes772.Last) {
                switch (attr) {
                    case Attributes772.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes772.GroundBorder:
                        Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes772.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes772.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes772.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes772.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes772.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes772.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes772.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes772.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes772.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes772.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes772.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes772.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes772.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes772.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes772.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes772.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes772.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes772.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes772.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes772.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes772.FloorChange:
                        Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes772.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes772.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes772.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes772.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes772.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes772.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes772.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    default:
                        throw new System.ArgumentException("Unknown Attribute: " + attr);
                }
                
                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private void Serialize854(Net.OutputMessage binaryWriter) {
            foreach (var pair in Attributes) {
                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes854.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.GroundBorder:
                        binaryWriter.AddU8(Attributes854.GroundBorder);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes854.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes854.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes854.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes854.Stackable);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes854.ForceUse);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes854.MultiUse);
                        break;
                    case AttributesUniform.Charges:
                        binaryWriter.AddU8(Attributes854.Charges);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes854.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes854.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes854.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes854.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes854.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes854.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes854.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes854.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes854.Pickupable);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.AddU8(Attributes854.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.AddU8(Attributes854.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.AddU8(Attributes854.HookEast);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes854.Rotateable);
                        break;
                    case AttributesUniform.Light:
                        var data = (LightInfo)pair.Value;
                        binaryWriter.AddU8(Attributes854.Light);
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.DontHide:
                        binaryWriter.AddU8(Attributes854.DontHide);
                        break;
                    case AttributesUniform.FloorChange:
                        binaryWriter.AddU8(Attributes854.FloorChange);
                        break;
                    case AttributesUniform.Offset:
                        var offset = (Vector2Int)pair.Value;
                        binaryWriter.AddU8(Attributes854.Light);
                        binaryWriter.AddU16(offset.x);
                        binaryWriter.AddU16(offset.y);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes854.Elevation);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes854.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes854.AnimateAlways);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes854.MinimapColor);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes854.LensHelp);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes854.FullGround);
                        break;
                    default:
                        break;
                }
            }

            binaryWriter.AddU8(Attributes854.Last);
        }

        private bool Unserialize854(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes854.Last) {
                switch (attr) {
                    case Attributes854.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes854.GroundBorder:
                        Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes854.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes854.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes854.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes854.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes854.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes854.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes854.Charges:
                        Attributes[AttributesUniform.Charges] = true;
                        break;
                    case Attributes854.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes854.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes854.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes854.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes854.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes854.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes854.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes854.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes854.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes854.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes854.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes854.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes854.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes854.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes854.DontHide:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes854.FloorChange:
                        Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes854.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes854.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes854.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes854.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes854.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes854.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes854.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private void Serialize986(Net.OutputMessage binaryWriter) {
            foreach (var pair in Attributes) {
                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes986.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.GroundBorder:
                        binaryWriter.AddU8(Attributes986.GroundBorder);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes986.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes986.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes986.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes986.Stackable);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes986.ForceUse);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes986.MultiUse);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes986.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes986.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes986.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes986.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes986.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes986.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes986.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes986.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes986.Pickupable);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.AddU8(Attributes986.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.AddU8(Attributes986.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.AddU8(Attributes986.HookEast);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes986.Rotateable);
                        break;
                    case AttributesUniform.Light:
                        var data = (LightInfo)pair.Value;
                        binaryWriter.AddU8(Attributes986.Light);
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.DontHide:
                        binaryWriter.AddU8(Attributes986.DontHide);
                        break;
                    case AttributesUniform.Translucent:
                        binaryWriter.AddU8(Attributes986.Translucent);
                        break;
                    case AttributesUniform.Offset:
                        var offset = (Vector2Int)pair.Value;
                        binaryWriter.AddU8(Attributes986.Light);
                        binaryWriter.AddU16(offset.x);
                        binaryWriter.AddU16(offset.y);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes986.Elevation);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes986.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes986.AnimateAlways);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes986.MinimapColor);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes986.LensHelp);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes986.FullGround);
                        break;
                    case AttributesUniform.Look:
                        binaryWriter.AddU8(Attributes986.Look);
                        break;
                    case AttributesUniform.Cloth:
                        binaryWriter.AddU8(Attributes986.Cloth);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Market:
                        var marketData = (MarketData)pair.Value;
                        binaryWriter.AddU8(Attributes986.Cloth);
                        binaryWriter.AddU16(marketData.category);
                        binaryWriter.AddU16(marketData.tradeAs);
                        binaryWriter.AddU16(marketData.showAs);
                        binaryWriter.AddString(marketData.name);
                        binaryWriter.AddU16(marketData.restrictProfession);
                        binaryWriter.AddU16(marketData.restrictLevel);
                        break;
                    default:
                        break;
                }
            }

            binaryWriter.AddU8(Attributes986.Last);
        }

        private bool Unserialize986(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes986.Last) {
                switch (attr) {
                    case Attributes986.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes986.GroundBorder:
                        Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes986.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes986.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes986.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes986.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes986.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes986.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes986.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes986.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes986.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes986.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes986.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes986.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes986.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes986.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes986.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes986.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes986.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes986.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes986.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes986.Light:
                        LightInfo lightData = new LightInfo();
                        lightData.intensity = binaryReader.GetU16();
                        lightData.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = lightData;
                        break;
                    case Attributes986.DontHide:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes986.Translucent:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes986.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes986.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes986.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes986.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes986.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes986.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes986.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes986.Look:
                        Attributes[AttributesUniform.Look] = true;
                        break;
                    case Attributes986.Cloth:
                        Attributes[AttributesUniform.Cloth] = binaryReader.GetU16();
                        break;
                    case Attributes986.Market:
                        MarketData marketData = new MarketData();
                        marketData.category = binaryReader.GetU16();
                        marketData.tradeAs = binaryReader.GetU16();
                        marketData.showAs = binaryReader.GetU16();
                        marketData.name = binaryReader.GetString();
                        marketData.restrictProfession = binaryReader.GetU16();
                        marketData.restrictLevel = binaryReader.GetU16();

                        Attributes[AttributesUniform.Market] = marketData;
                        break;
                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private void Serialize1010(Net.OutputMessage binaryWriter, int clientVersion) {
            foreach (var pair in Attributes) {
                if (ID == 424 && Category == ThingCategory.Item)
                    Console.WriteLine("Writing: " + pair.Key);

                switch (pair.Key) {
                    case AttributesUniform.Ground:
                        binaryWriter.AddU8(Attributes1056.Ground);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.GroundBorder:
                        binaryWriter.AddU8(Attributes1056.GroundBorder);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.AddU8(Attributes1056.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.AddU8(Attributes1056.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.AddU8(Attributes1056.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.AddU8(Attributes1056.Stackable);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.AddU8(Attributes1056.ForceUse);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.AddU8(Attributes1056.MultiUse);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.AddU8(Attributes1056.Writable);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.AddU8(Attributes1056.WritableOnce);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.AddU8(Attributes1056.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.AddU8(Attributes1056.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.AddU8(Attributes1056.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.AddU8(Attributes1056.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.AddU8(Attributes1056.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.AddU8(Attributes1056.BlockPath);
                        break;
                    case AttributesUniform.NoMoveAnimation:
                        binaryWriter.AddU8(Attributes1056.NoMoveAnimation);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.AddU8(Attributes1056.Pickupable);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.AddU8(Attributes1056.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.AddU8(Attributes1056.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.AddU8(Attributes1056.HookEast);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.AddU8(Attributes1056.Rotateable);
                        break;
                    case AttributesUniform.Light:
                        var data = (LightInfo)pair.Value;
                        binaryWriter.AddU8(Attributes1056.Light);
                        binaryWriter.AddU16(data.intensity);
                        binaryWriter.AddU16(data.color);
                        break;
                    case AttributesUniform.DontHide:
                        binaryWriter.AddU8(Attributes1056.DontHide);
                        break;
                    case AttributesUniform.Translucent:
                        binaryWriter.AddU8(Attributes1056.Translucent);
                        break;
                    case AttributesUniform.Offset:
                        var offset = (Vector2Int)pair.Value;
                        binaryWriter.AddU8(Attributes1056.Offset);
                        binaryWriter.AddU16(offset.x);
                        binaryWriter.AddU16(offset.y);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.AddU8(Attributes1056.Elevation);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.AddU8(Attributes1056.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.AddU8(Attributes1056.AnimateAlways);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.AddU8(Attributes1056.MinimapColor);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.AddU8(Attributes1056.LensHelp);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.AddU8(Attributes1056.FullGround);
                        break;
                    case AttributesUniform.Look:
                        binaryWriter.AddU8(Attributes1056.Look);
                        break;
                    case AttributesUniform.Cloth:
                        binaryWriter.AddU8(Attributes1056.Cloth);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Market:
                        var marketData = (MarketData)Attributes[AttributesUniform.Market];

                        binaryWriter.AddU8(Attributes1056.Market);
                        binaryWriter.AddU16(marketData.category);
                        binaryWriter.AddU16(marketData.tradeAs);
                        binaryWriter.AddU16(marketData.showAs);
                        binaryWriter.AddString(marketData.name);
                        binaryWriter.AddU16(marketData.restrictProfession);
                        binaryWriter.AddU16(marketData.restrictLevel);
                        break;
                    case AttributesUniform.DefaultAction:
                        binaryWriter.AddU8(Attributes1056.DefaultAction);
                        binaryWriter.AddU16((ushort)pair.Value);
                        break;
                    case AttributesUniform.Use:
                        binaryWriter.AddU8(Attributes1056.Use);
                        break;
                    case AttributesUniform.Wrapable:
                        if (clientVersion >= 1092)
                            binaryWriter.AddU8(Attributes1056.Wrapable);
                        break;
                    case AttributesUniform.Unwrapable:
                        if (clientVersion >= 1092)
                            binaryWriter.AddU8(Attributes1056.Unwrapable);
                        break;
                    case AttributesUniform.TopEffect:
                        if (clientVersion >= 1093)
                            binaryWriter.AddU8(Attributes1056.TopEffect);
                        break;
                }
            }

            binaryWriter.AddU8(Attributes1056.Last);
        }

        private bool Unserialize1010(Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes1056.Last) {
                if (ID == 424 && Category == ThingCategory.Item)
                    Console.WriteLine("Reading: " + attr);
                
                switch (attr) {
                    case Attributes1056.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes1056.GroundBorder:
                        Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes1056.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes1056.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes1056.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes1056.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes1056.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes1056.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes1056.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes1056.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes1056.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes1056.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes1056.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes1056.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes1056.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes1056.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes1056.NoMoveAnimation:
                        Attributes[AttributesUniform.NoMoveAnimation] = true;
                        break;
                    case Attributes1056.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes1056.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes1056.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes1056.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes1056.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes1056.Light:
                        LightInfo lightData = new LightInfo();
                        lightData.intensity = binaryReader.GetU16();
                        lightData.color = binaryReader.GetU16();
                        Attributes[AttributesUniform.Light] = lightData;
                        break;
                    case Attributes1056.DontHide:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes1056.Translucent:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes1056.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes1056.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes1056.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes1056.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes1056.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes1056.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes1056.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes1056.Look:
                        Attributes[AttributesUniform.Look] = true;
                        break;
                    case Attributes1056.Cloth:
                        Attributes[AttributesUniform.Cloth] = binaryReader.GetU16();
                        break;
                    case Attributes1056.Market:
                        MarketData marketData = new MarketData();
                        marketData.category = binaryReader.GetU16();
                        marketData.tradeAs = binaryReader.GetU16();
                        marketData.showAs = binaryReader.GetU16();
                        marketData.name = binaryReader.GetString();
                        marketData.restrictProfession = binaryReader.GetU16();
                        marketData.restrictLevel = binaryReader.GetU16();

                        Attributes[AttributesUniform.Market] = marketData;
                        break;
                    case Attributes1056.DefaultAction:
                        Attributes[AttributesUniform.DefaultAction] = binaryReader.GetU16();
                        break;
                    case Attributes1056.Use:
                        Attributes[AttributesUniform.Use] = true;
                        break;
                    case Attributes1056.Wrapable:
                        Attributes[AttributesUniform.Wrapable] = true;
                        break;
                    case Attributes1056.Unwrapable:
                        Attributes[AttributesUniform.Unwrapable] = true;
                        break;
                    case Attributes1056.TopEffect:
                        Attributes[AttributesUniform.TopEffect] = true;
                        break;
                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }
    }
}