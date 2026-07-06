using System;
using Aquality.Selenium.Core.Applications;
using Aquality.Selenium.Browsers;
using System.Linq;
using Aquality.Selenium.Elements.Interfaces;
using Aquality.Selenium.Forms;
using Aquality.Selenium.Core.Logging;
using ExampleProject.Framework.Utils;
using OpenQA.Selenium;

namespace ExampleProject.Framework.Pages
{
    public class MainPage : Form
    {
        private const string ClassAttribute = "class";

        private IButton RandomOpponentButton => ElementFactory.GetButton(
            By.XPath("//*[contains(text(), 'random')]"), "Random opponent button");
        private IButton RandomiseShipsButton => ElementFactory.GetButton(
            By.XPath("//*[contains(text(), 'Randomise')]"), "Randomise ships button");
        private IButton PlayButton => ElementFactory.GetButton(
            By.XPath("//*[@class='battlefield-start-button']"), "Play button");

        private ILabel MoveOnNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__game-started-move-on') or contains(@class, 'notification__move-on')]"),
            "Move on notification");
        private ILabel MoveOffNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__game-started-move-off') or contains(@class, 'notification__move-off')]"),
            "Move off notification");
        private ILabel GameOverWinNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__game-over-win')]"), "Game over win notification");
        private ILabel GameOverLoseNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__game-over-lose')]"), "Game over lose notification");
        private ILabel RivalLeaveNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__rival-leave')]"), "Rival leave notification");
        private ILabel ErrorNotification => ElementFactory.GetLabel(
            By.XPath("//*[contains(@class, 'notification__server-error') or contains(@class, 'notification__game-error')]"),
            "Error notification");

        public MainPage() : base(By.XPath("//*[contains(text(), 'Place the ships')]"), "Main page")
        {
        }

        public void ClickRandomOpponent()
        {
            RandomOpponentButton.Click();
        }

        public void ClickRandomiseSeveralTimes(int clickCount)
        {
            for (int i = 0; i < clickCount; i++)
            {
                RandomiseShipsButton.Click();
            }
        }

        public void ClickPlayButton()
        {
            PlayButton.Click();
        }

        public bool IsMyTurn()
        {
            return MoveOnNotification.State.IsDisplayed;
        }

        public bool IsGameOver()
        {
            return GameOverWinNotification.State.IsDisplayed
                || GameOverLoseNotification.State.IsDisplayed
                || RivalLeaveNotification.State.IsDisplayed
                || ErrorNotification.State.IsDisplayed;
        }

        public bool IsGameWon()
        {
            return GameOverWinNotification.State.IsDisplayed;
        }

        public string GetEndReason()
        {
            if (GameOverWinNotification.State.IsDisplayed) return "Win";
            if (GameOverLoseNotification.State.IsDisplayed) return "Loss";
            if (RivalLeaveNotification.State.IsDisplayed) return "Rival left";
            if (ErrorNotification.State.IsDisplayed) return "Server/Game error";
            return "Unknown";
        }

        public void WaitForOpponentConnectionMessages()
        {
            AqualityServices.ConditionalWait.WaitFor(() =>
                MoveOnNotification.State.IsDisplayed
                || MoveOffNotification.State.IsDisplayed
                || RivalLeaveNotification.State.IsDisplayed);
        }

        public void WaitForTurnTransition()
        {
            AqualityServices.ConditionalWait.WaitFor(() =>
                MoveOnNotification.State.IsDisplayed
                || MoveOffNotification.State.IsDisplayed
                || GameOverWinNotification.State.IsDisplayed
                || GameOverLoseNotification.State.IsDisplayed
                || RivalLeaveNotification.State.IsDisplayed
                || ErrorNotification.State.IsDisplayed);
        }

        public void WaitForMyTurnOrGameOver()
        {
            AqualityServices.ConditionalWait.WaitFor(() => IsMyTurn() || IsGameOver());
        }

        public bool IsCellMiss(int x, int y)
        {
            var classes = GetRivalCellClasses(x, y);
            return classes.Contains("battlefield-cell__miss");
        }

        public bool IsCellHit(int x, int y)
        {
            var classes = GetRivalCellClasses(x, y);
            return classes.Contains("battlefield-cell__hit");
        }

        public bool IsCellKilled(int x, int y)
        {
            var classes = GetRivalCellClasses(x, y);
            return classes.Contains("battlefield-cell__done") || classes.Contains("battlefield-cell__killed");
        }

        public CellStatus GetRivalCellStatus(int x, int y)
        {
            var classes = GetRivalCellClasses(x, y);

            if (classes.Contains("battlefield-cell__done") || classes.Contains("battlefield-cell__killed"))
            {
                return CellStatus.Killed;
            }

            if (classes.Contains("battlefield-cell__hit"))
            {
                return CellStatus.Hit;
            }

            if (classes.Contains("battlefield-cell__miss"))
            {
                return CellStatus.Miss;
            }

            return CellStatus.Unknown;
        }

        public void FireAt(int x, int y)
        {
            Logger.Instance.Info($"Firing at {x},{y}");
            var cellLocator = GetRivalCellLocator(x, y);
            var cellButton = ElementFactory.GetButton(cellLocator, $"Rival cell {x},{y}");

            try
            {
                cellButton.Click();
            }
            catch (WebDriverException)
            {
                cellButton.JsActions.Click();
            }

            AqualityServices.ConditionalWait.WaitFor(() =>
            {
                var classes = GetRivalCellClasses(x, y);
                return classes.Contains("battlefield-cell__miss")
                    || classes.Contains("battlefield-cell__hit")
                    || classes.Contains("battlefield-cell__done")
                    || classes.Contains("battlefield-cell__killed");
            });
        }

        private string GetRivalCellClasses(int x, int y)
        {
            var cellLocator = GetRivalCellLocator(x, y);
            var cell = ElementFactory.GetLabel(cellLocator, $"Rival cell {x},{y}");
            return cell.GetAttribute(ClassAttribute) ?? string.Empty;
        }

        private By GetRivalCellLocator(int x, int y)
        {
            var rows = AqualityServices.Browser.Driver.FindElements(
                By.XPath("//div[contains(@class, 'battlefield__rival')]//table//tr"));

            int rowOffset = rows.Count > 10 ? 2 : 1;
            int colOffset = rows.Count > 10 ? 2 : 1;

            return By.XPath(
                $"//div[contains(@class, 'battlefield__rival')]//table//tr[{y + rowOffset}]//td[{x + colOffset}]");
        }
    }
}
