using PlayniteExtensions.Common;
using System.Linq;
using Xunit;

namespace ViveportLibrary.Tests
{
    public class AppDataReaderTests
    {
        [Fact]
        public void GetInstalledApps_ReturnsInstalledApps()
        {
            var appDataReader = new AppDataReader("installed_apps.json");

            var installedApps = appDataReader.GetInstalledApps();

            Assert.Equal(2, installedApps.Count());
        }

        [Fact]
        public void GetInstalledApps_IgnoresMalformedEntries_WhenMalformedEntriesExist()
        {
            var appDataReader = new AppDataReader("installed_apps_malformed.json");

            var installedApps = appDataReader.GetInstalledApps();

            Assert.Equal(2, installedApps.Count());
            Assert.DoesNotContain(installedApps, x => x.Title == "Malformed Game");
        }

        [Fact]
        public void GetInstalledApps_ReturnsNull_WhenNoFileExists()
        {
            var appDataReader = new AppDataReader("non_existing_file.json");

            var installedApps = appDataReader.GetInstalledApps();

            Assert.Null(installedApps);
        }

        [Fact]
        public void GetInstalledApps_ReturnsNull_WhenFileIsEmpty()
        {
            var appDataReader = new AppDataReader("empty.json");

            var installedApps = appDataReader.GetInstalledApps();

            Assert.Null(installedApps);
        }

        [Fact]
        public void GetInstalledApps_Handles_Duplicate_Ids()
        {
            var appDataReader = new AppDataReader("installed_apps_duplicate.json");

            var installedApps = appDataReader.GetInstalledApps();

            var dict = installedApps.ToDictionarySafe(a => a.AppId);
        }
    }
}