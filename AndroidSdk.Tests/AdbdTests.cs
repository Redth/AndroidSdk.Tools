using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
    public class Adbd_Tests : TestsBase
    {
        public Adbd_Tests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        { }

        [Fact]
        public async Task GetVersion()
        {
            var adbclient = new AdbdClient();

            await adbclient.ConnectAsync();

            var v = await adbclient.GetHostVersionAsync();
            
            Assert.True(v != null);
        }
    }
}
