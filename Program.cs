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
using OpenTibiaUnity.Protobuf.Appearances;

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

        public AppearancesToken(string file) {
            this.file = file;
        }

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
            
            var datParser = new Core.Assets.ContentData(File.ReadAllBytes(datFile), fromVersion);

            byte[] result = datParser.ConvertTo(toVersion);
            File.WriteAllBytes(newDatFile, result);

            var sprParser = new Core.Assets.ContentSprites(File.ReadAllBytes(sprFile), fromVersion, useAlpha);

            result = sprParser.ConvertTo(toVersion);
            File.WriteAllBytes(newSprFile, result);

            Console.WriteLine("Convertion Successfull to " + toVersion + ".");
        }

        static void Main(string[] args) {
            int clientVersion = -1;
            int buildVersion = -1;
            int convertTo = -1;
            bool useAlpha = false;
            foreach (var arg in args) {
                if (arg.StartsWith("--version=")) {
                    int.TryParse(arg.Substring(10), out clientVersion);
                } else if (arg.StartsWith("--build-version=")) {
                    int.TryParse(arg.Substring(16), out buildVersion);
                } else if (arg.StartsWith("--alpha=")) {
                    var boolstr = arg.Substring(8).ToLower();
                    useAlpha = boolstr == "y" || boolstr == "yes" || boolstr == "true" || boolstr == "1";
                } else if (arg.StartsWith("--convert-to=")) {
                    convertTo = int.Parse(arg.Substring(13));
                } else {
                    Console.WriteLine("Unknown Attribute: " + arg);
                    return;
                }
            }

            if (clientVersion == -1) {
                Console.WriteLine("Invalid client version.");
                return;
            }

            if (clientVersion >= 1100 && buildVersion == -1) {
                Console.WriteLine("Invalid build version.");
                return;
            }

            if (clientVersion >= 1100)
                Console.WriteLine("Loading version: {0}.{1}", clientVersion, buildVersion);
            else
                Console.WriteLine("Loading version: {0}", clientVersion);
            
            Stopwatch watch = new Stopwatch();
            watch.Start();
            
            if (convertTo != -1) {
                Console.WriteLine("Converting to: " + convertTo);
                ConvertClientVersion(clientVersion, convertTo, useAlpha);
            } else {
                Core.Converter.IConverter converter = null;

                if (clientVersion < 1100)
                    converter = new Core.Converter.LegacyConverter(clientVersion, useAlpha);
                else
                    converter = new Core.Converter.ProtobufConverter(clientVersion, buildVersion);

                var task = converter.BeginProcessing();
                task.Wait();
            }
            
            watch.Stop();
            
            double seconds = watch.ElapsedMilliseconds / (double)1000;
            Console.WriteLine("Time elapsed: " + seconds + " seconds.");
        }
    }
}
