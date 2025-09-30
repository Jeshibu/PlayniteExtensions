using Newtonsoft.Json;
using System.Management;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.Linq;

namespace EAAppEmulater;

public class HardwareInfo
{
    public static string GetWMI(string className, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
            foreach (ManagementObject obj in searcher.Get())
                return obj[property]?.ToString()?.Trim();
        }
        catch { }
        return string.Empty;
    }

    public static string GetBIOSSerial() =>
        GetWMI("Win32_BIOS", "SerialNumber");

    public static string GetMotherboardSerial() =>
        GetWMI("Win32_BaseBoard", "SerialNumber");

    public static string GetHDDSerial() =>
        GetWMI("Win32_PhysicalMedia", "SerialNumber");

    public static int GetGPUDeviceIdFromPnP()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name FROM Win32_PnPEntity");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "";
                var deviceId = obj["DeviceID"]?.ToString() ?? "";

                // 更精准判断：必须是 NVIDIA 且 DeviceID 来自 PCI 总线（不是 HDAUDIO）
                if (deviceId.StartsWith("PCI\\VEN_10DE", StringComparison.OrdinalIgnoreCase))
                {
                    var devMatch = Regex.Match(deviceId, @"DEV_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    if (devMatch.Success)
                    {
                        return Convert.ToInt32(devMatch.Groups[1].Value, 16);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return 0;
    }

    public static string GetMacAddress()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                   nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)?
            .GetPhysicalAddress().ToString();
    }

    public static string GenerateMID()
    {
        var raw = GetBIOSSerial()
                + GetMotherboardSerial()
                + GetHDDSerial()
                + GetMacAddress();

        using var sha = SHA256.Create();
        byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

        BigInteger bigInt = new BigInteger([..hashBytes, 0]); // 防止负数
        string digits = BigInteger.Abs(bigInt).ToString();

        return digits.PadLeft(19, '0').Substring(0, 19);
    }

    public static string GetTimestamp()
    {
        var now = DateTime.Now;
        return $"{now:yyyy-MM-dd H:m:s:fff}";
    }


    public static string GetPcSign()
    {
        var machineId = new
        {
            av = "v1",
            bsn = GetBIOSSerial(),
            gid = GetGPUDeviceIdFromPnP(),
            hsn = GetHDDSerial() ?? "To Be Filled By O.E.M.",
            mac = "$" + GetMacAddress(),
            mid = GenerateMID(),
            msn = GetMotherboardSerial(),
            sv = "v2",
            ts = GetTimestamp()
        };

        string json = JsonConvert.SerializeObject(machineId);
        string base64urlPayload = ToBase64Url(json);
        string secret = "nt5FfJbdPzNcl2pkC3zgjO43Knvscxft";
        string signature = CreateHmac(base64urlPayload, secret);
        return base64urlPayload + "." + signature;
    }

    public static string ToBase64Url(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        string base64 = Convert.ToBase64String(bytes);
        string base64Url = base64.Split('=')[0];
        base64Url = base64Url.Replace('+', '-').Replace('/', '_');

        return base64Url;
    }

    public static string CreateHmac(string data, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Base64UrlEncode(hashBytes);
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        string base64 = Convert.ToBase64String(input);
        base64 = base64.Split('=')[0];
        base64 = base64.Replace('+', '-').Replace('/', '_');

        return base64;
    }
}

