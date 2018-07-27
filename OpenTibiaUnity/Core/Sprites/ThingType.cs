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
        public ushort requiredLevel;
        public ushort restrictVocation;
        public ushort showAs;
        public ushort tradeAs;
    }

    public sealed class Vector2Int
    {
        public int x = 0;
        public int y = 0;
    }

    public sealed class ThingType
    {
        public ThingCategory Category { get; private set; }
        public ushort ID { get; private set; }
        public ushort Elevation { get; private set; }
        public Dictionary<byte, object> Attributes { get; private set; } = new Dictionary<byte, object>();
        public Dictionary<FrameGroupType, FrameGroup> FrameGroups { get; private set; } = new Dictionary<FrameGroupType, FrameGroup>();

        public bool HasAttribute(byte attr) {
            object value;
            return Attributes.TryGetValue(attr, out value);
        }

        public static ThingType Serialize(ushort id, ThingCategory category, ref Net.InputMessage binaryReader) {
            ThingType type = new ThingType() {
                ID = id,
                Category = category
            };

            int count = 0, attr = -1;
            bool done = false;

            for (int i = 0; i < DatAttributes.LastAttr; ++i) {
                count++;
                attr = binaryReader.GetU8();
                if (attr == DatAttributes.LastAttr) {
                    done = true;
                    break;
                }
                
                switch (attr) {
                    case DatAttributes.Ground:
                    case DatAttributes.Writable:
                    case DatAttributes.WritableOnce:
                    case DatAttributes.MinimapColor:
                    case DatAttributes.LensHelp:
                    case DatAttributes.Cloth:
                    case DatAttributes.DefaultAction:
                        type.Attributes[(byte)attr] = binaryReader.GetU16();
                        break;

                    case DatAttributes.GroundBorder:
                    case DatAttributes.OnBottom:
                    case DatAttributes.OnTop:
                    case DatAttributes.Container:
                    case DatAttributes.Stackable:
                    case DatAttributes.ForceUse:
                    case DatAttributes.MultiUse:
                    case DatAttributes.FluidContainer:
                    case DatAttributes.Splash:
                    case DatAttributes.NotWalkable:
                    case DatAttributes.NotMoveable:
                    case DatAttributes.BlockProjectile:
                    case DatAttributes.NotPathable:
                    case DatAttributes.NoMoveAnimation:
                    case DatAttributes.Pickupable:
                    case DatAttributes.Hangable:
                    case DatAttributes.HookSouth:
                    case DatAttributes.HookEast:
                    case DatAttributes.Rotateable:
                    case DatAttributes.DontHide:
                    case DatAttributes.Translucent:
                    case DatAttributes.LyingCorpse:
                    case DatAttributes.AnimateAlways:
                    case DatAttributes.FullGround:
                    case DatAttributes.Look:
                    case DatAttributes.Wrapable:
                    case DatAttributes.Unwrapable:
                    case DatAttributes.TopEffect:
                    case DatAttributes.Usable:
                        type.Attributes[(byte)attr] = true;
                        break;

                    case DatAttributes.Light:
                        type.Attributes[(byte)attr] = new LightInfo() {
                            intensity = binaryReader.GetU16(),
                            color = binaryReader.GetU16()
                        };
                        break;

                    case DatAttributes.Displacement:
                        type.Attributes[(byte)attr] = new Vector2Int {
                            x = binaryReader.GetU16(),
                            y = binaryReader.GetU16()
                        };
                        break;

                    case DatAttributes.Elevation:
                        type.Elevation = binaryReader.GetU16();
                        type.Attributes[(byte)attr] = type.Elevation;
                        break;

                    case DatAttributes.Market:
                        type.Attributes[(byte)attr] = new MarketData() {
                            category = binaryReader.GetU16(),
                            tradeAs = binaryReader.GetU16(),
                            showAs = binaryReader.GetU16(),
                            name = binaryReader.GetString(),
                            restrictVocation = binaryReader.GetU16(),
                            requiredLevel = binaryReader.GetU16(),
                        };
                        break;

                    default:
                        throw new System.Exception("Unhandled DatAttribute [" + attr + "].");
                }
            }

            if (!done) {
                throw new System.Exception("Couldn't parse thing [category: " + category + ", ID: " + id + "].");
            }

            byte groupCount = (category == ThingCategory.Creature) ? binaryReader.GetU8() : (byte)1U;
            for (int i = 0; i < groupCount; i++) {
                FrameGroupType groupType = FrameGroupType.Default;
                if (category == ThingCategory.Creature) {
                    groupType = (FrameGroupType)binaryReader.GetU8();
                }

                type.FrameGroups[groupType] = FrameGroup.Serialize(category, ref binaryReader);
            }

            return type;
        }
    }
}