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
using OpenTibiaUnity.Core.Metaflags;
using OpenTibiaUnity.Proto.Appearances;

namespace OpenTibiaUnity
{
    // This is taken from the the OpenOpenTibiaUnity project

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
            if (thingType.HasAttribute(AttributesUniform.Ground)) appearance.Flags.Ground = new Ground() { Speed = (ushort)thingType.Attributes[AttributesUniform.Ground] };
            if (thingType.HasAttribute(AttributesUniform.Writable)) appearance.Flags.Writable = new Writable() { Length = (ushort)thingType.Attributes[AttributesUniform.Writable] };
            if (thingType.HasAttribute(AttributesUniform.WritableOnce)) appearance.Flags.WritableOnce = new Writable() { Length = (ushort)thingType.Attributes[AttributesUniform.WritableOnce] };
            if (thingType.HasAttribute(AttributesUniform.MinimapColor)) appearance.Flags.Minimap = new MiniMap() { Color = (ushort)thingType.Attributes[AttributesUniform.MinimapColor] };
            if (thingType.HasAttribute(AttributesUniform.Elevation)) appearance.Flags.Elevation = new Elevation() { Elevation_ = (ushort)thingType.Attributes[AttributesUniform.Elevation] };
            if (thingType.HasAttribute(AttributesUniform.LensHelp)) appearance.Flags.LensHelp = new LensHelp() { Id = (ushort)thingType.Attributes[AttributesUniform.LensHelp] };
            if (thingType.HasAttribute(AttributesUniform.Cloth)) appearance.Flags.Cloth = new Clothes() { Slot = (ushort)thingType.Attributes[AttributesUniform.Cloth] };

            // default action
            if (thingType.HasAttribute(AttributesUniform.DefaultAction)) {
                var defaultAction = new DefaultAction();
                var oldDefaultActionValue = (ushort)thingType.Attributes[AttributesUniform.DefaultAction];
                if (oldDefaultActionValue > 4)
                    Console.WriteLine("Invalid default action: " + oldDefaultActionValue + " for item id: " + thingType.ID);
                appearance.Flags.DefaultAction = new DefaultAction() { Action = (PlayerAction)oldDefaultActionValue };
            }

