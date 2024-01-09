using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Text;

namespace ESPROG.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class ConfigModel
    {
        private const string ConfigFile = "ESPROG.config.json";

        [JsonProperty]
        public ChipModel Chip { get; set; } = new ChipModel();

        [JsonProperty]
        public FwModel FwWrite { get; set; } = new FwModel();

        public bool LoadConfig()
        {
            try
            {
                using (FileStream fs = new(ConfigFile, FileMode.Open))
                {
                    using (StreamReader sr = new(fs, Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        ConfigModel? config = JsonConvert.DeserializeObject<ConfigModel>(json);
                        if (config != null)
                        {
                            Chip = config.Chip;
                            FwWrite = config.FwWrite;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fail to read config file.");
            }
            return false;
        }

        public bool SaveConfig()
        {
            try
            {
                using (FileStream fs = new(ConfigFile, FileMode.Create))
                {
                    using (StreamWriter sw = new(fs, Encoding.UTF8))
                    {
                        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                        sw.Write(json);
                        sw.Flush();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fail to save config.");
            }
            return false;
        }

        public class ChipModel
        {
            public ushort Chip { get; set; }

            public byte DevAddr { get; set; }
        }

        public class FwModel
        {
            public string FwFileWrite { get; set; } = string.Empty;

            public string ConfigFileWrite { get; set; } = string.Empty;

            public string TrimFileWrite { get; set; } = string.Empty;

            public byte Zone { get; set; }
        }
    }
}
