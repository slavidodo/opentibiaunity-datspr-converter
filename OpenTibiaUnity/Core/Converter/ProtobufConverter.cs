using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTibiaUnity.Core.Converter
{
    public class ProtobufConverter : IConverter
    {
        private int m_ClientVersion;
        private int m_BuildVersion;
        private string m_AppearancesFile = null;
        private List<SpriteTypeImpl> m_SpriteSheet = new List<SpriteTypeImpl>();
        private List<ITokenItem> m_JsonTokens = new List<ITokenItem>();

        public ProtobufConverter(int clientVersion, int buildVersion) {
            m_ClientVersion = clientVersion;
            m_BuildVersion = buildVersion;
        }
        
        public async Task<bool> BeginProcessing() {
            await Task.Yield();

            var defaultPath = Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "assets");

            string catalogContentFile = Path.Combine(defaultPath, "catalog-content.json");
            if (!File.Exists(catalogContentFile)) {
                Console.WriteLine("catalog-content.json not found at {0}", catalogContentFile);
                return false;
            }

            ParseCatalogContent(catalogContentFile);
            
            string datFile = Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "assets", m_AppearancesFile);
            if (!File.Exists(datFile)) {
                Console.WriteLine("appearances.dat not found at {0}", datFile);
                return false;
            }
            
            var bytes = File.ReadAllBytes(datFile);
            var tibiaAppearances = Tibia.Protobuf.Appearances.Appearances.Parser.ParseFrom(bytes);
            
            var openTibiaAppearances = new Protobuf.Appearances.Appearances();
            foreach (var tibiaAppearance in tibiaAppearances.Object)
                openTibiaAppearances.Objects.Add(ConvertAppearance(tibiaAppearance));
            foreach (var tibiaAppearance in tibiaAppearances.Outfit)
                openTibiaAppearances.Outfits.Add(ConvertAppearance(tibiaAppearance));
            foreach (var tibiaAppearance in tibiaAppearances.Effect)
                openTibiaAppearances.Effects.Add(ConvertAppearance(tibiaAppearance));
            foreach (var tibiaAppearance in tibiaAppearances.Missile)
                openTibiaAppearances.Missles.Add(ConvertAppearance(tibiaAppearance));

            if (tibiaAppearances.SpecialMeaningAppearanceIds != null) {
                openTibiaAppearances.SpecialMeaningAppearanceIDs = new Protobuf.Appearances.SpecialMeaningAppearanceIds();
                openTibiaAppearances.SpecialMeaningAppearanceIDs.GoldCoinId = tibiaAppearances.SpecialMeaningAppearanceIds.GoldCoinId;
                openTibiaAppearances.SpecialMeaningAppearanceIDs.PlatinumCoinId = tibiaAppearances.SpecialMeaningAppearanceIds.PlatinumCoinId;
                openTibiaAppearances.SpecialMeaningAppearanceIDs.CrystalCoinId = tibiaAppearances.SpecialMeaningAppearanceIds.CrystalCoinId;
                openTibiaAppearances.SpecialMeaningAppearanceIDs.TibiaCoinId = tibiaAppearances.SpecialMeaningAppearanceIds.TibiaCoinId;
                openTibiaAppearances.SpecialMeaningAppearanceIDs.StampedLetterId = tibiaAppearances.SpecialMeaningAppearanceIds.StampedLetterId;
                openTibiaAppearances.SpecialMeaningAppearanceIDs.SupplyStashId = tibiaAppearances.SpecialMeaningAppearanceIds.SupplyStashId;
            }

            m_JsonTokens.Add(new AppearancesToken() {
                file = "appearances.dat"
            });

            Directory.CreateDirectory(Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "result"));

            // saving appearances.dat (with the respective version)
            using (var stream = File.Create(Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "result", "appearances.dat"))) {
                openTibiaAppearances.WriteTo(stream);
            }
            
            Console.Write("Processing Spritesheets...");
            Directory.CreateDirectory(Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "result", "sprites"));

            var spriteBuffer = new MemoryStream();
            foreach (var spriteType in m_SpriteSheet) {
                var spriteFile = Path.Combine(defaultPath, spriteType.File);
                if (!File.Exists(spriteFile))
                    continue;

                var decoder = new SevenZip.Compression.LZMA.Decoder();
                using (BinaryReader reader = new BinaryReader(File.OpenRead(spriteFile))) {
                    var input = reader.BaseStream;

                    while (reader.ReadByte() == 0) { }
                    reader.ReadUInt32();

                    while ((reader.ReadByte() & 0x80) == 0x80) { } // LZMA size, 7-bit integer where MSB = flag for next byte used

                    // LZMA file
                    decoder.SetDecoderProperties(reader.ReadBytes(5));
                    reader.ReadUInt64(); // Should be the decompressed size but CIP writes the compressed sized, so just use a large buffer size

                    // Disabled arithmetic underflow/overflow check in debug mode so this won't cause an exception
                    spriteBuffer.Position = 0;
                    decoder.Code(input, spriteBuffer, input.Length - input.Position, 0x100000, null);

                    spriteBuffer.Position = 0;
                    var image = Image.FromStream(spriteBuffer);

                    var filename = string.Format("sprites-{0}-{1}.png", spriteType.FirstSpriteID, spriteType.LastSpriteID);
                    var path = Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "result", "sprites", filename);
                    image.Save(path, ImageFormat.Png);

                    m_JsonTokens.Add(new SpritesToken() {
                        file = filename,
                        spritetype = spriteType.SpriteType,
                        firstspriteid = (int)spriteType.FirstSpriteID,
                        lastspriteid = (int)spriteType.LastSpriteID,
                    });
                }
            }

            spriteBuffer.Dispose();
            Console.WriteLine("\rProcessing Spritesheets: Done!");

            // saving spritesheets information (catalog-content)
            using (FileStream file = File.Create(Path.Combine(m_ClientVersion.ToString(), m_BuildVersion.ToString(), "result", "catalog-content.json"))) {
                var catalogJson = new JArray();
                foreach (var token in m_JsonTokens)
                    catalogJson.Add(token.GetJObject());

                string str = catalogJson.ToString();
                file.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
            }

            return true;
        }

        public List<SpriteTypeImpl> GetSpriteSheet() {
            return m_SpriteSheet;
        }

        private Protobuf.Appearances.Appearance ConvertAppearance(Tibia.Protobuf.Appearances.Appearance tibiaAppearance) {
            var appearance = new Protobuf.Appearances.Appearance();
            appearance.ID = tibiaAppearance.Id;
            appearance.Name = tibiaAppearance.Name;
            
            // frame groups
            foreach (var tibiaFrameGroup in tibiaAppearance.FrameGroup) {
                var frameGroup = new Protobuf.Appearances.FrameGroup();
                appearance.FrameGroups.Add(frameGroup);
                frameGroup.Type = (Protobuf.Shared.FrameGroupType)tibiaFrameGroup.FixedFrameGroup;
                frameGroup.ID = tibiaFrameGroup.Id;

                var tibiaSpriteInfo = tibiaFrameGroup.SpriteInfo;
                if (tibiaSpriteInfo != null) {
                    var spriteInfo = new Protobuf.Appearances.SpriteInfo();
                    frameGroup.SpriteInfo = spriteInfo;

                    spriteInfo.PatternWidth = tibiaSpriteInfo.PatternWidth;
                    spriteInfo.PatternHeight = tibiaSpriteInfo.PatternHeight;
                    spriteInfo.PatternDepth = tibiaSpriteInfo.PatternDepth;
                    spriteInfo.Layers = tibiaSpriteInfo.Layers;
                    spriteInfo.SpriteIDs.AddRange(tibiaSpriteInfo.SpriteId);
                    spriteInfo.BoundingSquare = tibiaSpriteInfo.BoundingSquare;
                    spriteInfo.IsOpaque = tibiaSpriteInfo.IsOpaque;
                    foreach (var tibiaBox in tibiaSpriteInfo.BoundingBoxPerDirection) {
                        spriteInfo.BoundingBoxPerDirection.Add(new Protobuf.Appearances.Box() {
                            X = tibiaBox.X,
                            Y = tibiaBox.Y,
                            Width = tibiaBox.Width,
                            Height = tibiaBox.Height
                        });
                    }

                    // animation
                    var tibiaSpriteAnimation = tibiaSpriteInfo.Animation;
                    if (tibiaSpriteAnimation != null) {
                        var spriteAnimation = new Protobuf.Appearances.SpriteAnimation();
                        spriteInfo.Animation = spriteAnimation;

                        spriteAnimation.DefaultStartPhase = tibiaSpriteAnimation.DefaultStartPhase;
                        spriteAnimation.Synchronized = tibiaSpriteAnimation.Synchronized;
                        spriteAnimation.RandomStartPhase = tibiaSpriteAnimation.RandomStartPhase;
                        spriteAnimation.LoopCount = tibiaSpriteAnimation.LoopCount;

                        if ((int)tibiaSpriteAnimation.LoopType == -1)
                            spriteAnimation.LoopType = Protobuf.Shared.AnimationLoopType.PingPong;
                        else
                            spriteAnimation.LoopType = (Protobuf.Shared.AnimationLoopType)tibiaSpriteAnimation.LoopType;

                        foreach (var tibiaSpritePhase in tibiaSpriteAnimation.SpritePhase) {
                            spriteAnimation.SpritePhases.Add(new Protobuf.Appearances.SpritePhase() {
                                DurationMin = tibiaSpritePhase.DurationMin,
                                DurationMax = tibiaSpritePhase.DurationMax
                            });
                        }

                        spriteInfo.Phases = (uint)tibiaSpriteAnimation.SpritePhase.Count;
                    } else {
                        spriteInfo.Phases = 1;
                    }
                }
            }

            // flags
            var tibiaFlags = tibiaAppearance.Flags;
            if (tibiaFlags != null) {
                var flags = new Protobuf.Appearances.AppearanceFlags();
                appearance.Flags = flags;

                var tibiaFlagBank = tibiaFlags.Bank;
                if (tibiaFlagBank != null)
                    flags.Ground = new Protobuf.Appearances.AppearanceFlagGround() { Speed = tibiaFlagBank.Waypoints };

                flags.GroundBorder = tibiaFlags.Clip;
                flags.Bottom = tibiaFlags.Bottom;
                flags.Top = tibiaFlags.Top;
                flags.Container = tibiaFlags.Container;
                flags.Stackable = tibiaFlags.Cumulative;
                flags.ForceUse = tibiaFlags.Forceuse;
                flags.MultiUse = tibiaFlags.Multiuse;

                var tibiaFlagWrite = tibiaFlags.Write;
                if (tibiaFlagWrite != null)
                    flags.Writable = new Protobuf.Appearances.AppearanceFlagWritable() { MaxTextLength = tibiaFlagWrite.MaxTextLength };

                var tibiaFlagWriteOnce = tibiaFlags.WriteOnce;
                if (tibiaFlagWriteOnce != null)
                    flags.WritableOnce = new Protobuf.Appearances.AppearanceFlagWritableOnce() { MaxTextLengthOnce = tibiaFlagWriteOnce.MaxTextLengthOnce };

                flags.FluidContainer = tibiaFlags.Liquidcontainer;
                flags.Splash = tibiaFlags.Liquidpool;
                flags.Unpassable = tibiaFlags.Unpass;
                flags.Unmoveable = tibiaFlags.Unmove;
                flags.Unsight = tibiaFlags.Unsight;
                flags.BlockPath = tibiaFlags.Avoid;
                flags.Hangable = tibiaFlags.Hang;

                var tibiaFlagHook = tibiaFlags.Hook;
                if (tibiaFlagHook != null) {
                    var hook = new Protobuf.Appearances.AppearanceFlagHook();
                    switch (tibiaFlagHook.Direction) {
                        case Tibia.Protobuf.Shared.HOOK_TYPE.South:
                            hook.Type = Protobuf.Shared.HookType.South;
                            break;
                        case Tibia.Protobuf.Shared.HOOK_TYPE.East:
                            hook.Type = Protobuf.Shared.HookType.East;
                            break;
                    }

                    flags.Hook = hook;
                }

                flags.Rotateable = tibiaFlags.Rotate;

                var tibiaFlagLight = tibiaFlags.Light;
                if (tibiaFlagLight != null)
                    flags.Light = new Protobuf.Appearances.AppearanceFlagLight() {
                        Intensity = tibiaFlagLight.Brightness,
                        Color = tibiaFlagLight.Color
                    };

                flags.DontHide = tibiaFlags.DontHide;
                flags.Translucent = tibiaFlags.Translucent;

                var tibiaFlagShift = tibiaFlags.Shift;
                if (tibiaFlagShift != null)
                    flags.Offset = new Protobuf.Appearances.AppearanceFlagOffset() {
                        X = tibiaFlagShift.X,
                        Y = tibiaFlagShift.Y
                    };

                var tibiaFlagHeight = tibiaFlags.Height;
                if (tibiaFlagHeight != null)
                    flags.Height = new Protobuf.Appearances.AppearanceFlagHeight() { Elevation = tibiaFlagHeight.Elevation };

                flags.LyingCorpse = tibiaFlags.LyingObject;
                flags.AnimateAlways = tibiaFlags.AnimateAlways;

                var tibiaFlagAutomap = tibiaFlags.Automap;
                if (tibiaFlagAutomap != null)
                    flags.Automap = new Protobuf.Appearances.AppearanceFlagAutomap() { Color = tibiaFlagAutomap.Color };

                var tibiaFlagsLenshelp = tibiaFlags.Lenshelp;
                if (tibiaFlagsLenshelp != null)
                    flags.LensHelp = new Protobuf.Appearances.AppearanceFlagLensHelp() { ID = tibiaFlagsLenshelp.Id };

                flags.FullGround = tibiaFlags.Fullbank;
                flags.IgnoreLook = tibiaFlags.IgnoreLook;

                var tibiaFlagClothes = tibiaFlags.Clothes;
                if (tibiaFlagClothes != null)
                    flags.Clothes = new Protobuf.Appearances.AppearanceFlagClothes() { Slot = tibiaFlagClothes.Slot };

                var tibiaFlagMarket = tibiaFlags.Market;
                if (tibiaFlagMarket != null) {
                    flags.Market = new Protobuf.Appearances.AppearanceFlagMarket() {
                        Category = (Protobuf.Shared.ItemCategory)tibiaFlagMarket.Category,
                        TradeAsObjectID = tibiaFlagMarket.TradeAsObjectId,
                        ShowAsObjectID = tibiaFlagMarket.ShowAsObjectId,
                        MinimumLevel = tibiaFlagMarket.MinimumLevel,
                    };

                    foreach (var professhion in tibiaFlagMarket.RestrictToProfession) {
                        if ((int)professhion == -1)
                            flags.Market.RestrictToProfession.Add(Protobuf.Shared.PlayerProfession.Any);
                        else
                            flags.Market.RestrictToProfession.Add((Protobuf.Shared.PlayerProfession)professhion);
                    }
                }

                var tibiaFlagDefaultAction = tibiaFlags.DefaultAction;
                if (tibiaFlagDefaultAction != null)
                    flags.DefaultAction = new Protobuf.Appearances.AppearanceFlagDefaultAction() { Action = (Protobuf.Shared.PlayerAction)tibiaFlagDefaultAction.Action };

                flags.Use = tibiaFlags.Usable;
                flags.Wrapable = tibiaFlags.Wrap;
                flags.UnWrapable = tibiaFlags.Unwrap;
                flags.TopEffect = tibiaFlags.Topeffect;

                foreach (var npcSaleData in tibiaFlags.Npcsaledata) {
                    flags.NpcSaleData.Add(new Protobuf.Appearances.AppearanceFlagNPC() {
                        Name = npcSaleData.Name,
                        Location = npcSaleData.Location,
                        SalePrice = npcSaleData.SalePrice,
                        BuyPrice = npcSaleData.BuyPrice,
                    });
                }

                var tibiaFlagChangedToExpire = tibiaFlags.Changedtoexpire;
                if (tibiaFlagChangedToExpire != null)
                    flags.ChangedToExpire = new Protobuf.Appearances.AppearanceFlagChangedToExpire() { FormerObjectTypeID = tibiaFlagChangedToExpire.FormerObjectTypeid };

                flags.Corpse = tibiaFlags.Corpse;
                flags.PlayerCorpse = tibiaFlags.PlayerCorpse;

                var tibiaFlagCyclopediaItem = tibiaFlags.Cyclopediaitem;
                if (tibiaFlagCyclopediaItem != null)
                    flags.CyclopediaItem = new Protobuf.Appearances.AppearanceFlagCyclopedia() { CyclopediaType = tibiaFlagCyclopediaItem.CyclopediaType };
            }

            return appearance;
        }

        private void ParseCatalogContent(string filename) {
            var catalogObjects = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(filename));
            if (catalogObjects == null)
                throw new System.Exception("SpriteProvider.SpritesProvider: Invalid catalog-content JSON");
            
            foreach (var @object in catalogObjects.Children<JObject>()) {
                var typeProperty = @object.Property("type");
                if (typeProperty == null)
                    continue;

                if (typeProperty.Value.ToString() == "appearances") {
                    if (!@object.TryGetValue("file", out JToken appearancesFileToken))
                        continue;

                    m_AppearancesFile = appearancesFileToken.ToString();
                }

                if (!@object.TryGetValue("file", out JToken fileToken)
                    || !@object.TryGetValue("spritetype", out JToken spriteTypeToken)
                    || !@object.TryGetValue("firstspriteid", out JToken firstSpriteIDToken)
                    || !@object.TryGetValue("lastspriteid", out JToken lastSpriteIDToken))
                    continue;

                try {
                    m_SpriteSheet.Add(new SpriteTypeImpl() {
                        File = (string)fileToken,
                        SpriteType = (int)spriteTypeToken + 1,
                        FirstSpriteID = (uint)firstSpriteIDToken,
                        LastSpriteID = (uint)lastSpriteIDToken
                    });
                } catch (System.InvalidCastException) { }
            }
        }
    }
}
