using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTibiaUnity.Core.Metaflags;
using OpenTibiaUnity.Protobuf.Appearances;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OpenTibiaUnity.Core.Converter
{
    // TODO, refactor this whole thing as it was done initially just
    // to fit the purpose and didn't gave much care about style
    public class LegacyConverter : IConverter
    {
        public delegate void DrawBitmapsDelegate(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0);

        private struct FrameGroupDetail
        {
            public int Width;
            public int Height;

            public FrameGroupDetail(int width, int height) {
                Width = width;
                Height = height;
            }
        }

        private int m_ClientVersion;
        private uint m_ReferencedSpriteID = 0;
        private uint m_ReferenceFrameGroupID = 0;
        private bool m_UseAlpha;
        private Dictionary<FrameGroup, FrameGroupDetail> m_FrameGroupDetails = new Dictionary<FrameGroup, FrameGroupDetail>();
        private List<SpriteTypeImpl> m_SpriteSheet = new List<SpriteTypeImpl>();
        private List<Task> m_Tasks = new List<Task>();
        private LimitedConcurrencyLevelTaskScheduler m_LCTS;
        private TaskFactory m_TaskFactory;

        public LegacyConverter(int clientVersion, bool useAlpha, int maxThreads = 4) {
            m_ClientVersion = clientVersion;
            m_UseAlpha = useAlpha;
            m_LCTS = new LimitedConcurrencyLevelTaskScheduler(maxThreads);
            m_TaskFactory = new TaskFactory(m_LCTS);
        }

        AppearanceFlags GenerateAppearanceFlags(Assets.ThingType thingType, Appearance appearance) {
            if (thingType.Attributes.Count == 0)
                return null;

            var appearanceFlags = new AppearanceFlags();

            if (thingType.HasAttribute(AttributesUniform.Ground)) appearanceFlags.Ground = new AppearanceFlagGround() { Speed = (ushort)thingType.Attributes[AttributesUniform.Ground] };
            if (thingType.HasAttribute(AttributesUniform.Writable)) appearanceFlags.Writable = new AppearanceFlagWritable() { MaxTextLength = (ushort)thingType.Attributes[AttributesUniform.Writable] };
            if (thingType.HasAttribute(AttributesUniform.WritableOnce)) appearanceFlags.WritableOnce = new AppearanceFlagWritableOnce() { MaxTextLengthOnce = (ushort)thingType.Attributes[AttributesUniform.WritableOnce] };
            if (thingType.HasAttribute(AttributesUniform.MinimapColor)) appearanceFlags.Automap = new AppearanceFlagAutomap() { Color = (ushort)thingType.Attributes[AttributesUniform.MinimapColor] };
            if (thingType.HasAttribute(AttributesUniform.Elevation)) appearanceFlags.Height = new AppearanceFlagHeight() { Elevation = (ushort)thingType.Attributes[AttributesUniform.Elevation] };
            if (thingType.HasAttribute(AttributesUniform.LensHelp)) appearanceFlags.LensHelp = new AppearanceFlagLensHelp() { ID = (ushort)thingType.Attributes[AttributesUniform.LensHelp] };
            if (thingType.HasAttribute(AttributesUniform.Cloth)) appearanceFlags.Clothes = new AppearanceFlagClothes() { Slot = (ushort)thingType.Attributes[AttributesUniform.Cloth] };

            // default action
            if (thingType.HasAttribute(AttributesUniform.DefaultAction)) {
                var defaultAction = new AppearanceFlagDefaultAction();
                var oldDefaultActionValue = (ushort)thingType.Attributes[AttributesUniform.DefaultAction];
                if (oldDefaultActionValue > 4)
                    Console.WriteLine("Invalid default action: " + oldDefaultActionValue + " for item id: " + thingType.ID);
                appearanceFlags.DefaultAction = new AppearanceFlagDefaultAction() { Action = (Protobuf.Shared.PlayerAction)oldDefaultActionValue };
            }

            if (thingType.HasAttribute(AttributesUniform.GroundBorder)) appearanceFlags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.Bottom)) appearanceFlags.Bottom = true;
            if (thingType.HasAttribute(AttributesUniform.Top)) appearanceFlags.Top = true;
            if (thingType.HasAttribute(AttributesUniform.Container)) appearanceFlags.Container = true;
            if (thingType.HasAttribute(AttributesUniform.Stackable)) appearanceFlags.Stackable = true;
            if (thingType.HasAttribute(AttributesUniform.Use)) appearanceFlags.Use = true;
            if (thingType.HasAttribute(AttributesUniform.ForceUse)) appearanceFlags.ForceUse = true;
            if (thingType.HasAttribute(AttributesUniform.MultiUse)) appearanceFlags.MultiUse = true;
            if (thingType.HasAttribute(AttributesUniform.FluidContainer)) appearanceFlags.FluidContainer = true;
            if (thingType.HasAttribute(AttributesUniform.Splash)) appearanceFlags.Splash = true;
            if (thingType.HasAttribute(AttributesUniform.Unpassable)) appearanceFlags.Unpassable = true;
            if (thingType.HasAttribute(AttributesUniform.Unmoveable)) appearanceFlags.Unmoveable = true;
            if (thingType.HasAttribute(AttributesUniform.Unsight)) appearanceFlags.Unsight = true;
            if (thingType.HasAttribute(AttributesUniform.BlockPath)) appearanceFlags.BlockPath = true;
            if (thingType.HasAttribute(AttributesUniform.NoMoveAnimation)) appearanceFlags.NoMoveAnimation = true;
            if (thingType.HasAttribute(AttributesUniform.Pickupable)) appearanceFlags.Pickupable = true;
            if (thingType.HasAttribute(AttributesUniform.Hangable)) appearanceFlags.Hangable = true;

            // can have only one hook //
            if (thingType.HasAttribute(AttributesUniform.HookSouth)) appearanceFlags.Hook = new AppearanceFlagHook() { Type = Protobuf.Shared.HookType.South };
            else if (thingType.HasAttribute(AttributesUniform.HookEast)) appearanceFlags.Hook = new AppearanceFlagHook() { Type = Protobuf.Shared.HookType.East };

            if (thingType.HasAttribute(AttributesUniform.Rotateable)) appearanceFlags.Rotateable = true;
            if (thingType.HasAttribute(AttributesUniform.DontHide)) appearanceFlags.DontHide = true;
            if (thingType.HasAttribute(AttributesUniform.Translucent)) appearanceFlags.Translucent = true;
            if (thingType.HasAttribute(AttributesUniform.LyingCorpse)) appearanceFlags.LyingCorpse = true;
            if (thingType.HasAttribute(AttributesUniform.AnimateAlways)) appearanceFlags.AnimateAlways = true;
            if (thingType.HasAttribute(AttributesUniform.FullGround)) appearanceFlags.FullGround = true;
            if (thingType.HasAttribute(AttributesUniform.Look)) appearanceFlags.IgnoreLook = true;
            if (thingType.HasAttribute(AttributesUniform.Wrapable)) appearanceFlags.Wrapable = true;
            if (thingType.HasAttribute(AttributesUniform.Unwrapable)) appearanceFlags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.TopEffect)) appearanceFlags.TopEffect = true;

            if (thingType.HasAttribute(AttributesUniform.Light)) {
                var lightInfo = (Assets.Light)thingType.Attributes[AttributesUniform.Light];

                appearanceFlags.Light = new AppearanceFlagLight() {
                    Intensity = lightInfo.intensity,
                    Color = lightInfo.color,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Offset)) {
                var displacement = (Assets.Vector2Int)thingType.Attributes[AttributesUniform.Offset];
                appearanceFlags.Offset = new AppearanceFlagOffset() {
                    X = displacement.x,
                    Y = displacement.y,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Market)) {
                var market = (Assets.MarketData)thingType.Attributes[AttributesUniform.Market];

                appearanceFlags.Market = new AppearanceFlagMarket() {
                    Category = (Protobuf.Shared.ItemCategory)market.category,
                    TradeAsObjectID = market.tradeAs,
                    ShowAsObjectID = market.showAs,
                    MinimumLevel = market.restrictLevel,
                };

                appearanceFlags.Market.RestrictToProfession.Add((Protobuf.Shared.PlayerProfession)market.restrictProfession);
                appearance.Name = market.name;
            }

            return appearanceFlags;
        }

        /// <summary>
        /// Generates protobuf Appearance from assets ThingType
        /// </summary>
        /// <param name="thingType">thing generated from tibia.dat (old revisions)</param>
        /// <returns></returns>
        Appearance GenerateAppearance(Assets.ThingType thingType) {
            var appearance = new Appearance();
            appearance.ID = thingType.ID;
            appearance.Flags = GenerateAppearanceFlags(thingType, appearance);
            
            foreach (var pair in thingType.FrameGroups) {
                var frameGroupType = pair.Key;
                var legacyFrameGroup = pair.Value;

                var frameGroup = new FrameGroup();
                var spriteInfo = new SpriteInfo();
                frameGroup.Type = pair.Key == 0 ? Protobuf.Shared.FrameGroupType.Idle : Protobuf.Shared.FrameGroupType.Walking;
                frameGroup.ID = m_ReferenceFrameGroupID++;
                frameGroup.SpriteInfo = spriteInfo;

                spriteInfo.PatternWidth = legacyFrameGroup.PatternWidth;
                spriteInfo.PatternHeight = legacyFrameGroup.PatternHeight;
                spriteInfo.PatternDepth = legacyFrameGroup.PatternDepth;
                spriteInfo.Layers = legacyFrameGroup.Layers;
                spriteInfo.Phases = legacyFrameGroup.Phases;
                spriteInfo.BoundingSquare = legacyFrameGroup.ExactSize;

                if (legacyFrameGroup.Animator != null) {
                    var animation = new SpriteAnimation();
                    spriteInfo.Animation = animation;
                    animation.DefaultStartPhase = (uint)legacyFrameGroup.Animator.StartPhase;
                    animation.Synchronized = !legacyFrameGroup.Animator.Async;
                    //animation.RandomStartPhase = false;

                    if (legacyFrameGroup.Animator.LoopCount < 0) {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.PingPong;
                    } else if (legacyFrameGroup.Animator.LoopCount == 0) {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.Infinite;
                    } else {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.Counted;
                        animation.LoopCount = (uint)legacyFrameGroup.Animator.LoopCount;
                    }

                    //animation.IsOpaque = false;

                    foreach (var m in legacyFrameGroup.Animator.FrameGroupDurations) {
                        var spritePhase = new SpritePhase();
                        spritePhase.DurationMin = (uint)m.Minimum;
                        spritePhase.DurationMax = (uint)m.Maximum;

                        animation.SpritePhases.Add(spritePhase);
                    }
                }

                foreach (var spriteID in legacyFrameGroup.Sprites)
                    spriteInfo.SpriteIDs.Add(spriteID);

                m_FrameGroupDetails.Add(frameGroup, new FrameGroupDetail(legacyFrameGroup.Width, legacyFrameGroup.Height));
                appearance.FrameGroups.Add(frameGroup);
            }

            return appearance;
        }

        Appearances GenerateAppearances() {
            try {
                var rawContentDat = File.ReadAllBytes(Path.Combine(m_ClientVersion.ToString(), "Tibia.dat"));
                var contentData = new Assets.ContentData(rawContentDat, m_ClientVersion);

                var appearances = new Appearances();
                for (int i = 0; i < contentData.ThingTypeDictionaries.Length; i++) {
                    var dict = contentData.ThingTypeDictionaries[i];
                    foreach (var pair in dict) {
                        var appearance = GenerateAppearance(pair.Value);
                        switch (i) {
                            case 0: appearances.Objects.Add(appearance); break;
                            case 1: appearances.Outfits.Add(appearance); break;
                            case 2: appearances.Effects.Add(appearance); break;
                            case 3: appearances.Missles.Add(appearance); break;
                        }
                    }
                }

                return appearances;
            } catch (Exception e) {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                Environment.Exit(0);
            }

            return null;
        }

        public async Task<bool> BeginProcessing() {
            string datFile = Path.Combine(m_ClientVersion.ToString(), "Tibia.dat");
            string sprFile = Path.Combine(m_ClientVersion.ToString(), "Tibia.spr");
            if (!File.Exists(datFile) || !File.Exists(sprFile)) {
                Console.WriteLine("Tibia.dat or Tibia.spr doesn't exist");
                return false;
            }

            Console.Write("Processing Appearances...");
            var appearances = GenerateAppearances();
            Console.WriteLine("\rProcessing Appearances: Done!");

            // loading tibia.spr into chunks
            Assets.ContentSprites contentSprites;
            try {
                var rawContentSprites = File.ReadAllBytes(sprFile);
                contentSprites = new Assets.ContentSprites(rawContentSprites, m_ClientVersion, m_UseAlpha);
            } catch (Exception e) {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                return false;
            }

            string resultPath = Path.Combine(m_ClientVersion.ToString(), "result");

            Console.Write("Processing Spritesheets...");
            Directory.CreateDirectory(Path.Combine(resultPath, "sprites"));

            int start = 0;
            SaveSprites(appearances.Outfits, ref start, contentSprites);
            SaveSprites(appearances.Effects, ref start, contentSprites);
            SaveSprites(appearances.Missles, ref start, contentSprites);
            SaveSprites(appearances.Objects, ref start, contentSprites);
            
            await Task.WhenAll(m_Tasks.ToArray());
            Console.WriteLine("\rProcessing Spritesheets: Done!");

            // saving appearances.dat (with the respective version)
            using (var stream = File.Create(Path.Combine(resultPath, "appearances.otud"))) {
                appearances.WriteTo(stream);
            }

            // save spritesheets
            using (var spriteStream = new FileStream(Path.Combine(resultPath, "assets.otus"), FileMode.Create))
            using (var binaryWriter = new BinaryWriter(spriteStream)) {
                m_SpriteSheet.Sort((a, b) => {
                    return a.FirstSpriteID.CompareTo(b.FirstSpriteID);
                });

                binaryWriter.Write((uint)m_SpriteSheet.Count);
                uint index = 0;
                foreach (var spriteType in m_SpriteSheet) {
                    spriteType.AtlasID = index++;

                    var buffer = File.ReadAllBytes(Path.Combine(resultPath, "sprites", spriteType.File));
                    binaryWriter.Write(spriteType.AtlasID);
                    binaryWriter.Write((ushort)spriteType.SpriteType);
                    binaryWriter.Write(spriteType.FirstSpriteID);
                    binaryWriter.Write(spriteType.LastSpriteID);

                    binaryWriter.Write((uint)buffer.Length);
                    binaryWriter.Write(buffer);
                }
            }

            Directory.Delete(Path.Combine(resultPath, "sprites"), true);
            return true;
        }

        public List<SpriteTypeImpl> GetSpriteSheet() {
            return m_SpriteSheet;
        }

        static void DrawBitmap32x32From1_32x32(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0) {
            /*
             * Fill: 1
            */

            if (bitmaps[0] != null) gfx.DrawImage(bitmaps[0], x, y, 32, 32);
        }
        static void DrawBitmap64x32From2_32x32(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0) {
            /*
             * Left: 2
             * Right: 1
            */

            if (bitmaps[1] != null) gfx.DrawImage(bitmaps[1], x, y, 32, 32);
            if (bitmaps[0] != null) gfx.DrawImage(bitmaps[0], x + 32, y, 32, 32);
        }
        static void DrawBitmap32x64From2_32x32(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0) {
            /*
             * Top: 2
             * Bottom: 1
            */

            if (bitmaps[1] != null) gfx.DrawImage(bitmaps[1], x, y, 32, 32);
            if (bitmaps[0] != null) gfx.DrawImage(bitmaps[0], x, y + 32, 32, 32);
        }
        static void DrawBitmap64x64From4_32x32(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0) {
            /*
             * Topleft: 4
             * TopRight: 3
             * BottomLeft: 2
             * BottomRight: 1
            */

            if (bitmaps[3] != null) gfx.DrawImage(bitmaps[3], x, y, 32, 32);
            if (bitmaps[2] != null) gfx.DrawImage(bitmaps[2], x + 32, y, 32, 32);
            if (bitmaps[1] != null) gfx.DrawImage(bitmaps[1], x, y + 32, 32, 32);
            if (bitmaps[0] != null) gfx.DrawImage(bitmaps[0], x + 32, y + 32, 32, 32);
        }

        private void InternalSaveStaticBitmaps(RepeatedField<uint> sprites, DrawBitmapsDelegate drawFunc, int parts, int spriteType, int localStart, Assets.ContentSprites sprParser, int width, int height) {
            int singleSize = width * height;

            AsyncGraphics gfx = new AsyncGraphics(new Bitmap(Program.SEGMENT_DIMENTION, Program.SEGMENT_DIMENTION));
            string filename;

            int x = 0, y = 0, z = 0;
            for (int i = 0; i < sprites.Count;) {
                Bitmap[] bitmapParts = new Bitmap[parts];
                for (int m = 0; m < parts; m++) {
                    if (i + m >= sprites.Count)
                        break;

                    bitmapParts[m] = sprParser.GetSprite(sprites[i + m]);
                }

                if (y >= Program.SEGMENT_DIMENTION) {
                    filename = string.Format("sprites-{0}-{1}.png", localStart, localStart + (Program.BITMAP_SIZE / singleSize) - 1);
                    m_Tasks.Add(gfx.SaveAndDispose(Path.Combine(m_ClientVersion.ToString(), "result", "sprites", filename)));

                    m_SpriteSheet.Add(new SpriteTypeImpl() {
                        File = filename,
                        SpriteType = spriteType,
                        FirstSpriteID = (uint)localStart,
                        LastSpriteID = (uint)(localStart + (Program.BITMAP_SIZE / singleSize) - 1)
                    });

                    localStart += Program.BITMAP_SIZE / singleSize;

                    gfx = new AsyncGraphics(new Bitmap(Program.SEGMENT_DIMENTION, Program.SEGMENT_DIMENTION));
                    x = y = z = 0;
                }

                var tmpSmallBitmaps = bitmapParts;
                drawFunc(gfx, bitmapParts, x, y);
                m_Tasks.Add(gfx.DisposeOnDone(bitmapParts));

                x += width;
                if (x >= Program.SEGMENT_DIMENTION) {
                    y += height;
                    x = 0;
                }

                if (i == sprites.Count)
                    break;

                i = Math.Min(i + parts, sprites.Count);
                z++;
            }

            // save the last gfx
            int end = localStart + z;
            filename = string.Format("sprites-{0}-{1}.png", localStart, end - 1);
            m_Tasks.Add(gfx.SaveAndDispose(Path.Combine(m_ClientVersion.ToString(), "result", "sprites", filename)));

            m_SpriteSheet.Add(new SpriteTypeImpl() {
                File = filename,
                SpriteType = spriteType,
                FirstSpriteID = (uint)localStart,
                LastSpriteID = (uint)(end - 1)
            });
        }

        private void SaveStaticBitmaps(RepeatedField<uint> sprites, ref int start, Assets.ContentSprites sprParser, int width, int height) {
            DrawBitmapsDelegate drawFunc;
            int parts = 0;
            int spritetype = 1;
            if (width == 32 && height == 32) {
                drawFunc = DrawBitmap32x32From1_32x32;
                parts = 1;
            } else if (width == 32 && height == 64) {
                drawFunc = DrawBitmap32x64From2_32x32;
                parts = 2;
                spritetype = 2;
            } else if (width == 64 && height == 32) {
                drawFunc = DrawBitmap64x32From2_32x32;
                parts = 2;
                spritetype = 3;
            } else {
                drawFunc = DrawBitmap64x64From4_32x32;
                parts = 4;
                spritetype = 4;
            }

            int amountInBitmap = Program.BITMAP_SIZE / (32 * 32);
            int totalBitmaps = (int)Math.Ceiling((double)sprites.Count / amountInBitmap);
            if (totalBitmaps == 0)
                return;

            int localStart = start;
            start += sprites.Count / parts;
            
            m_Tasks.Add(m_TaskFactory.StartNew(() => InternalSaveStaticBitmaps(sprites, drawFunc, parts, spritetype, localStart, sprParser, width, height)));
        }

        private void SaveSprites(RepeatedField<Appearance> appearances, ref int start, Assets.ContentSprites sprParser) {
            RepeatedField<uint>[] sprites = new RepeatedField<uint>[4];
            for (int i = 0; i < 4; i++) sprites[i] = new RepeatedField<uint>();
            DeploySprites(appearances, sprites);

            SaveStaticBitmaps(sprites[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sprites[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sprites[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sprites[3], ref start, sprParser, 64, 64);
        }

        private void DeploySprites(RepeatedField<Appearance> appearances, RepeatedField<uint>[] sprites) {
            var frameGroupsArray = new List<FrameGroup>[4];
            for (int i = 0; i < 4; i++)
                frameGroupsArray[i] = new List<FrameGroup>();

            foreach (var appearance in appearances) {
                foreach (var frameGroup in appearance.FrameGroups) {
                    if (m_FrameGroupDetails.TryGetValue(frameGroup, out var detail)) {
                        int type = -1;
                        if (detail.Width == 1) {
                            if (detail.Height == 1)
                                type = 0;
                            else if (detail.Height == 2)
                                type = 1;
                        } else if (detail.Width == 2) {
                            if (detail.Height == 1)
                                type = 2;
                            else if (detail.Height == 2)
                                type = 3;
                        }

                        if (type >= 0) {
                            sprites[type].AddRange(frameGroup.SpriteInfo.SpriteIDs);
                            frameGroupsArray[type].Add(frameGroup);
                        } else {
                            Console.WriteLine(string.Format("Invalid width or height, currently there is maximum support for 64x64 sprites ({0}, {1})", detail.Width, detail.Height));
                        }
                    }
                }
            }

            for (int i = 0; i < 4; i++) {
                int parts = 1; // by default, each sprite represents 32x32 which is one part
                if (i == 1 || i == 2) // some sprites are 64x32 or 32x64 which is represented by 2 parts
                    parts = 2;
                else if (i == 3) // 64x64 sprites are represented by 4 parts
                    parts = 4;

                var frameGroups = frameGroupsArray[i];
                for (int j = 0; j < frameGroups.Count; j++)
                    ChangeSpriteIDs(frameGroups[j], parts);
            }
        }

        private void ChangeSpriteIDs(FrameGroup frameGroup, int parts) {
            /**
             * initialy, we save the sprite ids as per legacy sprites (only supports 32x32, aka parts)
             * so to correct this, we simply divide the total number of sprites by the number of parts
             * to obtain the sufficient number of required sprites
             */

            var spriteIDs = frameGroup.SpriteInfo.SpriteIDs;
            var newSpriteIDs = new RepeatedField<uint>();

            int total = (int)Math.Ceiling((double)spriteIDs.Count / parts);
            for (int i = 0; i < total; i++)
                newSpriteIDs.Add(m_ReferencedSpriteID++);

            frameGroup.SpriteInfo.SpriteIDs.Clear();
            frameGroup.SpriteInfo.SpriteIDs.AddRange(newSpriteIDs);
        }
    }
}
