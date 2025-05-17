using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace EaLibrary.Services;
public class PcSignature
{
    public static string GetPcSignature() => GetPcSignature(HardwareInfo.Get());

    public static string GetPcSignature(HardwareInfo h)
    {
        var serialized = JsonConvert.SerializeObject(h, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });
        var s1 = Base64Encode(serialized);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("ISa3dpGOc8wW7Adn4auACSQmaccrOyR2")); //nt5FfJbdPzNcl2pkC3zgjO43Knvscxft if sv=v2
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(serialized));
        var s2 = Convert.ToBase64String(hashBytes);
        return $"{s1}.{s2}";
    }

    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }
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
    public string sv = "v1";
    public string ts;

    public HardwareInfo() { }

    public HardwareInfo(string bsn, int gid, string hsn, string mac, string msn)
    {
        this.bsn = bsn;
        this.gid = gid;
        this.hsn = hsn;
        this.mac = mac;
        this.msn = msn;
        SetMidField();
        this.ts = GetTimeStampString();
    }

    public static HardwareInfo Get()
    {
        var output = new HardwareInfo(
            bsn: GetWmiProperty("Win32_BIOS", "SerialNumber").Single(),
            gid: GetGpuId(),
            hsn: GetWmiProperty("Win32_DiskDrive", "SerialNumber").First().Trim(),
            msn: GetWmiProperty("Win32_BaseBoard", "SerialNumber").First().Trim(),
            mac: GetPhysicalMacAddresses().FirstOrDefault()
        );

        return output;
    }

    public void SetMidField()
    {
        var hwBytes = Encoding.UTF8.GetBytes($"{bsn}{gid}{hsn}{msn}");
        ulong offset = 0xcbf29ce484222325;
        ulong prime = 0x100000001b3;

        foreach (var b in hwBytes)
        {
            offset ^= b;
            offset = (offset * prime) & 0xFFFFFFFFFFFFFFFF;
        }

        mid = offset.ToString("x16");
    }

    private static IEnumerable<string> GetWmiProperty(string className, string propertyName)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
        foreach (var obj in searcher.Get())
            yield return obj.GetPropertyValue(propertyName).ToString();
    }

    private static int GetGpuId()
    {
        var pnpIds = GetWmiProperty("Win32_VideoController", "PNPDeviceID");
        foreach (var pnpId in pnpIds)
        {
            if (!pnpId.Contains("DEV_"))
                continue;

            var hexId = pnpId.Split(["DEV_"], StringSplitOptions.None)[1].Split('&')[0];
            if (int.TryParse(hexId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int gid))
                return gid;
        }
        return 0;
    }

    private static IEnumerable<string> GetPhysicalMacAddresses()
    {
        var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter");
        foreach (var obj in searcher.Get())
        {
            var name = obj.GetPropertyValue("Name");
            bool physical = (bool)obj.GetPropertyValue("PhysicalAdapter");
            if (!physical)
                continue;

            yield return obj.GetPropertyValue("MACAddress").ToString();
        }
    }

    private static string GetTimeStampString() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:fff");
}