using DZen.Security.Cryptography;
using EaLibrary.Services;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static System.Environment;

namespace EaLibrary.LocalDecryption;

public class EaProgramData
{
    private static Encoding encoding = Encoding.UTF8;

    public static string GetNSFileContents() => GetFileContents("d12ca10d54e24d15765c1a1c3961c6cb88d212d7616426a6094fb97d2d265657", "NS");

    public static string GetISFileContents() => GetFileContents("530c11479fe252fc5aabc24935b9776d4900eb3ba58fdc271e0d6229413ad40e", "IS");

    public static string GetFileContents(string foldername, string filename)
    {
        var filePath = Path.Combine(GetFolderPath(SpecialFolder.CommonApplicationData), "EA Desktop", foldername , filename);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("EA Desktop game list file not found", filePath);

        var allUsersGenericIdISBytes = encoding.GetBytes("allUsersGenericId" + filename);
        var hwInfo = GetHardwareInfoString();
        var iv = Sha3Hash(allUsersGenericIdISBytes).Take(16).ToArray();
        var key = Sha3Hash([.. allUsersGenericIdISBytes, .. Sha1Hash(hwInfo)]);

        //using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //fileStream.Seek(64, SeekOrigin.Begin);
        var fileBytes = File.ReadAllBytes(filePath).Skip(64).ToArray();
        using var fileStream = new MemoryStream(fileBytes);
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
        using var readStream = new StreamReader(cryptoStream);
        return readStream.ReadToEnd();
    }

    public static string GetHardwareInfoString()
    {
        var sb = new StringBuilder();
        sb.Append(HardwareInfo.GetWmiProperty("Win32_BaseBoard", "Manufacturer"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_BaseBoard", "SerialNumber"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_BIOS", "Manufacturer"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_BIOS", "SerialNumber"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetVolumeSerialNumber(@"C:\").ToString("X", CultureInfo.InvariantCulture));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_VideoController", "PNPDeviceId"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_Processor", "Manufacturer"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_Processor", "ProcessorId"));
        sb.Append(';');
        sb.Append(HardwareInfo.GetWmiProperty("Win32_Processor", "Name"));
        sb.Append(';');
        return sb.ToString();
    }

    private static byte[] Sha1Hash(string s)
    {
        var sha1 = SHA1.Create();
        var stringBytes = encoding.GetBytes(s);
        var hashBytes = sha1.ComputeHash(stringBytes);
        var outputString = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        return encoding.GetBytes(outputString);
    }

    private static byte[] Sha3Hash(byte[] input)
    {
        var sha3 = SHA3.Create();
        return sha3.ComputeHash(input);
    }
}