            if (thingType.HasAttribute(AttributesUniform.GroundBorder)) appearance.Flags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.Bottom)) appearance.Flags.Bottom = true;
            if (thingType.HasAttribute(AttributesUniform.Top)) appearance.Flags.Top = true;
            if (thingType.HasAttribute(AttributesUniform.Container)) appearance.Flags.Container = true;
            if (thingType.HasAttribute(AttributesUniform.Stackable)) appearance.Flags.Stackable = true;
            if (thingType.HasAttribute(AttributesUniform.Use)) appearance.Flags.Use = true;
            if (thingType.HasAttribute(AttributesUniform.ForceUse)) appearance.Flags.ForceUse = true;
            if (thingType.HasAttribute(AttributesUniform.MultiUse)) appearance.Flags.MultiUse = true;
            if (thingType.HasAttribute(AttributesUniform.FluidContainer)) appearance.Flags.FluidContainer = true;
            if (thingType.HasAttribute(AttributesUniform.Splash)) appearance.Flags.Splash = true;
            if (thingType.HasAttribute(AttributesUniform.Unpassable)) appearance.Flags.Unpassable = true;
            if (thingType.HasAttribute(AttributesUniform.Unmoveable)) appearance.Flags.Unmoveable = true;
            if (thingType.HasAttribute(AttributesUniform.Unsight)) appearance.Flags.Unsight = true;
            if (thingType.HasAttribute(AttributesUniform.BlockPath)) appearance.Flags.BlockPath = true;
            if (thingType.HasAttribute(AttributesUniform.NoMoveAnimation)) appearance.Flags.NoMoveAnimation = true;
            if (thingType.HasAttribute(AttributesUniform.Pickupable)) appearance.Flags.Pickupable = true;
            if (thingType.HasAttribute(AttributesUniform.Hangable)) appearance.Flags.Hangable = true;

            // can have only one hook //
            if (thingType.HasAttribute(AttributesUniform.HookSouth)) appearance.Flags.Hook = new Hook() { Type = HookType.South };
            else if (thingType.HasAttribute(AttributesUniform.HookEast)) appearance.Flags.Hook = new Hook() { Type = HookType.East };

            if (thingType.HasAttribute(AttributesUniform.Rotateable)) appearance.Flags.Rotateable = true;
            if (thingType.HasAttribute(AttributesUniform.DontHide)) appearance.Flags.DontHide = true;
            if (thingType.HasAttribute(AttributesUniform.Translucent)) appearance.Flags.Translucent = true;
            if (thingType.HasAttribute(AttributesUniform.LyingCorpse)) appearance.Flags.LyingCorpse = true;
            if (thingType.HasAttribute(AttributesUniform.AnimateAlways)) appearance.Flags.AnimateAlways = true;
            if (thingType.HasAttribute(AttributesUniform.FullGround)) appearance.Flags.FullGround = true;
            if (thingType.HasAttribute(AttributesUniform.Look)) appearance.Flags.Look = true;
            if (thingType.HasAttribute(AttributesUniform.Wrapable)) appearance.Flags.Wrapable = true;
            if (thingType.HasAttribute(AttributesUniform.Unwrapable)) appearance.Flags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.TopEffect)) appearance.Flags.TopEffect = true;

            if (thingType.HasAttribute(AttributesUniform.Light)) {
                var lightInfo = (Core.Sprites.LightInfo)thingType.Attributes[AttributesUniform.Light];

                appearance.Flags.Light = new LightInfo() {
                    Intensity = lightInfo.intensity,
                    Color = lightInfo.color,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Offset)) {
                var displacement = (Core.Sprites.Vector2Int)thingType.Attributes[AttributesUniform.Offset];
                appearance.Flags.Displacement = new Displacement() {
                    X = displacement.x,
                    Y = displacement.y,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Market)) {
                var Market = (Core.Sprites.MarketData)thingType.Attributes[AttributesUniform.Market];

                appearance.Flags.Market = new MarketInfo() {
                    Category = Market.category,
                    TradeAs = Market.tradeAs,
                    ShowAs = Market.showAs,
                    Name = Market.name,
                    RestrictVocation = Market.restrictProfession,
                    RequiredLevel = Market.restrictLevel,
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
        /// <param name="clientVersion">the client version of this dat</param>
        /// <returns></returns>
        static Appearances GenerateAppearances(string datFile, int clientVersion) {
            try {
                Core.Sprites.ContentData datParser = new Core.Sprites.ContentData(File.ReadAllBytes(datFile), clientVersion);
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

        static void InternalSaveStaticBitmaps(RepeatedField<uint> sprites, DrawBitmapsDelegate drawFunc, int layers, int spriteType, int localStart, Core.Sprites.ContentSprites sprParser, int dX, int dY, int clientVersion) {
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
                    tasks.Add(gfx.SaveAndDispose(Path.Combine(clientVersion.ToString() + "/sprites", filename)));
                    
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
            tasks.Add(gfx.SaveAndDispose(Path.Combine(clientVersion.ToString(), "sprites", filename)));
            
            jsonTokens.Add(new SpritesToken() {
                file = filename,
                spritetype = spriteType,
                firstspriteid = localStart,
                lastspriteid = end - 1
            });
        }

        static void SaveStaticBitmaps(RepeatedField<uint> sprites, ref int start, Core.Sprites.ContentSprites sprParser, int dX, int dY, int clientVersion) {
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

            tasks.Add(Task.Run(() => InternalSaveStaticBitmaps(sprites, drawFunc, layers, spritetype, localStart, sprParser, dX, dY, clientVersion)));
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

        static void DeploySprites(RepeatedField<Appearance> appearances, RepeatedField<uint>[] sortedFrameGroups) {
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

        static void SaveSprites(RepeatedField<Appearance> appearances, ref int start, Core.Sprites.ContentSprites sprParser, int clientVersion) {
            RepeatedField<uint>[] sortedFrameGroups = new RepeatedField<uint>[4];
            for (int i = 0; i < 4; i++) sortedFrameGroups[i] = new RepeatedField<uint>();
            DeploySprites(appearances, sortedFrameGroups);
            
            SaveStaticBitmaps(sortedFrameGroups[0], ref start, sprParser, 32, 32, clientVersion);
            SaveStaticBitmaps(sortedFrameGroups[1], ref start, sprParser, 32, 64, clientVersion);
            SaveStaticBitmaps(sortedFrameGroups[2], ref start, sprParser, 64, 32, clientVersion);
            SaveStaticBitmaps(sortedFrameGroups[3], ref start, sprParser, 64, 64, clientVersion);
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


        static void GenerateEverything(int clientVersion, bool useAlpha) {
            // generating new appearances
            string datFile = clientVersion.ToString() + "/Tibia.dat";
            string sprFile = clientVersion.ToString() + "/Tibia.spr";
            if (!File.Exists(datFile) || !File.Exists(sprFile)) {
                Console.WriteLine("Tibia.dat or Tibia.spr doesn't exist");
                Environment.Exit(0);
                return;
            }

            Appearances appearances = GenerateAppearances(datFile, clientVersion);
            
            // loading tibia.spr into chunks
            Core.Sprites.ContentSprites sprParser;
            try {
                var bytes = File.ReadAllBytes(sprFile);
                sprParser = new Core.Sprites.ContentSprites(bytes, clientVersion, useAlpha);
                sprParser.Parse();
            } catch (Exception e) {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                Environment.Exit(0);
                return;
            }

            Directory.CreateDirectory(clientVersion + "/sprites");

            int start = 0;
            SaveSprites(appearances.Outfits, ref start, sprParser, clientVersion);
            SaveSprites(appearances.Effects, ref start, sprParser, clientVersion);
            SaveSprites(appearances.Missles, ref start, sprParser, clientVersion);
            SaveSprites(appearances.Objects, ref start, sprParser, clientVersion);

            Task.WaitAll(tasks.ToArray());

            // saving appearances.dat (with the respective version)
            using (FileStream file = File.Create(clientVersion + "/appearances.dat")) {
                appearances.WriteTo(file);
            }
            
            // saving spritesheets information (catalog-content)
            using (FileStream file = File.Create(clientVersion + "/catalog-content.json")) {
                jsonTokens.Sort(ITokenItemSort);
                foreach (var token in jsonTokens) {
                    catalogJson.Add(token.GetJObject());
                }

                string str = catalogJson.ToString();
                file.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
            }
        }

        static void ConvertClientVersion(int fromVersion, int toVersion, bool useAlpha) {
            string datFile = fromVersion.ToString() + "/Tibia.dat";
            string sprFile = fromVersion.ToString() + "/Tibia.spr";
            if (!File.Exists(datFile) || !File.Exists(sprFile)) {
                Console.WriteLine("Tibia.dat or Tibia.spr doesn't exist");
                Environment.Exit(0);
                return;
            }

            Directory.CreateDirectory(toVersion.ToString());
            string newDatFile = toVersion.ToString() + "/Tibia.dat";
            string newSprFile = toVersion.ToString() + "/Tibia.spr";
            
            var datParser = new Core.Sprites.ContentData(File.ReadAllBytes(datFile), fromVersion);
            datParser.Parse();

            byte[] result = datParser.ConvertTo(toVersion);
            File.WriteAllBytes(newDatFile, result);

            var sprParser = new Core.Sprites.ContentSprites(File.ReadAllBytes(sprFile), fromVersion, useAlpha);
            sprParser.Parse();

            result = sprParser.ConvertTo(toVersion);
            File.WriteAllBytes(newSprFile, result);

            Console.WriteLine("Convertion Successfull to " + toVersion + ".");
        }

        static void Main(string[] args) {
            // todo: this class must be cleaned
            /* todo: introduce semaphores to limit the maximum concurrent threads
             * to avoid intense cpu usage */ 

            int clientVersion = -1;
            int convertTo = -1;
            bool useAlpha = false;
            foreach (var arg in args) {
                if (arg.StartsWith("--version=")) {
                    clientVersion = int.Parse(arg.Substring(10));
                } else if (arg.StartsWith("--alpha=")) {
                    var boolstr = arg.Substring(8).ToLower();
                    useAlpha = boolstr == "y" || boolstr == "yes" || boolstr == "true" || boolstr == "1";
                } else if (arg.StartsWith("--convert-to=")) {
                    convertTo = int.Parse(arg.Substring(13));
                } else {
                    Console.WriteLine("Invalid Attribute: " + arg);
                    return;
                }
            }

            if (clientVersion == -1) {
                Console.WriteLine("Invalid client version.");
                return;
            }

            Console.WriteLine("Loading version: " + clientVersion);
            
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (convertTo != -1) {
                Console.WriteLine("Converting to: " + convertTo);
                ConvertClientVersion(clientVersion, convertTo, useAlpha);
            } else {
                GenerateEverything(clientVersion, useAlpha);
            }

            watch.Stop();
            
            double seconds = watch.ElapsedMilliseconds / (double)1000;
            Console.WriteLine("Time elapsed: " + seconds + " seconds.");
        }
    }
}
