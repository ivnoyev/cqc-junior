using Aquality.Selenium.Browsers;
using Aquality.Selenium.Core.Logging;
using NUnit.Framework;
using System;
using ExampleProject.Framework.Utils;

namespace ExampleProject.Tests
{
    public class BattleshipGameTest : BaseTest
    {
        [Test]
        public void PlayBattleshipWithHuntAndTargetStrategy()
        {
            AqualityServices.Get<Logger>().Info("Starting Battleship auto-play test...");

            var bot = new GameBot(mainPage);

            int randomPlacements = new Random().Next(1, 16);
            const int maxIterations = 250;
            bot.Play(randomPlacements, maxIterations);

            string reason = mainPage.GetEndReason();
            AqualityServices.Get<Logger>().Info($"Game over. Outcome: {reason}");

            if (mainPage.IsGameWon())
            {
                Assert.Pass("We won the game!");
            }
            else
            {
                Assert.Fail($"The game was not won. Reason: {reason}");
            }
        }
    }
}
