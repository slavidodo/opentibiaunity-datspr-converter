using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Newtonsoft.Json.Linq;
using OpenTibiaUnity.Proto.Appearances;

namespace OpenTibiaUnity
{
    // This is taken from the the OpenOpenTibiaUnity project
    using DatAttributes = Core.Sprites.DatAttributes;

    interface ITokenItem
    {
        JObject GetJObject();
    }

    struct AppearancesToken : ITokenItem
    {
        public string file;
        public JObject GetJObject() {
            JObject obj = new JObject();
            obj["type"] = "appearances";
            obj["file"] = file;
            return obj;
        }
    }

    struct SpritesToken : ITokenItem
    {
        public string file;
        public int spritetype;
        public int firstspriteid;
        public int lastspriteid;

        public JObject GetJObject() {
            JObject obj = new JObject();
            obj["type"] = "sprite";
            obj["file"] = file;
            obj["spritetype"] = spritetype;
            obj["firstspriteid"] = firstspriteid;
            obj["lastspriteid"] = lastspriteid;
            return obj;
        }
    }

    static class Program
    {
        public const int SEGMENT_DIMENTION = 512;
        public const int BITMAP_SIZE = SEGMENT_DIMENTION * SEGMENT_DIMENTION;

        static uint referencedSprite = 0;
        static JArray catalogJson = new JArray();
        static List<Task> tasks = new List<Task>();
        static List<ITokenItem> jsonTokens = new List<ITokenItem>();

        /// <summary>
        /// Draw AxA image from n BxB images (i.e 64x64 from 4 32x32)
        /// </summary>
        /// <param name="gfx">async graphics to draw to</param>
        /// <param name="bitmaps">array of sufficient number of bitmaps</param>
        /// <returns></returns>
        public delegate void DrawBitmapsDelegate(AsyncGraphics gfx, Bitmap[] bitmaps, int x = 0, int y = 0);

