using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Newtonsoft.Json.Linq;
using OpenTibiaUnity.Protobuf.Appearances001;

namespace OpenTibiaUnity
{
    // This is taken from the the OpenOpenTibiaUnity project
    using DatAttributes = Core.Sprites.DatAttributes;

    static class Program
    {
        public const int SEGMENT_SIZE = 384 * 2;

        static uint referencedSprite = 0;
        static JArray catalogJson = new JArray();

        /// <summary>
        /// Delegate to generate AxA image from n BxB images (i.e 64x64 from 4 32x32)
        /// </summary>
        /// <param name="bitmaps">array of sufficient number of bitmaps</param>
        /// <returns></returns>
        public delegate Bitmap GenerateBitmapDelegate(Bitmap[] bitmaps);

        /// <summary>
        /// Generates protobuf appearance from the legacy thingtype
        /// </summary>
        /// <param name="thingType">thing generated from tibia.dat (old revisions)</param>
        /// <returns></returns>
        static Appearance GenerateAppearance(OpenTibiaUnity.Core.Sprites.ThingType thingType) {
            Appearance appearance = new Appearance() {
                Id = thingType.ID,
            };
            
            if (thingType.Attributes.Count > 0) {
                appearance.Flags = new AppearanceFlags();
            }

            // Flags
            if (thingType.HasAttribute(DatAttributes.Ground)) appearance.Flags.Ground = (ushort)thingType.Attributes[DatAttributes.Ground];
            if (thingType.HasAttribute(DatAttributes.Writable)) appearance.Flags.Writable = (ushort)thingType.Attributes[DatAttributes.Writable];
            if (thingType.HasAttribute(DatAttributes.WritableOnce)) appearance.Flags.WritableOnce = (ushort)thingType.Attributes[DatAttributes.WritableOnce];
            if (thingType.HasAttribute(DatAttributes.MinimapColor)) appearance.Flags.MinimapColor = (ushort)thingType.Attributes[DatAttributes.MinimapColor];
            if (thingType.HasAttribute(DatAttributes.Elevation)) appearance.Flags.Elevation = (ushort)thingType.Attributes[DatAttributes.Elevation];
            if (thingType.HasAttribute(DatAttributes.LensHelp)) appearance.Flags.LensHelp = (ushort)thingType.Attributes[DatAttributes.LensHelp];
            if (thingType.HasAttribute(DatAttributes.Cloth)) appearance.Flags.Cloth = (ushort)thingType.Attributes[DatAttributes.Cloth];
            if (thingType.HasAttribute(DatAttributes.DefaultAction)) appearance.Flags.DefaultAction = (ushort)thingType.Attributes[DatAttributes.DefaultAction];

            if (thingType.HasAttribute(DatAttributes.GroundBorder)) appearance.Flags.GroundBorder = (bool)thingType.Attributes[DatAttributes.GroundBorder];
            if (thingType.HasAttribute(DatAttributes.OnBottom)) appearance.Flags.OnBottom = (bool)thingType.Attributes[DatAttributes.OnBottom];
            if (thingType.HasAttribute(DatAttributes.OnTop)) appearance.Flags.OnTop = (bool)thingType.Attributes[DatAttributes.OnTop];
            if (thingType.HasAttribute(DatAttributes.Container)) appearance.Flags.Container = (bool)thingType.Attributes[DatAttributes.Container];
            if (thingType.HasAttribute(DatAttributes.Stackable)) appearance.Flags.Stackable = (bool)thingType.Attributes[DatAttributes.Stackable];
            if (thingType.HasAttribute(DatAttributes.ForceUse)) appearance.Flags.ForceUse = (bool)thingType.Attributes[DatAttributes.ForceUse];
            if (thingType.HasAttribute(DatAttributes.MultiUse)) appearance.Flags.MultiUse = (bool)thingType.Attributes[DatAttributes.MultiUse];
            if (thingType.HasAttribute(DatAttributes.FluidContainer)) appearance.Flags.FluidContainer = (bool)thingType.Attributes[DatAttributes.FluidContainer];
            if (thingType.HasAttribute(DatAttributes.Splash)) appearance.Flags.Splash = (bool)thingType.Attributes[DatAttributes.Splash];
            if (thingType.HasAttribute(DatAttributes.NotWalkable)) appearance.Flags.NotWalkable = (bool)thingType.Attributes[DatAttributes.NotWalkable];
            if (thingType.HasAttribute(DatAttributes.NotMoveable)) appearance.Flags.NotMoveable = (bool)thingType.Attributes[DatAttributes.NotMoveable];
            if (thingType.HasAttribute(DatAttributes.BlockProjectile)) appearance.Flags.BlockProjectile = (bool)thingType.Attributes[DatAttributes.BlockProjectile];
            if (thingType.HasAttribute(DatAttributes.NotPathable)) appearance.Flags.NotPathable = (bool)thingType.Attributes[DatAttributes.NotPathable];
            if (thingType.HasAttribute(DatAttributes.NoMoveAnimation)) appearance.Flags.NoMoveAnimation = (bool)thingType.Attributes[DatAttributes.NoMoveAnimation];
            if (thingType.HasAttribute(DatAttributes.Pickupable)) appearance.Flags.Pickupable = (bool)thingType.Attributes[DatAttributes.Pickupable];
            if (thingType.HasAttribute(DatAttributes.Hangable)) appearance.Flags.Hangable = (bool)thingType.Attributes[DatAttributes.Hangable];
            if (thingType.HasAttribute(DatAttributes.HookSouth)) appearance.Flags.HookSouth = (bool)thingType.Attributes[DatAttributes.HookSouth];
            if (thingType.HasAttribute(DatAttributes.HookEast)) appearance.Flags.HookEast = (bool)thingType.Attributes[DatAttributes.HookEast];
            if (thingType.HasAttribute(DatAttributes.Rotateable)) appearance.Flags.Rotateable = (bool)thingType.Attributes[DatAttributes.Rotateable];
            if (thingType.HasAttribute(DatAttributes.DontHide)) appearance.Flags.DontHide = (bool)thingType.Attributes[DatAttributes.DontHide];
            if (thingType.HasAttribute(DatAttributes.Translucent)) appearance.Flags.Translucent = (bool)thingType.Attributes[DatAttributes.Translucent];
            if (thingType.HasAttribute(DatAttributes.LyingCorpse)) appearance.Flags.LyingCorpse = (bool)thingType.Attributes[DatAttributes.LyingCorpse];
            if (thingType.HasAttribute(DatAttributes.AnimateAlways)) appearance.Flags.AnimateAlways = (bool)thingType.Attributes[DatAttributes.AnimateAlways];
            if (thingType.HasAttribute(DatAttributes.FullGround)) appearance.Flags.FullGround = (bool)thingType.Attributes[DatAttributes.FullGround];
            if (thingType.HasAttribute(DatAttributes.Look)) appearance.Flags.Look = (bool)thingType.Attributes[DatAttributes.Look];
            if (thingType.HasAttribute(DatAttributes.Wrapable)) appearance.Flags.Wrapable = (bool)thingType.Attributes[DatAttributes.Wrapable];
            if (thingType.HasAttribute(DatAttributes.Unwrapable)) appearance.Flags.GroundBorder = (bool)thingType.Attributes[DatAttributes.Unwrapable];
            if (thingType.HasAttribute(DatAttributes.TopEffect)) appearance.Flags.TopEffect = (bool)thingType.Attributes[DatAttributes.TopEffect];
            if (thingType.HasAttribute(DatAttributes.Usable)) appearance.Flags.Usable = (bool)thingType.Attributes[DatAttributes.Usable];

            if (thingType.HasAttribute(DatAttributes.Light)) {
                var lightInfo = (OpenTibiaUnity.Core.Sprites.LightInfo)thingType.Attributes[DatAttributes.Light];

                appearance.Flags.Light = new LightInfo() {
                    Intensity = lightInfo.intensity,
                    Color = lightInfo.color,
                };
            }

            if (thingType.HasAttribute(DatAttributes.Displacement)) {
                var displacement = (OpenTibiaUnity.Core.Sprites.Vector2Int)thingType.Attributes[DatAttributes.Displacement];
                appearance.Flags.Displacement = new Vector2() {
                    X = (uint)displacement.x,
                    Y = (uint)displacement.y,
                };
            }

            if (thingType.HasAttribute(DatAttributes.Market)) {
                var Market = (OpenTibiaUnity.Core.Sprites.MarketData)thingType.Attributes[DatAttributes.Market];

                appearance.Flags.Market = new MarketInfo() {
                    Category = (uint)Market.category,
                    TradeAs = Market.tradeAs,
                    ShowAs = Market.showAs,
                    Name = Market.name,
                    RestrictVocation = Market.restrictVocation,
                    RequiredLevel = Market.requiredLevel,
                };
            }

            foreach (var f in thingType.FrameGroups) {
                FrameGroup frameGroup = new FrameGroup();

                frameGroup.Type = f.Key == 0 ? FrameGroupType.Idle : FrameGroupType.Walking;
                frameGroup.Height = f.Value.Height;
                frameGroup.Width = f.Value.Width;
                frameGroup.ExactSize = f.Value.ExactSize;
                frameGroup.Layers = f.Value.Layers;
                frameGroup.PatternWidth = f.Value.PatternWidth;
                frameGroup.PatternHeight = f.Value.PatternHeight;
                frameGroup.PatternDepth = f.Value.PatternDepth;
                frameGroup.Phases = f.Value.Phases;

                if (f.Value.Animator != null) {
                    frameGroup.FrameAnimation = new FrameAnimation();
                    frameGroup.FrameAnimation.Async = f.Value.Animator.Async;
                    frameGroup.FrameAnimation.LoopCount = f.Value.Animator.LoopCount;
                    frameGroup.FrameAnimation.StartPhase = f.Value.Animator.StartPhase;

                    foreach (var m in f.Value.Animator.FrameGroupDurations) {
                        FrameGroupDuration duration = new FrameGroupDuration();
                        duration.Min = (uint)m.Minimum;
                        duration.Max = (uint)m.Maximum;

                        frameGroup.FrameAnimation.FrameGroupDurations.Add(duration);
                    }
                }

                foreach (var s in f.Value.Sprites) {
                    frameGroup.Sprites.Add(s);
                }
                
                appearance.FrameGroups.Add(frameGroup);
            }

            return appearance;
        }

