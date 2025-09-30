using Xunit;
using EaLibrary.Services;
using EaLibrary.LocalDecryption;

namespace EaLibrary.Tests;

public class PcSignHashTests
{
    [Fact]
    public void VerifyHash()
    {
        var hwi = new HardwareInfo
        {
            av = "v1",
            sv = "v1",
            ts = "2025-5-7 16:44:40:822",
        };

        hwi.SetMidField();

        //var mid = EaLibrary.Services.PcSign2.HardwareInfo.CalculateFnv1aHash(hwi.bsn, hwi.gid, hwi.hsn, hwi.msn);
        //mid = EAAppEmulater.HardwareInfo.GenerateMID();


        var signature = PcSignature.GetPcSignature(hwi);
    }
}

public class ProgramDataTests
{
    [Fact]
    public void GetISContent()
    {
        var content = EaProgramData.GetNSFileContents();
    }
}