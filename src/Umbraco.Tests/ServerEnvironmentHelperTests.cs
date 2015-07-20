using System.Configuration;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Logging;
using Umbraco.Core.Profiling;
using Umbraco.Core.Sync;
using Umbraco.Tests.TestHelpers;

namespace Umbraco.Tests
{
    [TestFixture]
    public class ServerEnvironmentHelperTests
    {

        // note: in tests, read appContext._umbracoApplicationUrl and not the property,
        // because reading the property does run some code, as long as the field is null.

        [Test]
        public void SetApplicationUrlWhenNoSettings()
        {
            var appContext = new ApplicationContext(null)
            {
                UmbracoApplicationUrl = null // NOT set
            };

            ConfigurationManager.AppSettings.Set("umbracoUseSSL", "true"); // does not make a diff here

            ServerEnvironmentHelper.TrySetApplicationUrlFromSettings(appContext,
                Mock.Of<IUmbracoSettingsSection>(
                    section =>
                        section.DistributedCall == Mock.Of<IDistributedCallSection>(callSection => callSection.Servers == Enumerable.Empty<IServer>())
                        && section.WebRouting == Mock.Of<IWebRoutingSection>(wrSection => wrSection.UmbracoApplicationUrl == (string) null)
                        && section.ScheduledTasks == Mock.Of<IScheduledTasksSection>()));


            // still NOT set
            Assert.IsNull(appContext._umbracoApplicationUrl);
        }

        [Test]
        public void SetApplicationUrlFromDcSettingsNoSsl()
        {
            var appContext = new ApplicationContext(null);

            ConfigurationManager.AppSettings.Set("umbracoUseSSL", "false");

            ServerEnvironmentHelper.TrySetApplicationUrlFromSettings(appContext,
                Mock.Of<IUmbracoSettingsSection>(
                    section =>
                        section.DistributedCall == Mock.Of<IDistributedCallSection>(callSection => callSection.Servers == Enumerable.Empty<IServer>())
                        && section.WebRouting == Mock.Of<IWebRoutingSection>(wrSection => wrSection.UmbracoApplicationUrl == (string) null)
                        && section.ScheduledTasks == Mock.Of<IScheduledTasksSection>(tasksSection => tasksSection.BaseUrl == "mycoolhost.com/hello/world/")));


            Assert.AreEqual("http://mycoolhost.com/hello/world", appContext._umbracoApplicationUrl);
        }

        [Test]
        public void SetApplicationUrlFromDcSettingsSsl()
        {
            var appContext = new ApplicationContext(null);

            ConfigurationManager.AppSettings.Set("umbracoUseSSL", "true");

            ServerEnvironmentHelper.TrySetApplicationUrlFromSettings(appContext,
                Mock.Of<IUmbracoSettingsSection>(
                    section =>
                        section.DistributedCall == Mock.Of<IDistributedCallSection>(callSection => callSection.Servers == Enumerable.Empty<IServer>())
                        && section.WebRouting == Mock.Of<IWebRoutingSection>(wrSection => wrSection.UmbracoApplicationUrl == (string) null)
                        && section.ScheduledTasks == Mock.Of<IScheduledTasksSection>(tasksSection => tasksSection.BaseUrl == "mycoolhost.com/hello/world")));


            Assert.AreEqual("https://mycoolhost.com/hello/world", appContext._umbracoApplicationUrl);
        }

        [Test]
        public void SetApplicationUrlFromWrSettingsSsl()
        {
            var appContext = new ApplicationContext(null);

            ConfigurationManager.AppSettings.Set("umbracoUseSSL", "true"); // does not make a diff here

            ServerEnvironmentHelper.TrySetApplicationUrlFromSettings(appContext,
                Mock.Of<IUmbracoSettingsSection>(
                    section =>
                        section.DistributedCall == Mock.Of<IDistributedCallSection>(callSection => callSection.Servers == Enumerable.Empty<IServer>())
                        && section.WebRouting == Mock.Of<IWebRoutingSection>(wrSection => wrSection.UmbracoApplicationUrl == "httpx://whatever.com/hello/world/")
                        && section.ScheduledTasks == Mock.Of<IScheduledTasksSection>(tasksSection => tasksSection.BaseUrl == "mycoolhost.com/hello/world")));


            Assert.AreEqual("httpx://whatever.com/hello/world", appContext._umbracoApplicationUrl);
        }
    }
}