        /// <summary>
        /// Loads tibia.dat and generates new a list of appearances
        /// </summary>
        /// <returns></returns>
        static Appearances GenerateAppearances001() {
            try {
                var bytes = File.ReadAllBytes("tibia.dat");
                OpenTibiaUnity.Core.Sprites.ContentData parser = new OpenTibiaUnity.Core.Sprites.ContentData(bytes);
                parser.Parse();

                Appearances appearances0001 = new Appearances();

                int index = 0;
                foreach (var thingsDict in parser.ThingTypes) {
                    foreach (var thingIt in thingsDict) {
                        var a = GenerateAppearance(thingIt.Value);

                        if (index == 0) {
                            appearances0001.Objects.Add(a);
                        } else if (index == 1) {
                            appearances0001.Outfits.Add(a);
                        } else if (index == 2) {
                            appearances0001.Effects.Add(a);
                        } else if (index == 3) {
                            appearances0001.Missles.Add(a);
                        }
                    }

                    index++;
                }

                JObject obj = new JObject();
                obj.Add("type", "appearances");
                obj.Add("file", "appearances001.dat");

                catalogJson.Add(obj);

                return appearances0001;
            } catch (System.Exception e) {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            return null;
        }
        
        static Bitmap GenerateBitmap64x64From4_32x32(Bitmap[] bitmapArray) {
            Bitmap bitmap = new Bitmap(64, 64);
            /*
             * Topleft: 4
             * TopRight: 3
             * BottomLeft: 2
             * BottomRight: 1
            */
            
            using (Graphics gfx = Graphics.FromImage(bitmap)) {
                if (bitmapArray[3] != null) gfx.DrawImage(bitmapArray[3], 0, 0, 32, 32);
                if (bitmapArray[2] != null) gfx.DrawImage(bitmapArray[2], 32, 0, 32, 32);
                if (bitmapArray[1] != null) gfx.DrawImage(bitmapArray[1], 0, 32, 32, 32);
                if (bitmapArray[0] != null) gfx.DrawImage(bitmapArray[0], 32, 32, 32, 32);
            }

            return bitmap;
        }
        static Bitmap GenerateBitmap64x32From2_32x32(Bitmap[] bitmapArray) {
            Bitmap bitmap = new Bitmap(64, 32);
            /*
             * Left: 2
             * Right: 1
            */

            using (Graphics gfx = Graphics.FromImage(bitmap)) {
                if (bitmapArray[1] != null) gfx.DrawImage(bitmapArray[1], 0, 0, 32, 32);
                if (bitmapArray[0] != null) gfx.DrawImage(bitmapArray[0], 32, 0, 32, 32);
            }

            return bitmap;
        }
        static Bitmap GenerateBitmap32x64From2_32x32(Bitmap[] bitmapArray) {
            Bitmap bitmap = new Bitmap(32, 64);
            /*
             * Top: 2
             * Bottom: 1
            */

            using (Graphics gfx = Graphics.FromImage(bitmap)) {
                if (bitmapArray[1] != null) gfx.DrawImage(bitmapArray[1], 0, 0, 32, 32);
                if (bitmapArray[0] != null) gfx.DrawImage(bitmapArray[0], 0, 32, 32, 32);
            }

            return bitmap;
        }

        static void SaveStaticBitmaps(RepeatedField<uint> sprites, ref int start, Core.Sprites.ContentSprites sprParser, int dX, int dY) {
            GenerateBitmapDelegate gen;
            int layer = 0;
            int spritetype = 1;
            if (dX == 32 && dY == 32) {
                gen = delegate (Bitmap[] f) { return f[0] != null ? f[0] : new Bitmap(32, 32); };
                layer = 1;
            } else if (dX == 32 && dY == 64) {
                gen = GenerateBitmap32x64From2_32x32;
                layer = 2;
                spritetype = 2;
            } else if (dX == 64 && dY == 32) {
                gen = GenerateBitmap64x32From2_32x32;
                layer = 2;
                spritetype = 3;
            } else {
                gen = GenerateBitmap64x64From4_32x32;
                layer = 4;
                spritetype = 4;
            }
            
            int totalSize = SEGMENT_SIZE * SEGMENT_SIZE;
            int singleSize = dX * dY;

            int amountInBitmap = totalSize / (32 * 32); // Any sprite is 32x32, so the total bitmaps should rely on that 
            int totalBitmaps = (int)Math.Ceiling((double)sprites.Count / amountInBitmap);
            if (totalBitmaps == 0) {
                return;
            }
            
            Bitmap currentBitmap = new Bitmap(SEGMENT_SIZE, SEGMENT_SIZE);
            Graphics gfx = Graphics.FromImage(currentBitmap);

            int x = 0, y = 0, z = 0;
            for (int i = 0; i < sprites.Count;) {
                Bitmap[] malform_bitmaps = new Bitmap[layer];
                for (int m = 0; m < layer; m++) {
                    if (i + m >= sprites.Count) {
                        break;
                    }

                    malform_bitmaps[m] = sprParser.GetSprite(sprites[i + m]);
                }

                Bitmap generated = gen(malform_bitmaps);
                if (y >= SEGMENT_SIZE) {
                    string filename = string.Format("sprites/sprites-{0}-{1}.png", start, start + (totalSize / singleSize) - 1);
                    
                    currentBitmap.Save(filename);
                    currentBitmap.Dispose();
                    gfx.Dispose();

                    JObject obj = new JObject();
                    obj.Add("type", "sprite");
                    obj.Add("file", filename);
                    obj.Add("spritetype", spritetype);
                    obj.Add("firstspriteid", start);
                    obj.Add("lastspriteid", start + (totalSize / singleSize) - 1);

                    catalogJson.Add(obj);
                    
                    start += (totalSize / singleSize);

                    currentBitmap = new Bitmap(SEGMENT_SIZE, SEGMENT_SIZE);
                    gfx = Graphics.FromImage(currentBitmap);
                    x = y = z = 0;
                }

                try {
                    gfx.DrawImage(generated, x, y, dX, dY);
                    generated.Dispose();
                } catch (Exception) {
                }

                x += dX;
                if (x >= SEGMENT_SIZE) {
                    y += dY;
                    x = 0;
                }
                
                if (i == sprites.Count) {
                    break;
                }

                i = Math.Min(i + layer, sprites.Count);
                z++;
            }

            if (currentBitmap != null) {
                int end = start + z;
                string filename = string.Format("sprites/sprites-{0}-{1}.png", start, end - 1);

                currentBitmap.Save(filename);
                currentBitmap.Dispose();
                gfx.Dispose();

                JObject obj = new JObject();
                obj.Add("type", "sprite");
                obj.Add("file", filename);
                obj.Add("spritetype", spritetype);
                obj.Add("firstspriteid", start);
                obj.Add("lastspriteid", end - 1);
                
                start = end;
                catalogJson.Add(obj);
            }

            GC.Collect();
        }

        static void DeployNewSprites(uint id, FrameGroup frameGroup, int layer) {
            var m = frameGroup.Sprites;
            var s = new RepeatedField<uint>();

            int totalNew = (int)Math.Ceiling((double)m.Count / layer);
            for (int i = 0; i < totalNew; i++) {
                s.Add(referencedSprite++);
            }

            frameGroup.Sprites.Clear();
            frameGroup.Sprites.AddRange(s);
        }

        static void DeploySprites(RepeatedField<Appearance> appearances, ref RepeatedField<uint>[] sortedFrameGroups) {
            List<FrameGroup>[] frameGroups = new List<FrameGroup>[4];
            frameGroups[0] = new List<FrameGroup>();
            frameGroups[1] = new List<FrameGroup>();
            frameGroups[2] = new List<FrameGroup>();
            frameGroups[3] = new List<FrameGroup>();

            List<uint>[] ids = new List<uint>[4];
            ids[0] = new List<uint>();
            ids[1] = new List<uint>();
            ids[2] = new List<uint>();
            ids[3] = new List<uint>();

            foreach (var appearance in appearances) {
                foreach (FrameGroup frameGroup in appearance.FrameGroups) {
                    if (frameGroup.Width == 1) {
                        if (frameGroup.Height == 1) {
                            sortedFrameGroups[0].AddRange(frameGroup.Sprites);
                            frameGroups[0].Add(frameGroup);
                            ids[0].Add(appearance.Id);
                        } else if (frameGroup.Height == 2) {
                            sortedFrameGroups[1].AddRange(frameGroup.Sprites);
                            frameGroups[1].Add(frameGroup);
                            ids[1].Add(appearance.Id);
                        } else {
                            //string.Format("Apperance ID: " + appearance.Id + ", Unknown height: " + frameGroup.Height);
                        }
                    } else if (frameGroup.Width == 2) {
                        if (frameGroup.Height == 1) {
                            sortedFrameGroups[2].AddRange(frameGroup.Sprites);
                            frameGroups[2].Add(frameGroup);
                            ids[2].Add(appearance.Id);
                        } else if (frameGroup.Height == 2) {
                            sortedFrameGroups[3].AddRange(frameGroup.Sprites);
                            frameGroups[3].Add(frameGroup);
                            ids[3].Add(appearance.Id);
                        } else {
                            //string.Format("Apperance ID: " + appearance.Id + ", Unknown height: " + frameGroup.Height);
                        }
                    } else {
                        //string.Format("Apperance ID: " + appearance.Id + ", Unknown Width: " + frameGroup.Width);
                    }
                }
            }

            int h = 0;
            foreach (var a in frameGroups[0]) DeployNewSprites(ids[0][h++], a, 1); h = 0;
            foreach (var a in frameGroups[1]) DeployNewSprites(ids[1][h++], a, 2); h = 0;
            foreach (var a in frameGroups[2]) DeployNewSprites(ids[2][h++], a, 2); h = 0;
            foreach (var a in frameGroups[3]) DeployNewSprites(ids[3][h++], a, 4);
        }

        static void GenerateEverything() {
            // Generating New appearances
            Appearances appearances0001 = GenerateAppearances001();

            // Creating the sprites folder to save files in.
            Directory.CreateDirectory("sprites");

            // Loading tibia.spr into chunks
            Core.Sprites.ContentSprites sprParser;
            try {
                var bytes = File.ReadAllBytes("tibia.spr");
                sprParser = new Core.Sprites.ContentSprites(bytes);
                sprParser.Parse();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
                return;
            }

            int start = 0;
            RepeatedField<uint>[] sortedFrameGroups = new RepeatedField<uint>[4];

            // grouping different sizes into different textures
            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances0001.Outfits, ref sortedFrameGroups);
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64);

            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances0001.Effects, ref sortedFrameGroups);
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64);

            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances0001.Missles, ref sortedFrameGroups);
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64);

            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances0001.Objects, ref sortedFrameGroups);
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64);

            // saving appearances.dat (with the respective version)
            using (FileStream file1 = File.Create("appearances001.dat")) {
                appearances0001.WriteTo(file1);
            }

            // This is a helper, by using some sort of data structure (i.e interval tree)
            // you can load any sprite quickly at runtime
            using (FileStream file2 = File.Create("catalog-content.json")) {
                string str = catalogJson.ToString();
                file2.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
            }
        }

        static void Main(string[] args) {
            try {
                GenerateEverything();
            } catch (OutOfMemoryException e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