        /// <summary>
        /// Generates protobuf appearance from the legacy thingtype
        /// </summary>
        /// <param name="thingType">thing generated from tibia.dat (old revisions)</param>
        /// <returns></returns>
        static Appearance GenerateAppearance(Core.Sprites.ThingType thingType) {
            Appearance appearance = new Appearance() {
                Id = thingType.ID,
            };

            if (thingType.Attributes.Count > 0) {
                appearance.Flags = new AppearanceFlags();
            }

            // Flags
            if (thingType.HasAttribute(DatAttributes.Ground)) appearance.Flags.Ground = new Ground() { Speed = (ushort)thingType.Attributes[DatAttributes.Ground] };
            if (thingType.HasAttribute(DatAttributes.Writable)) appearance.Flags.Writable = new Writable() { Length = (ushort)thingType.Attributes[DatAttributes.Writable] };
            if (thingType.HasAttribute(DatAttributes.WritableOnce)) appearance.Flags.WritableOnce = new Writable() { Length = (ushort)thingType.Attributes[DatAttributes.WritableOnce] };
            if (thingType.HasAttribute(DatAttributes.MinimapColor)) appearance.Flags.Minimap = new MiniMap() { Color = (ushort)thingType.Attributes[DatAttributes.MinimapColor] };
            if (thingType.HasAttribute(DatAttributes.Elevation)) appearance.Flags.Elevation = new Elevation() { Elevation_ = (ushort)thingType.Attributes[DatAttributes.Elevation] };
            if (thingType.HasAttribute(DatAttributes.LensHelp)) appearance.Flags.LensHelp = new LensHelp() { Id = (ushort)thingType.Attributes[DatAttributes.LensHelp] };
            if (thingType.HasAttribute(DatAttributes.Cloth)) appearance.Flags.Cloth = new Clothes() { Slot = (ushort)thingType.Attributes[DatAttributes.Cloth] };

            // default action
            if (thingType.HasAttribute(DatAttributes.DefaultAction)) {
                var defaultAction = new DefaultAction();
                var oldDefaultActionValue = (ushort)thingType.Attributes[DatAttributes.DefaultAction];
                if (oldDefaultActionValue > 4)
                    Console.WriteLine("Invalid default action: " + oldDefaultActionValue + " for item id: " + thingType.ID);
                appearance.Flags.DefaultAction = new DefaultAction() { Action = (PlayerAction)oldDefaultActionValue };
            }

            if (thingType.HasAttribute(DatAttributes.GroundBorder)) appearance.Flags.GroundBorder = (bool)thingType.Attributes[DatAttributes.GroundBorder];
            if (thingType.HasAttribute(DatAttributes.OnBottom)) appearance.Flags.Bottom = (bool)thingType.Attributes[DatAttributes.OnBottom];
            if (thingType.HasAttribute(DatAttributes.OnTop)) appearance.Flags.Top = (bool)thingType.Attributes[DatAttributes.OnTop];
            if (thingType.HasAttribute(DatAttributes.Container)) appearance.Flags.Container = (bool)thingType.Attributes[DatAttributes.Container];
            if (thingType.HasAttribute(DatAttributes.Stackable)) appearance.Flags.Stackable = (bool)thingType.Attributes[DatAttributes.Stackable];
            if (thingType.HasAttribute(DatAttributes.Usable)) appearance.Flags.Use = (bool)thingType.Attributes[DatAttributes.Usable];
            if (thingType.HasAttribute(DatAttributes.ForceUse)) appearance.Flags.ForceUse = (bool)thingType.Attributes[DatAttributes.ForceUse];
            if (thingType.HasAttribute(DatAttributes.MultiUse)) appearance.Flags.MultiUse = (bool)thingType.Attributes[DatAttributes.MultiUse];
            if (thingType.HasAttribute(DatAttributes.FluidContainer)) appearance.Flags.FluidContainer = (bool)thingType.Attributes[DatAttributes.FluidContainer];
            if (thingType.HasAttribute(DatAttributes.Splash)) appearance.Flags.Splash = (bool)thingType.Attributes[DatAttributes.Splash];
            if (thingType.HasAttribute(DatAttributes.NotWalkable)) appearance.Flags.Unpassable = (bool)thingType.Attributes[DatAttributes.NotWalkable];
            if (thingType.HasAttribute(DatAttributes.NotMoveable)) appearance.Flags.Unmoveable = (bool)thingType.Attributes[DatAttributes.NotMoveable];
            if (thingType.HasAttribute(DatAttributes.BlockProjectile)) appearance.Flags.Unsight = (bool)thingType.Attributes[DatAttributes.BlockProjectile];
            if (thingType.HasAttribute(DatAttributes.NotPathable)) appearance.Flags.BlockPath = (bool)thingType.Attributes[DatAttributes.NotPathable];
            if (thingType.HasAttribute(DatAttributes.NoMoveAnimation)) appearance.Flags.NoMoveAnimation = (bool)thingType.Attributes[DatAttributes.NoMoveAnimation];
            if (thingType.HasAttribute(DatAttributes.Pickupable)) appearance.Flags.Pickupable = (bool)thingType.Attributes[DatAttributes.Pickupable];
            if (thingType.HasAttribute(DatAttributes.Hangable)) appearance.Flags.Hangable = (bool)thingType.Attributes[DatAttributes.Hangable];

            // can have only one hook //
            if (thingType.HasAttribute(DatAttributes.HookSouth)) appearance.Flags.Hook = new Hook() { Type = HookType.South };
            else if (thingType.HasAttribute(DatAttributes.HookEast)) appearance.Flags.Hook = new Hook() { Type = HookType.East };

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

            if (thingType.HasAttribute(DatAttributes.Light)) {
                var lightInfo = (Core.Sprites.LightInfo)thingType.Attributes[DatAttributes.Light];

                appearance.Flags.Light = new LightInfo() {
                    Intensity = lightInfo.intensity,
                    Color = lightInfo.color,
                };
            }

            if (thingType.HasAttribute(DatAttributes.Displacement)) {
                var displacement = (Core.Sprites.Vector2Int)thingType.Attributes[DatAttributes.Displacement];
                appearance.Flags.Displacement = new Displacement() {
                    X = (uint)displacement.x,
                    Y = (uint)displacement.y,
                };
            }

            if (thingType.HasAttribute(DatAttributes.Market)) {
                var Market = (Core.Sprites.MarketData)thingType.Attributes[DatAttributes.Market];

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
        /// <param name="datFile">the attributes file (tibia.dat)</param>
        /// <param name="version">the client version of this dat</param>
        /// <returns></returns>
        static Appearances GenerateAppearances(string datFile, int version) {
            try {
                Core.Sprites.ContentData datParser = new Core.Sprites.ContentData(File.ReadAllBytes(datFile), version);
                datParser.Parse();

                Appearances appearances = new Appearances();
                for (int i = 0; i < datParser.ThingTypeDictionaries.Length; i++) {
                    var dict = datParser.ThingTypeDictionaries[i];
                    foreach (var pair in dict) {
                        Appearance appearance = GenerateAppearance(pair.Value);
                        switch (i) {
                            case 0: appearances.Objects.Add(appearance); break;
                            case 1: appearances.Outfits.Add(appearance); break;
                            case 2: appearances.Effects.Add(appearance); break;
                            case 3: appearances.Missles.Add(appearance); break;
                        }
                    }
                }
                
                jsonTokens.Add(new AppearancesToken() {
                    file = "appearances.dat"
                });
                return appearances;
            } catch (Exception e) {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                Environment.Exit(0);
            }

            return null;
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

        static void InternalSaveStaticBitmaps(RepeatedField<uint> sprites, DrawBitmapsDelegate drawFunc, int layers, int spriteType, int localStart, Core.Sprites.ContentSprites sprParser, int dX, int dY) {
            int singleSize = dX * dY;

            AsyncGraphics gfx = new AsyncGraphics(new Bitmap(SEGMENT_DIMENTION, SEGMENT_DIMENTION));
            string filename;

            int x = 0, y = 0, z = 0;
            for (int i = 0; i < sprites.Count;) {
                Bitmap[] smallBitmaps = new Bitmap[layers];
                for (int m = 0; m < layers; m++) {
                    if (i + m >= sprites.Count)
                        break;

                    smallBitmaps[m] = sprParser.GetSprite(sprites[i + m]);
                }

                if (y >= SEGMENT_DIMENTION) {
                    filename = string.Format("sprites-{0}-{1}.png", localStart, localStart + (BITMAP_SIZE / singleSize) - 1);
                    tasks.Add(gfx.SaveAndDispose(Path.Combine("sprites", filename)));
                    
                    jsonTokens.Add(new SpritesToken() {
                        file = filename,
                        spritetype = spriteType,
                        firstspriteid = localStart,
                        lastspriteid = localStart + (BITMAP_SIZE / singleSize) - 1
                    });

                    localStart += BITMAP_SIZE / singleSize;

                    gfx = new AsyncGraphics(new Bitmap(SEGMENT_DIMENTION, SEGMENT_DIMENTION));
                    x = y = z = 0;
                }

                var tmpSmallBitmaps = smallBitmaps;
                drawFunc(gfx, smallBitmaps, x, y);
                tasks.Add(gfx.DisposeOnDone(smallBitmaps));

                x += dX;
                if (x >= SEGMENT_DIMENTION) {
                    y += dY;
                    x = 0;
                }

                if (i == sprites.Count)
                    break;

                i = Math.Min(i + layers, sprites.Count);
                z++;
            }
            
            // save the last gfx
            int end = localStart + z;
            filename = string.Format("sprites-{0}-{1}.png", localStart, end - 1);
            tasks.Add(gfx.SaveAndDispose(Path.Combine("sprites", filename)));
            
            jsonTokens.Add(new SpritesToken() {
                file = filename,
                spritetype = spriteType,
                firstspriteid = localStart,
                lastspriteid = end - 1
            });
        }

        static void SaveStaticBitmaps(RepeatedField<uint> sprites, ref int start, Core.Sprites.ContentSprites sprParser, int dX, int dY) {
            DrawBitmapsDelegate drawFunc;
            int layers = 0;
            int spritetype = 1;
            if (dX == 32 && dY == 32) {
                drawFunc = DrawBitmap32x32From1_32x32;
                layers = 1;
            } else if (dX == 32 && dY == 64) {
                drawFunc = DrawBitmap32x64From2_32x32;
                layers = 2;
                spritetype = 2;
            } else if (dX == 64 && dY == 32) {
                drawFunc = DrawBitmap64x32From2_32x32;
                layers = 2;
                spritetype = 3;
            } else {
                drawFunc = DrawBitmap64x64From4_32x32;
                layers = 4;
                spritetype = 4;
            }
            
            int amountInBitmap = BITMAP_SIZE / (32 * 32);
            int totalBitmaps = (int)Math.Ceiling((double)sprites.Count / amountInBitmap);
            if (totalBitmaps == 0)
                return;

            int localStart = start;
            start += sprites.Count / layers;

            tasks.Add(Task.Run(() => InternalSaveStaticBitmaps(sprites, drawFunc, layers, spritetype, localStart, sprParser, dX, dY)));
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

        static void SaveSprites(RepeatedField<Appearance> appearances, ref int start, Core.Sprites.ContentSprites sprParser) {
            RepeatedField<uint>[] sortedFrameGroups = new RepeatedField<uint>[4];
            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances, ref sortedFrameGroups);
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64);
        }

        static void GenerateEverything(string datfile, string sprfile, int clientVersion, bool useAlpha) {
            // generating new appearances
            Appearances appearances = GenerateAppearances(datfile, clientVersion);

            // creating the sprites folder to save files in.
            Directory.CreateDirectory("sprites");

            // loading tibia.spr into chunks
            Core.Sprites.ContentSprites sprParser;
            try {
                var bytes = File.ReadAllBytes(sprfile);
                sprParser = new Core.Sprites.ContentSprites(bytes, useAlpha);
                sprParser.Parse();
            } catch (Exception e) {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                Environment.Exit(0);
                return;
            }

            int start = 0;
            SaveSprites(appearances.Outfits, ref start, sprParser);
            SaveSprites(appearances.Effects, ref start, sprParser);
            SaveSprites(appearances.Missles, ref start, sprParser);
            SaveSprites(appearances.Objects, ref start, sprParser);

            Task.WaitAll(tasks.ToArray());

            // saving appearances.dat (with the respective version)
            using (FileStream file = File.Create("appearances.dat")) {
                appearances.WriteTo(file);
            }
            
            // saving spritesheets information (catalog-content)
            using (FileStream file = File.Create("catalog-content.json")) {
                jsonTokens.Sort(ITokenItemSort);
                foreach (var token in jsonTokens) {
                    catalogJson.Add(token.GetJObject());
                }

                string str = catalogJson.ToString();
                file.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
            }
        }

        private static int ITokenItemSort(ITokenItem x, ITokenItem y) {
            if (x is AppearancesToken)
                return -1;
            else if (y is AppearancesToken)
                return 1;

            SpritesToken a = (SpritesToken)x;
            SpritesToken b = (SpritesToken)y;
            return a.firstspriteid.CompareTo(b.firstspriteid);
        }

        static void Main(string[] args) {
            // todo: this class must be cleaned
            /* todo: introduce semaphores to limit the maximum concurrent threads
             * to avoid intense cpu usage */ 

            string datFile = null;
            string sprFile = null;
            int clientVersion = -1;
            bool useAlpha = false;
            foreach (var arg in args) {
                if (arg.StartsWith("--dat=")) {
                    datFile = arg.Substring(6);
                    Console.WriteLine("Dat: " + datFile);
                } else if (arg.StartsWith("--spr=")) {
                    sprFile = arg.Substring(6);
                    Console.WriteLine("Spr: " + sprFile);
                } else if (arg.StartsWith("--version=")) {
                    Console.WriteLine("Version: " + arg.Substring(10));
                    clientVersion = int.Parse(arg.Substring(10));
                } else if (arg.StartsWith("--alpha=")) {
                    var boolstr = arg.Substring(8).ToLower();
                    useAlpha = boolstr == "y" || boolstr == "yes" || boolstr == "true" || boolstr == "1";
                }
            }

            if (datFile == null || sprFile == null || clientVersion == -1) {
                Console.WriteLine("Invalid parameters, you should add sprites, dat files & client version.");
                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            GenerateEverything(datFile, sprFile, clientVersion, useAlpha);

            watch.Stop();
            
            double seconds = watch.ElapsedMilliseconds / (double)1000;
            Console.WriteLine("Time elapsed: " + seconds + " seconds.");
        }
    }
}
