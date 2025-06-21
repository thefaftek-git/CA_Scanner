
using Xunit;
using ConditionalAccessExporter.Services;
using System.Globalization;

namespace ConditionalAccessExporter.Tests
{
    public class PolicyComparisonServiceDateTests
    {
        [Theory]
        [InlineData("2024-01-01T12:00:00Z", "2024-01-01T13:00:00Z", true)] // Same date, different time
        [InlineData("2024-01-01T12:00:00Z", "2024-01-02T12:00:00Z", false)] // Different dates
        [InlineData("2024-01-01T12:00:00.000Z", "2024-01-01T12:00:00Z", true)] // Same date, different precision
        [InlineData("MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", false)] // Different formats but same date
        public void AreEquivalentDates_ShouldCompareOnlyDateParts(string dateStr1, string dateStr2, bool expectedResult)
        {
            var result = PolicyComparisonService.AreEquivalentDates(dateStr1, dateStr2);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
