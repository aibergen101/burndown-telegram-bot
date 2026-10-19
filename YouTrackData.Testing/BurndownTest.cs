using NUnit.Framework;
using YouTrackData;
using Models;
using System.Collections.Generic;

namespace BurndownTelegramBot.Tests
{
    public class BurndownCalculatorTests
    {
        private BurndownCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _calculator = new BurndownCalculator();
        }

        [Test]
        public void CalculateTotalStoryPoints_AllIssuesUnresolved_ProgressStaysFlat()
        {
            // Arrange
            var sprint = new Sprint
            {
                Start = new DateTimeOffset(DateTime.Now.AddDays(-2)).ToUnixTimeMilliseconds(),
                Finish = new DateTimeOffset(DateTime.Now.AddDays(8)).ToUnixTimeMilliseconds()
            };
            var issues = new[]
            {
                new Issue
                {
                    CustomFields = new List<IssueCustomField> { new IssueCustomField { Value = 5 } },
                    Resolved = null
                },
                new Issue
                {
                    CustomFields = new List<IssueCustomField> { new IssueCustomField { Value = 3 } },
                    Resolved = null
                }
            };

            // Act
            var result = _calculator.CalculateBurndown(sprint, issues);

            // Assert
            Assert.That(result.ProgressLine[0], Is.EqualTo(8)); 
            Assert.That(result.ProgressLine, Has.All.EqualTo(8)); 
        }

        [Test]
        public void CalculateTotalStoryPoints_SomeIssuesResolved_ProgressDecreases()
        {
            // Arrange
            var sprint = new Sprint
            {
                Start = new DateTimeOffset(DateTime.Now.AddDays(-2)).ToUnixTimeMilliseconds(),
                Finish = new DateTimeOffset(DateTime.Now.AddDays(8)).ToUnixTimeMilliseconds()
            };
            var issues = new[]
            {
                new Issue
                {
                    CustomFields = new List<IssueCustomField> { new IssueCustomField { Value = 5 } },
                    Resolved = new DateTimeOffset(DateTime.Now.AddDays(-1)).ToUnixTimeMilliseconds() 
                },
                new Issue
                {
                    CustomFields = new List<IssueCustomField> { new IssueCustomField { Value = 3 } },
                    Resolved = null 
                }
            };

            // Act
            var result = _calculator.CalculateBurndown(sprint, issues);

            // Assert
            Assert.That(result.ProgressLine[0], Is.EqualTo(8)); 
            Assert.That(result.ProgressLine[^1], Is.EqualTo(3)); 
        }
    }
}