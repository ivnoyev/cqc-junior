using Aquality.Selenium.Browsers;
using Aquality.Selenium.Core.Logging;
using ExampleProject.Framework.Config;
using ExampleProject.Framework.Pages;
using NUnit.Framework;

namespace ExampleProject.Tests
{
    public class BaseTest
    {
        protected MainPage mainPage = null!;
        protected Browser browser = null!;

        [SetUp]
        public void Setup()
        {
            AqualityServices.Get<Logger>().Info("Starting browser...");
            browser = AqualityServices.Browser;
            mainPage = new MainPage();
            browser.Maximize();
            browser.GoTo(Config.Get("url"));
        }

        [TearDown]
        public void Teardown()
        {
            AqualityServices.Get<Logger>().Info("Quitting browser...");
            browser.Quit();
        }
    }
}
