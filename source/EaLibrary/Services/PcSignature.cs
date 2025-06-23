using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace EaLibrary.Services;
public class PcSignature
{
    public static string GetPcSignature() => GetPcSignature(HardwareInfo.Get());

    public static string GetPcSignature(HardwareInfo h)
    {
        var serialized = ToJson(h);
        var s1 = Base64Encode(serialized);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("ISa3dpGOc8wW7Adn4auACSQmaccrOyR2")); //nt5FfJbdPzNcl2pkC3zgjO43Knvscxft if sv=v2
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(s1));
        var s2 = Base64Encode(hashBytes);
        return $"{s1}.{s2}";
    }

    private static string ToJson(object obj) => JsonConvert.SerializeObject(obj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None });

    private static string Base64Encode(string plainText) => Base64Encode(Encoding.UTF8.GetBytes(plainText));

    private static string Base64Encode(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-');
}

public class HardwareInfo
{
    public string av = "v1";
    public string bsn; //BIOS
    public int gid; //GPU
    public string hsn; //HDD
    public string mac;
    public string mid;
    public string msn; //Mobo
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
            hsn: GetWmiProperty("Win32_DiskDrive", "SerialNumber").First(),
            msn: GetWmiProperty("Win32_BaseBoard", "SerialNumber").First().Trim(),
            mac: GetMacAddress()
        );

        return output;
    }

    public void SetMidField()
    {
        var data = Encoding.UTF8.GetBytes($"{bsn}{gid}{hsn}{msn}");
        ulong offset = 0xcbf29ce484222325;
        const ulong prime = 0x100000001b3;

        foreach (byte b in data)
        {
            offset ^= b;
            offset *= prime;
        }

        //mid = offset.ToString("x16");
        mid = offset.ToString();
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

    private static string GetMacAddress()
    {
        var m = NetworkInterface
            .GetAllNetworkInterfaces()
            .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                   nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            ?.GetPhysicalAddress().ToString();
        if (m != null)
            m = "$" + m;

        return m;
    }

    private static string GetTimeStampString() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:fff");
}