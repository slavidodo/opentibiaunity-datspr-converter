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
        public int category;
        public ushort restrictLevel;
        public ushort restrictProfession;
        public ushort showAs;
        public ushort tradeAs;
    }

    public sealed class Vector2Int
    {
        public int x = 0;
        public int y = 0;

        public Vector2Int(int _x, int _y) { x = _x; y = _y; }
    }

    public sealed class ThingType
    {
        public ThingCategory Category { get; private set; }
        public ushort ID { get; private set; }
        public ushort Elevation { get; private set; }
        public Dictionary<byte, object> Attributes { get; private set; } = new Dictionary<byte, object>();
        public Dictionary<FrameGroupType, FrameGroup> FrameGroups { get; private set; } = new Dictionary<FrameGroupType, FrameGroup>();

        public bool HasAttribute(byte attr) {
            return Attributes.TryGetValue(attr, out object _);
        }

        public static ThingType Unserialize(ushort id, ThingCategory category, Net.InputMessage binaryReader, int clientVersion) {
            ThingType thingType = new ThingType() {
                ID = id,
                Category = category
            };

            int lastAttr = 0, previousAttr = 0, attr = 0;
            bool done;
            try {
                if (clientVersion <= 730)
                    done = Unserialize730(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 750)
                    done = Unserialize750(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 772)
                    done = Unserialize772(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 854)
                    done = Unserialize854(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else if (clientVersion <= 986)
                    done = Unserialize986(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
                else // Tibia 10
                    done = Unserialize1010(thingType, binaryReader, ref lastAttr, ref previousAttr, ref attr);
            } catch (Exception e) {
                throw new Exception(string.Format("Parsing Failed ({0}). (attr: 0x{1:X2}, previous: 0x{2:X2}, last: 0x{3:X2})", e, attr, previousAttr, lastAttr));
            }

            if (!done)
                throw new Exception("Couldn't parse thing [category: " + category + ", ID: " + id + "].");
            
            bool hasFrameGroups = category == ThingCategory.Creature && clientVersion >= 1057;
            byte groupCount = hasFrameGroups ? binaryReader.GetU8() : (byte)1U;
            for (int i = 0; i < groupCount; i++) {
                FrameGroupType groupType = FrameGroupType.Default;
                if (hasFrameGroups)
                    groupType = (FrameGroupType)binaryReader.GetU8();

                thingType.FrameGroups[groupType] = FrameGroup.Unserialize(category, binaryReader, clientVersion);
            }

            return thingType;
        }

        private static bool Unserialize730(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes730.Last) {
                switch (attr) {
                    case Attributes730.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes730.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes730.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes730.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes730.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes730.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes730.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes730.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes730.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes730.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes730.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes730.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes730.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes730.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes730.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes730.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes730.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes730.FloorChange:
                        thingType.Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes730.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes730.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes730.Offset:
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(8, 8);
                        break;
                    case Attributes730.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes730.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes730.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes730.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes730.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;

                    default:
                        throw new ArgumentException("Unknown flag: " + attr);
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private static bool Unserialize750(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes750.Last) {
                switch (attr) {
                    case Attributes750.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes750.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes750.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes750.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes750.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes750.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes750.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes750.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes750.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes750.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes750.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes750.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes750.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes750.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes750.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes750.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes750.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes750.FloorChange:
                        thingType.Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes750.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes750.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes750.Offset:
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(8, 8);
                        break;
                    case Attributes750.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes750.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes750.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes750.Hangable:
                        thingType.Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes750.HookSouth:
                        thingType.Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes750.HookEast:
                        thingType.Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes750.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes750.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;

                    default:
                        throw new ArgumentException("Unknown flag: " + attr);
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private static bool Unserialize772(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes772.Last) {
                switch (attr) {
                    case Attributes772.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes772.GroundBorder:
                        thingType.Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes772.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes772.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes772.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes772.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes772.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes772.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes772.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes772.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes772.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes772.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes772.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes772.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes772.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes772.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes772.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes772.Hangable:
                        thingType.Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes772.HookSouth:
                        thingType.Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes772.HookEast:
                        thingType.Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes772.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes772.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes772.FloorChange:
                        thingType.Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes772.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes772.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes772.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes772.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes772.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes772.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes772.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
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

        private static bool Unserialize854(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes854.Last) {
                switch (attr) {
                    case Attributes854.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes854.GroundBorder:
                        thingType.Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes854.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes854.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes854.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes854.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes854.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes854.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes854.Charges:
                        thingType.Attributes[AttributesUniform.Charges] = true;
                        break;
                    case Attributes854.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes854.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes854.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes854.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes854.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes854.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes854.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes854.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes854.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes854.Hangable:
                        thingType.Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes854.HookSouth:
                        thingType.Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes854.HookEast:
                        thingType.Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes854.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes854.Light:
                        LightInfo data = new LightInfo();
                        data.intensity = binaryReader.GetU16();
                        data.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes854.DontHide:
                        thingType.Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes854.FloorChange:
                        thingType.Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes854.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes854.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes854.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes854.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes854.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes854.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes854.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
                        break;
                    default:
                        throw new ArgumentException("Unknown flag: " + attr);
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private static bool Unserialize986(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes986.Last) {
                switch (attr) {
                    case Attributes986.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes986.GroundBorder:
                        thingType.Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes986.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes986.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes986.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes986.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes986.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes986.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes986.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes986.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes986.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes986.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes986.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes986.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes986.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes986.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes986.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes986.Hangable:
                        thingType.Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes986.HookSouth:
                        thingType.Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes986.HookEast:
                        thingType.Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes986.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes986.Light:
                        LightInfo lightData = new LightInfo();
                        lightData.intensity = binaryReader.GetU16();
                        lightData.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = lightData;
                        break;
                    case Attributes986.DontHide:
                        thingType.Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes986.Translucent:
                        thingType.Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes986.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes986.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes986.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes986.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes986.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes986.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes986.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes986.Look:
                        thingType.Attributes[AttributesUniform.Look] = true;
                        break;
                    case Attributes986.Cloth:
                        thingType.Attributes[AttributesUniform.Cloth] = binaryReader.GetU16();
                        break;
                    case Attributes986.Market:
                        MarketData marketData = new MarketData();
                        marketData.category = binaryReader.GetU16();
                        marketData.tradeAs = binaryReader.GetU16();
                        marketData.showAs = binaryReader.GetU16();
                        marketData.name = binaryReader.GetString();
                        marketData.restrictProfession = binaryReader.GetU16();
                        marketData.restrictLevel = binaryReader.GetU16();

                        thingType.Attributes[AttributesUniform.Market] = marketData;
                        break;
                    default:
                        throw new ArgumentException("Unknown flag: " + attr);
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }

        private static bool Unserialize1010(ThingType thingType, Net.InputMessage binaryReader, ref int lastAttr, ref int previousAttr, ref int attr) {
            attr = binaryReader.GetU8();
            while (attr < Attributes1056.Last) {
                switch (attr) {
                    case Attributes1056.Ground:
                        thingType.Attributes[AttributesUniform.Ground] = binaryReader.GetU16();
                        break;
                    case Attributes1056.GroundBorder:
                        thingType.Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes1056.Bottom:
                        thingType.Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes1056.Top:
                        thingType.Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes1056.Container:
                        thingType.Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes1056.Stackable:
                        thingType.Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes1056.ForceUse:
                        thingType.Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes1056.MultiUse:
                        thingType.Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes1056.Writable:
                        thingType.Attributes[AttributesUniform.Writable] = binaryReader.GetU16();
                        break;
                    case Attributes1056.WritableOnce:
                        thingType.Attributes[AttributesUniform.WritableOnce] = binaryReader.GetU16();
                        break;
                    case Attributes1056.FluidContainer:
                        thingType.Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes1056.Splash:
                        thingType.Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes1056.Unpassable:
                        thingType.Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes1056.Unmoveable:
                        thingType.Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes1056.Unsight:
                        thingType.Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes1056.BlockPath:
                        thingType.Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes1056.NoMoveAnimation:
                        thingType.Attributes[AttributesUniform.NoMoveAnimation] = true;
                        break;
                    case Attributes1056.Pickupable:
                        thingType.Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes1056.Hangable:
                        thingType.Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes1056.HookSouth:
                        thingType.Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes1056.HookEast:
                        thingType.Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes1056.Rotateable:
                        thingType.Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes1056.Light:
                        LightInfo lightData = new LightInfo();
                        lightData.intensity = binaryReader.GetU16();
                        lightData.color = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Light] = lightData;
                        break;
                    case Attributes1056.DontHide:
                        thingType.Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes1056.Translucent:
                        thingType.Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes1056.Offset:
                        ushort offsetX = binaryReader.GetU16();
                        ushort offsetY = binaryReader.GetU16();
                        thingType.Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes1056.Elevation:
                        thingType.Attributes[AttributesUniform.Elevation] = binaryReader.GetU16();
                        break;
                    case Attributes1056.LyingCorpse:
                        thingType.Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes1056.AnimateAlways:
                        thingType.Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes1056.MinimapColor:
                        thingType.Attributes[AttributesUniform.MinimapColor] = binaryReader.GetU16();
                        break;
                    case Attributes1056.LensHelp:
                        thingType.Attributes[AttributesUniform.LensHelp] = binaryReader.GetU16();
                        break;
                    case Attributes1056.FullGround:
                        thingType.Attributes[AttributesUniform.FullGround] = true;
                        break;
                    case Attributes1056.Look:
                        thingType.Attributes[AttributesUniform.Look] = true;
                        break;
                    case Attributes1056.Cloth:
                        thingType.Attributes[AttributesUniform.Cloth] = binaryReader.GetU16();
                        break;
                    case Attributes1056.Market:
                        MarketData marketData = new MarketData();
                        marketData.category = binaryReader.GetU16();
                        marketData.tradeAs = binaryReader.GetU16();
                        marketData.showAs = binaryReader.GetU16();
                        marketData.name = binaryReader.GetString();
                        marketData.restrictProfession = binaryReader.GetU16();
                        marketData.restrictLevel = binaryReader.GetU16();

                        thingType.Attributes[AttributesUniform.Market] = marketData;
                        break;
                    case Attributes1056.DefaultAction:
                        thingType.Attributes[AttributesUniform.DefaultAction] = binaryReader.GetU16();
                        break;
                    case Attributes1056.Use:
                        thingType.Attributes[AttributesUniform.Use] = true;
                        break;
                    case Attributes1056.Wrapable:
                        thingType.Attributes[AttributesUniform.Wrapable] = true;
                        break;
                    case Attributes1056.Unwrapable:
                        thingType.Attributes[AttributesUniform.Unwrapable] = true;
                        break;
                    case Attributes1056.TopEffect:
                        thingType.Attributes[AttributesUniform.TopEffect] = true;
                        break;
                    default:
                        throw new ArgumentException("Unknown flag: " + attr);
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.GetU8();
            }

            return true;
        }
    }
}