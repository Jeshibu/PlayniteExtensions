using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace EaLibrary.Services
{
    public enum PcSignVersion
    {
        V1,
        V2
    }

    public class PcSignature
    {
        public static string GetPcSignature() => GetPcSignature(HardwareInfo.Get());

        public static string GetPcSignature(HardwareInfo h)
        {
            var serialized = ToJson(h.ToDict());
            var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(serialized));

            using var hmac = new HMACSHA256(h.GetSignKey());
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = Base64UrlEncode(hashBytes);
            
            return $"{payload}.{signature}";
        }

        private static string ToJson(object obj) => JsonConvert.SerializeObject(obj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });

        private static string Base64UrlEncode(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public class HardwareInfo
    {
        public string av = "v1";
        public string bsn;
        public int gid;
        public string hsn;
        public string mac;
        public string mid;
        public string msn;
        public PcSignVersion sv = PcSignVersion.V1;
        public string ts;

        public HardwareInfo() { }

        public HardwareInfo(string bsn, int gid, string hsn, string mac, string msn)
        {
            this.bsn = bsn;
            this.gid = gid;
            this.hsn = hsn;
            this.mac = mac;
            this.msn = msn;
            this.mid = CalculateFnv1aHash(bsn, gid, hsn, msn);
            this.ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
        }

        public static HardwareInfo Get()
        {
            return new HardwareInfo(
                bsn: GetWmiProperty("Win32_BIOS", "SerialNumber").FirstOrDefault()?.Trim() ?? string.Empty,
                gid: GetGpuId(),
                hsn: GetWmiProperty("Win32_DiskDrive", "SerialNumber").FirstOrDefault()?.Trim() ?? string.Empty,
                msn: GetWmiProperty("Win32_BaseBoard", "SerialNumber").FirstOrDefault()?.Trim() ?? string.Empty,
                mac: GetPhysicalMacAddresses().FirstOrDefault()
            );
        }

        public static string CalculateFnv1aHash(string bsn, int gid, string hsn, string msn)
        {
            byte[] hardwareBytes = Encoding.UTF8.GetBytes($"{bsn}{gid}{hsn}{msn}");
            ulong offset = 0xcbf29ce484222325;
            const ulong prime = 0x100000001b3;
            
            foreach (var b in hardwareBytes)
            {
                offset ^= b;
                offset = (offset * prime) & 0xFFFFFFFFFFFFFFFF;
            }
            
            return $"{offset:x16}";
        }

        public byte[] GetSignKey()
        {
            var keys = new Dictionary<PcSignVersion, byte[]>
            {
                { PcSignVersion.V1, Encoding.UTF8.GetBytes("ISa3dpGOc8wW7Adn4auACSQmaccrOyR2") },
                { PcSignVersion.V2, Encoding.UTF8.GetBytes("nt5FfJbdPzNcl2pkC3zgjO43Knvscxft") }
            };

            if (!keys.TryGetValue(sv, out var key))
                throw new ArgumentException($"Version PCSign invalide: {sv}");

            return key;
        }

        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>
            {
                ["av"] = av,
                ["bsn"] = bsn,
                ["gid"] = gid,
                ["hsn"] = hsn,
                ["mid"] = mid,
                ["msn"] = msn,
                ["sv"] = sv == PcSignVersion.V1 ? "v1" : "v2",
                ["ts"] = ts
            };

            if (!string.IsNullOrEmpty(mac))
                dict["mac"] = mac;

            return dict;
        }

        private static IEnumerable<string> GetWmiProperty(string className, string propertyName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
                foreach (var obj in searcher.Get())
                {
                    var value = obj.GetPropertyValue(propertyName)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        yield return value;
                }
            }
            catch
            {
                yield break;
            }
        }

        private static int GetGpuId()
        {
            var pnpIds = GetWmiProperty("Win32_VideoController", "PNPDeviceID");
            foreach (var pnpId in pnpIds)
            {
                if (!pnpId.Contains("DEV_"))
                    continue;

                var hexId = pnpId.Split(new[] { "DEV_" }, StringSplitOptions.None)[1].Split('&')[0];
                if (int.TryParse(hexId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int gid))
                    return gid;
            }
            return 0;
        }

        private static IEnumerable<string> GetPhysicalMacAddresses()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True");
                foreach (var obj in searcher.Get())
                {
                    var macAddress = obj.GetPropertyValue("MACAddress") as string;
                    if (!string.IsNullOrEmpty(macAddress))
                        yield return macAddress;
                }
            }
            catch
            {
                yield break;
            }
        }

        private static string GetTimeStampString() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
    }
}
