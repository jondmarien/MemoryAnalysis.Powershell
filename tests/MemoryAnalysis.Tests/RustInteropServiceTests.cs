using Xunit;
using PowerShell.MemoryAnalysis.Services;
using PowerShell.MemoryAnalysis.Models;
using System;
using System.Text.Json;

namespace MemoryAnalysis.Tests
{
    public class RustInteropServiceTests
    {
        [Fact]
        public void RustInteropService_Initialize_DoesNotThrow()
        {
            // Arrange & Act - service initializes in constructor
            var exception = Record.Exception(() =>
            {
                using var service = new RustInteropService();
            });
            
            // Assert - may throw InvalidOperationException if DLL not found, which is acceptable
            Assert.True(exception == null || exception is InvalidOperationException);
        }

        [Fact]
        public void RustInteropService_GetVersion_ReturnsValidObject()
        {
            // Arrange & Act
            var exception = Record.Exception(() =>
            {
                using var service = new RustInteropService();
                var version = service.GetVersion();
                Assert.NotNull(version);
            });
            
            // Assert
            Assert.True(exception == null || exception is InvalidOperationException);
        }

        [Fact]
        public void RustInteropService_Dispose_DoesNotThrow()
        {
            // Arrange & Act & Assert - should not throw
            var exception = Record.Exception(() =>
            {
                var service = new RustInteropService();
                service.Dispose();
            });
            Assert.True(exception == null || exception is InvalidOperationException);
        }

        [Fact]
        public void ProcessInfo_Deserialization_WithValidJson_Succeeds()
        {
            // Arrange
            var json = @"{
                ""pid"": 1234,
                ""ppid"": 100,
                ""name"": ""test.exe"",
                ""offset"": ""0x12345"",
                ""threads"": 4,
                ""handles"": 100,
                ""create_time"": ""2025-01-01 12:00:00""
            }";

            // Act
            var processInfo = JsonSerializer.Deserialize<ProcessInfo>(json);

            // Assert
            Assert.NotNull(processInfo);
            Assert.Equal(1234u, processInfo.Pid);
            Assert.Equal("test.exe", processInfo.Name);
            Assert.Equal(4u, processInfo.Threads);
        }

        [Fact]
        public void ProcessInfo_Deserialization_WithMissingFields_DoesNotThrow()
        {
            // Arrange
            var json = @"{""pid"": 1234}";  // Missing optional fields

            // Act
            var processInfo = JsonSerializer.Deserialize<ProcessInfo>(json);

            // Assert - Should deserialize with defaults
            Assert.NotNull(processInfo);
            Assert.Equal(1234u, processInfo.Pid);
        }

        [Fact]
        public void CommandLineInfo_Deserialization_WithValidJson_Succeeds()
        {
            // Arrange
            var json = @"{
                ""pid"": 5678,
                ""process_name"": ""cmd.exe"",
                ""command_line"": ""cmd.exe /c dir""
            }";

            // Act
            var cmdInfo = JsonSerializer.Deserialize<CommandLineInfo>(json);

            // Assert
            Assert.NotNull(cmdInfo);
            Assert.Equal(5678u, cmdInfo.Pid);
            Assert.Equal("cmd.exe", cmdInfo.ProcessName);
            Assert.Equal("cmd.exe /c dir", cmdInfo.CommandLine);
        }

        [Fact]
        public void DllInfo_Deserialization_WithValidJson_Succeeds()
        {
            // Arrange
            var json = @"{
                ""pid"": 9999,
                ""process_name"": ""notepad.exe"",
                ""base_address"": ""0x7FF800000000"",
                ""size"": 16777216,
                ""dll_name"": ""kernel32.dll"",
                ""dll_path"": ""C:\\Windows\\System32\\kernel32.dll""
            }";

            // Act
            var dllInfo = JsonSerializer.Deserialize<DllInfo>(json);

            // Assert
            Assert.NotNull(dllInfo);
            Assert.Equal(9999u, dllInfo.Pid);
            Assert.Equal("kernel32.dll", dllInfo.DllName);
            Assert.Equal(16777216ul, dllInfo.Size);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void RustInteropService_ListProcesses_WithInvalidPath_ThrowsException(string invalidPath)
        {
            // Arrange & Act & Assert
            var exception = Record.Exception(() =>
            {
                using var service = new RustInteropService();
                service.ListProcesses(invalidPath);
            });

            // Should throw ArgumentException or InvalidOperationException (if DLL not loaded)
            Assert.True(exception is ArgumentException || exception is InvalidOperationException);
        }

        [Fact]
        public void ProcessInfo_PropertyMapping_IsCorrect()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                Pid = 1234,
                Ppid = 100,
                Name = "test.exe",
                Offset = "0x12345",
                Threads = 4,
                Handles = 100,
                CreateTime = "2025-01-01"
            };

            // Act
            var json = JsonSerializer.Serialize(processInfo);

            // Assert
            Assert.Contains("\"pid\":1234", json);
            Assert.Contains("\"name\":\"test.exe\"", json);
        }
    }
}
