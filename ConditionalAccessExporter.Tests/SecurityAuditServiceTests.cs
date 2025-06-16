

using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace ConditionalAccessExporter.Tests
{
    public class SecurityAuditServiceTests : IDisposable
    {
        private readonly Mock<ILogger<SecurityAuditService>> _mockLogger;
        private readonly Mock<ILoggingService> _mockLoggingService;
        private readonly MockFileSystem _mockFileSystem;
        private readonly SecurityAuditService _securityAuditService;
        private readonly string _tempDirectory;

        public SecurityAuditServiceTests()
        {
            _mockLogger = new Mock<ILogger<SecurityAuditService>>();
            _mockLoggingService = new Mock<ILoggingService>();
            _mockFileSystem = new MockFileSystem();
            _tempDirectory = GetSanitizedTempPath();

            // Setup test directories with sanitized paths
            SetupTestDirectories();

            _securityAuditService = new SecurityAuditService(_mockLogger.Object, _mockLoggingService.Object);
        }

        private string GetSanitizedTempPath()
        {
            var tempPath = Path.GetTempPath();
            // Ensure the path is properly normalized and validated
            return Path.GetFullPath(tempPath);
        }

        /// <summary>
        /// Safely combines path components for testing and validates the result to prevent path traversal attacks
        /// </summary>
        private static string GetSanitizedTestPath(params string[] pathComponents)
        {
            if (pathComponents == null || pathComponents.Length == 0)
                throw new ArgumentException("Path components cannot be null or empty", nameof(pathComponents));

            // Filter out null or empty components
            var validComponents = pathComponents.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (validComponents.Length == 0)
                throw new ArgumentException("No valid path components provided", nameof(pathComponents));

            // Combine paths safely
            var combinedPath = Path.Join(validComponents);
            
            // Get the full path to normalize it and prevent traversal attacks
            var fullPath = Path.GetFullPath(combinedPath);
            
            // Additional validation to ensure the path doesn't contain dangerous sequences
            var normalizedPath = Path.GetFullPath(fullPath);
            if (normalizedPath.Contains("..") || normalizedPath.Contains("~"))
            {
                throw new InvalidOperationException("Path contains potentially dangerous sequences");
            }
            
            return normalizedPath;
        }

        private void SetupTestDirectories()
        {
            // Create test directories with validated paths
            var securityEventsPath = GetSanitizedTestPath(_tempDirectory, "logs", "security-audit", "security-events");
            var accessEventsPath = GetSanitizedTestPath(_tempDirectory, "logs", "security-audit", "access-events");
            var complianceEventsPath = GetSanitizedTestPath(_tempDirectory, "logs", "compliance", "compliance-events");

            _mockFileSystem.AddDirectory(securityEventsPath);
            _mockFileSystem.AddDirectory(accessEventsPath);
            _mockFileSystem.AddDirectory(complianceEventsPath);
        }

        [Fact]
        public async Task LogSecurityEventAsync_ShouldLogSecurityEvent_WhenValidEventProvided()
        {
            // Arrange
            var securityEvent = new SecurityEvent
            {
                EventType = "AuthenticationFailure",
                Severity = SecurityEventSeverity.High,
                Description = "Failed authentication attempt",
                Source = "Authentication Service",
                UserId = "test@example.com"
            };

            // Act
            await _securityAuditService.LogSecurityEventAsync(securityEvent);

            // Assert
            Assert.NotNull(securityEvent.EventId);
            Assert.NotEqual(default(DateTime), securityEvent.Timestamp);
            Assert.NotNull(securityEvent.Hash);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogSecurityEventAsync_ShouldThrowException_WhenNullEventProvided()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _securityAuditService.LogSecurityEventAsync(null!));
        }

        [Fact]
        public async Task LogComplianceEventAsync_ShouldLogComplianceEvent_WhenValidEventProvided()
        {
            // Arrange
            var complianceEvent = new ComplianceEvent
            {
                ComplianceStandard = ComplianceStandard.SOC2,
                ControlId = "CC1.1",
                ControlDescription = "Access control policy",
                ComplianceStatus = ComplianceStatus.Compliant,
                Evidence = "Access control policy implemented and tested",
                AssessmentDate = DateTime.UtcNow
            };

            // Act
            await _securityAuditService.LogComplianceEventAsync(complianceEvent);

            // Assert
            Assert.NotNull(complianceEvent.EventId);
            Assert.NotEqual(default(DateTime), complianceEvent.Timestamp);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Compliance event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogAccessEventAsync_ShouldLogAccessEvent_WhenValidEventProvided()
        {
            // Arrange
            var accessEvent = new AccessEvent
            {
                UserId = "test@example.com",
                Action = "PolicyExport",
                ResourceAccessed = "ConditionalAccessPolicies",
                Success = true,
                IpAddress = "192.168.1.100",
                UserAgent = "CA_Scanner/1.0"
            };

            // Act
            await _securityAuditService.LogAccessEventAsync(accessEvent);

            // Assert
            Assert.NotNull(accessEvent.EventId);
            Assert.NotEqual(default(DateTime), accessEvent.Timestamp);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Access event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogVulnerabilityDetectionAsync_ShouldLogBothSecurityAndComplianceEvents()
        {
            // Arrange
            var vulnerabilityEvent = new VulnerabilityEvent
            {
                VulnerabilityId = "CVE-2024-1234",
                Description = "High severity vulnerability in dependency",
                Severity = VulnerabilitySeverity.High,
                CvssScore = 7.5,
                AffectedComponent = "TestComponent",
                Source = "DependencyScanner",
                RecommendedAction = "Update to version 2.0.1"
            };

            // Act
            await _securityAuditService.LogVulnerabilityDetectionAsync(vulnerabilityEvent);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Compliance event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogSecurityConfigurationChangeAsync_ShouldLogConfigurationChange()
        {
            // Arrange
            var configEvent = new ConfigurationChangeEvent
            {
                ConfigurationItem = "AuthenticationSettings",
                OldValue = "SingleFactor",
                NewValue = "MultiFactor",
                UserId = "admin@example.com",
                Source = "AdminPortal",
                ChangeReason = "Security enhancement",
                RequiresApproval = true,
                IsApproved = true
            };

            // Act
            await _securityAuditService.LogSecurityConfigurationChangeAsync(configEvent);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateAuditReportAsync_ShouldGenerateComprehensiveReport()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-30);
            var toDate = DateTime.UtcNow;

            // Act
            var report = await _securityAuditService.GenerateAuditReportAsync(fromDate, toDate);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.ReportId);
            Assert.Equal(fromDate, report.PeriodStart);
            Assert.Equal(toDate, report.PeriodEnd);
            Assert.NotEqual(default(DateTime), report.GeneratedAt);
            Assert.NotNull(report.SecurityEventsByType);
            Assert.NotNull(report.ComplianceByStandard);
            Assert.NotNull(report.TopAccessedResources);
            Assert.NotNull(report.SecurityTrends);
            Assert.NotNull(report.Recommendations);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating security audit report")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateComplianceReportAsync_ShouldGenerateComplianceSpecificReport()
        {
            // Arrange
            var standard = ComplianceStandard.SOC2;

            // Act
            var report = await _securityAuditService.GenerateComplianceReportAsync(standard);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.ReportId);
            Assert.Equal(standard, report.ComplianceStandard);
            Assert.NotEqual(default(DateTime), report.GeneratedAt);
            Assert.True(report.CompliancePercentage >= 0 && report.CompliancePercentage <= 100);
            Assert.NotNull(report.ControlDetails);
            Assert.NotNull(report.RecentEvents);
            Assert.NotNull(report.Recommendations);
            Assert.NotNull(report.Metrics);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating compliance report")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSecurityEventsAsync_ShouldReturnFilteredEvents()
        {
            // Arrange
            var filter = new SecurityEventFilter
            {
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow,
                MinimumSeverity = SecurityEventSeverity.Medium,
                MaxResults = 100
            };

            // Act
            var events = await _securityAuditService.GetSecurityEventsAsync(filter);

            // Assert
            Assert.NotNull(events);
            // Events list should be empty for this test setup, but method should not throw
        }

        [Fact]
        public async Task ValidateSecurityComplianceAsync_ShouldCompleteWithoutErrors()
        {
            // Act & Assert - Should not throw any exceptions
            await _securityAuditService.ValidateSecurityComplianceAsync();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting security compliance validation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security compliance validation completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ArchiveOldAuditLogsAsync_ShouldArchiveLogsOlderThanRetentionPeriod()
        {
            // Arrange
            var retentionPeriod = TimeSpan.FromDays(90);

            // Act & Assert - Should not throw any exceptions
            await _securityAuditService.ArchiveOldAuditLogsAsync(retentionPeriod);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Archived")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(SecurityEventSeverity.Critical)]
        [InlineData(SecurityEventSeverity.High)]
        [InlineData(SecurityEventSeverity.Medium)]
        [InlineData(SecurityEventSeverity.Low)]
        [InlineData(SecurityEventSeverity.Info)]
        public async Task LogSecurityEventAsync_ShouldHandleAllSeverityLevels(SecurityEventSeverity severity)
        {
            // Arrange
            var securityEvent = new SecurityEvent
            {
                EventType = "TestEvent",
                Severity = severity,
                Description = $"Test event with {severity} severity",
                Source = "TestSource"
            };

            // Act
            await _securityAuditService.LogSecurityEventAsync(securityEvent);

            // Assert
            Assert.Equal(severity, securityEvent.Severity);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(ComplianceStandard.SOC2)]
        [InlineData(ComplianceStandard.ISO27001)]
        [InlineData(ComplianceStandard.NIST)]
        [InlineData(ComplianceStandard.OWASP)]
        public async Task GenerateComplianceReportAsync_ShouldHandleAllComplianceStandards(ComplianceStandard standard)
        {
            // Act
            var report = await _securityAuditService.GenerateComplianceReportAsync(standard);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(standard, report.ComplianceStandard);
        }

        [Fact]
        public void SecurityEvent_ShouldHaveProperDefaultValues()
        {
            // Arrange & Act
            var securityEvent = new SecurityEvent();

            // Assert
            Assert.Equal(string.Empty, securityEvent.EventId);
            Assert.Equal(string.Empty, securityEvent.EventType);
            Assert.Equal(string.Empty, securityEvent.Description);
            Assert.Equal(string.Empty, securityEvent.Source);
            Assert.Equal(SecurityEventSeverity.Info, securityEvent.Severity);
            Assert.False(securityEvent.IsResolved);
        }

        [Fact]
        public void ComplianceEvent_ShouldHaveProperDefaultValues()
        {
            // Arrange & Act
            var complianceEvent = new ComplianceEvent();

            // Assert
            Assert.Equal(string.Empty, complianceEvent.EventId);
            Assert.Equal(string.Empty, complianceEvent.ControlId);
            Assert.Equal(string.Empty, complianceEvent.ControlDescription);
            Assert.Equal(string.Empty, complianceEvent.Evidence);
            Assert.Equal(ComplianceStandard.SOC2, complianceEvent.ComplianceStandard);
            Assert.Equal(ComplianceStatus.Compliant, complianceEvent.ComplianceStatus);
        }

        [Fact]
        public void VulnerabilityEvent_ShouldHaveProperDefaultValues()
        {
            // Arrange & Act
            var vulnerabilityEvent = new VulnerabilityEvent();

            // Assert
            Assert.Equal(string.Empty, vulnerabilityEvent.VulnerabilityId);
            Assert.Equal(string.Empty, vulnerabilityEvent.Description);
            Assert.Equal(string.Empty, vulnerabilityEvent.AffectedComponent);
            Assert.Equal(string.Empty, vulnerabilityEvent.Version);
            Assert.Equal(string.Empty, vulnerabilityEvent.Source);
            Assert.Equal(string.Empty, vulnerabilityEvent.RecommendedAction);
            Assert.Equal(VulnerabilitySeverity.Info, vulnerabilityEvent.Severity);
            Assert.Equal(0, vulnerabilityEvent.CvssScore);
            Assert.False(vulnerabilityEvent.HasExploit);
            Assert.False(vulnerabilityEvent.IsExploitActive);
            Assert.NotNull(vulnerabilityEvent.References);
            Assert.NotNull(vulnerabilityEvent.AffectedPlatforms);
        }

        [Fact]
        public async Task SecurityAuditService_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SecurityAuditService(null!, _mockLoggingService.Object));
        }

        [Fact]
        public async Task SecurityAuditService_ShouldThrowArgumentNullException_WhenLoggingServiceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SecurityAuditService(_mockLogger.Object, null!));
        }

        public void Dispose()
        {
            // MockFileSystem doesn't implement IDisposable in this version
            // No cleanup needed for mock file system
        }
    }

    /// <summary>
    /// Integration tests for SecurityAuditService that test end-to-end scenarios
    /// </summary>
    public class SecurityAuditServiceIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<SecurityAuditService>> _mockLogger;
        private readonly Mock<ILoggingService> _mockLoggingService;
        private readonly SecurityAuditService _securityAuditService;
        private readonly string _tempTestDirectory;

        public SecurityAuditServiceIntegrationTests()
        {
            _mockLogger = new Mock<ILogger<SecurityAuditService>>();
            _mockLoggingService = new Mock<ILoggingService>();
            _tempTestDirectory = GetSanitizedTestPath(Path.GetTempPath(), "SecurityAuditTests", Guid.NewGuid().ToString());
            
            Directory.CreateDirectory(_tempTestDirectory);
            Environment.CurrentDirectory = _tempTestDirectory;

            _securityAuditService = new SecurityAuditService(_mockLogger.Object, _mockLoggingService.Object);
        }

        /// <summary>
        /// Safely combines path components for testing and validates the result to prevent path traversal attacks
        /// </summary>
        private static string GetSanitizedTestPath(params string[] pathComponents)
        {
            if (pathComponents == null || pathComponents.Length == 0)
                throw new ArgumentException("Path components cannot be null or empty", nameof(pathComponents));

            // Filter out null or empty components
            var validComponents = pathComponents.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (validComponents.Length == 0)
                throw new ArgumentException("No valid path components provided", nameof(pathComponents));

            // Combine paths safely
            var combinedPath = Path.Join(validComponents);
            
            // Get the full path to normalize it and prevent traversal attacks
            var fullPath = Path.GetFullPath(combinedPath);
            
            // Additional validation to ensure the path doesn't contain dangerous sequences
            var normalizedPath = Path.GetFullPath(fullPath);
            if (normalizedPath.Contains("..") || normalizedPath.Contains("~"))
            {
                throw new InvalidOperationException("Path contains potentially dangerous sequences");
            }
            
            return normalizedPath;
        }

        [Fact]
        public async Task EndToEndSecurityWorkflow_ShouldProcessCompleteSecurityScenario()
        {
            // Arrange - Create a complete security scenario
            var vulnerabilityEvent = new VulnerabilityEvent
            {
                VulnerabilityId = "CVE-2024-TEST",
                Description = "Test vulnerability for integration testing",
                Severity = VulnerabilitySeverity.High,
                CvssScore = 8.5,
                AffectedComponent = "TestLibrary",
                Source = "IntegrationTest",
                RecommendedAction = "Update immediately"
            };

            var configChangeEvent = new ConfigurationChangeEvent
            {
                ConfigurationItem = "SecuritySettings",
                OldValue = "Basic",
                NewValue = "Enhanced",
                UserId = "admin@test.com",
                Source = "IntegrationTest",
                ChangeReason = "Security improvement"
            };

            var accessEvent = new AccessEvent
            {
                UserId = "user@test.com",
                Action = "PolicyAccess",
                ResourceAccessed = "ConditionalAccessPolicies",
                Success = true,
                IpAddress = "192.168.1.1"
            };

            // Act - Process the complete workflow
            await _securityAuditService.LogVulnerabilityDetectionAsync(vulnerabilityEvent);
            await _securityAuditService.LogSecurityConfigurationChangeAsync(configChangeEvent);
            await _securityAuditService.LogAccessEventAsync(accessEvent);

            // Generate reports
            var auditReport = await _securityAuditService.GenerateAuditReportAsync(
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(1));

            var complianceReport = await _securityAuditService.GenerateComplianceReportAsync(ComplianceStandard.SOC2);

            // Run compliance validation
            await _securityAuditService.ValidateSecurityComplianceAsync();

            // Assert - Verify complete workflow
            Assert.NotNull(auditReport);
            Assert.NotEmpty(auditReport.ReportId);
            Assert.NotNull(complianceReport);
            Assert.NotEmpty(complianceReport.ReportId);

            // Verify logging calls were made
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("logged")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(6)); // At least 6 log calls for the events and reports
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempTestDirectory))
                {
                    Directory.Delete(_tempTestDirectory, true);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore access denied errors during test cleanup
            }
            catch (IOException)
            {
                // Ignore I/O errors during test cleanup - files may be locked or directory doesn't exist
            }
        }
    }
}


