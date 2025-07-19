﻿using GamesSizeCalculator.SteamSizeCalculation;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GamesSizeCalculator.Tests.Steam;

public class SteamSizeCalculatorTests
{
    [Fact]
    public async Task MasterChiefCollection()
    {
        ulong expectedBaseSize = 14715108044L;

        ulong expectedDLCsize =
            9880606811L +
            5364014009L +
            31248440761L +
            21063599446L +
            8582825401L +
            9088632434L +
            9475667570L +
            8546806807L +
            25369702824L +
            21123601548L +
            10099205546L +
            8229782954L +
            1467914176L +
            933914112L +
            564810752L +
            1899134368L +
            1542698112L +
            3724939335L;

        var calc = Setup(976730, out var settings);
        var completeSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());

        settings.IncludeDlcInSteamCalculation = false;
        settings.IncludeOptionalInSteamCalculation = false;
        var baseSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedBaseSize + expectedDLCsize, completeSize);
        Assert.Equal(expectedBaseSize, baseSize);
    }

    [Fact]
    public async Task Watch_Dogs()
    {
        ulong expectedBaseSize =
            1790331721L + //Watch Dogs English
            12807162011L; //Watch Dogs Common

        ulong expectedDLCsize =
            28037350L + //Watch_Dogs - DLC 0 (293055) Depot
            30841083L + //Watch_Dogs - DLC 1 (293057) Depot
            1147L + //Watch_Dogs - DLC 2 (293059) Depot
            3718567700L + //Watch_Dogs - DLC 3 (293061) Depot
            525L; //Watch_Dogs Season Pass Uplay Activation (293054) Depot

        var calc = Setup(243470, out var settings);

        var completeSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedBaseSize + expectedDLCsize, completeSize);

        settings.IncludeDlcInSteamCalculation = false;
        settings.IncludeOptionalInSteamCalculation = false;
        var baseSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedBaseSize, baseSize);
    }

    [Fact]
    public async Task AssassinsCreedBrotherhood()
    {
        ulong normal = 9529023303L;
        ulong optional = 1954919585L;
        ulong worldwide = 7993844118L;

        var calc = Setup(48190, out var settings);

        var completeSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(normal + optional + worldwide, completeSize.Value);

        settings.IncludeDlcInSteamCalculation = false;
        settings.IncludeOptionalInSteamCalculation = false;
        var minimalSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(normal + worldwide, minimalSize.Value);
    }

    [Fact]
    public async Task MissingDepotsReturnNull()
    {
        //The Door (unreleased)
        Assert.Null(await Setup(1360440, out _).GetInstallSizeAsync(new Playnite.SDK.Models.Game()));

        //Castle Crashers
        Assert.Null(await Setup(204360, out _).GetInstallSizeAsync(new Playnite.SDK.Models.Game()));

        //Two Point Hospital
        Assert.Null(await Setup(535930, out _).GetInstallSizeAsync(new Playnite.SDK.Models.Game()));

        //PC Building Simulator
        Assert.Null(await Setup(621060, out _).GetInstallSizeAsync(new Playnite.SDK.Models.Game()));
    }

    [Fact]
    public async Task Metro2033()
    {
        //For some reason, every depot for Metro 2033 is marked optional
        ulong expectedSize = 5028692647L + 2884284795L;

        var calc = Setup(43110, out var settings);

        var completeSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedSize, completeSize);

        settings.IncludeDlcInSteamCalculation = false;
        settings.IncludeOptionalInSteamCalculation = false;
        var minimalSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedSize, minimalSize);
    }

    [Fact]
    public async Task MeganTheeStallion()
    {
        //This depot has no maxsize properties
        ulong expectedSize = 6301203201UL;

        var calc = Setup(2269330, out var settings);

        var completeSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedSize, completeSize.Value);

        settings.IncludeDlcInSteamCalculation = false;
        settings.IncludeOptionalInSteamCalculation = false;
        var minimalSize = await calc.GetInstallSizeAsync(new Playnite.SDK.Models.Game());
        Assert.Equal(expectedSize, minimalSize.Value);
    }

    //[Theory]
    //[InlineData(243470u)]
    //[InlineData(48190u)]
    //[InlineData(1360440u)]
    //[InlineData(204360u)] //Castle Crashers
    //[InlineData(621060u)] //PC Building Simulator
    //[InlineData(43110u)] //Metro 2033
    //[InlineData(535930u)] //Two Point Hospital
    //[InlineData(24980u)] //Mass Effect 2 (2010)
    //[InlineData(24999u)] //Mass Effect 2 - guide, but only registered as Mass Effect 2 for some reason
    public async Task Serialize(uint appId)
    {
        SteamApiClient client = new();
        var productInfo = await client.GetProductInfo(appId);
        File.WriteAllText($@"D:\code\{appId}.json", Newtonsoft.Json.JsonConvert.SerializeObject(productInfo));
    }

    private SteamSizeCalculator Setup(uint appId, out GamesSizeCalculatorSettings settings)
    {
        settings = new GamesSizeCalculatorSettings()
        {
            GetUninstalledGameSizeFromSteam = true,
            GetSizeFromSteamNonSteamGames = true,
            IncludeDlcInSteamCalculation = true,
            IncludeOptionalInSteamCalculation = true,
        };
        var calc = new SteamSizeCalculator(new FakeSteamApiClient(), new FakeSteamAppIdUtility(appId), settings);
        return calc;
    }
}