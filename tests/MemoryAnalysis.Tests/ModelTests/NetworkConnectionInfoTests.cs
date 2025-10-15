using Xunit;
using PowerShell.MemoryAnalysis.Models;
using System.Text.Json;

namespace MemoryAnalysis.Tests.ModelTests
{
    public class NetworkConnectionInfoTests
    {
        [Fact]
        public void NetworkConnectionInfo_Deserialization_WithValidJson_Succeeds()
        {
            // Arrange
            var json = @"{
                ""pid"": 1234,
                ""process_name"": ""chrome.exe"",
                ""local_address"": ""192.168.1.100"",
                ""local_port"": 54321,
                ""foreign_address"": ""142.250.185.78"",
                ""foreign_port"": 443,
                ""protocol"": ""TCP"",
                ""state"": ""ESTABLISHED"",
                ""created_time"": ""2025-01-01 12:00:00""
            }";

            // Act
            var connection = JsonSerializer.Deserialize<NetworkConnectionInfo>(json);

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(1234u, connection.Pid);
            Assert.Equal("chrome.exe", connection.ProcessName);
            Assert.Equal("192.168.1.100", connection.LocalAddress);
            Assert.Equal(54321, connection.LocalPort);
            Assert.Equal("142.250.185.78", connection.ForeignAddress);
            Assert.Equal(443, connection.ForeignPort);
            Assert.Equal("TCP", connection.Protocol);
            Assert.Equal("ESTABLISHED", connection.State);
            Assert.Equal("2025-01-01 12:00:00", connection.CreatedTime);
        }

        [Theory]
        [InlineData("ESTABLISHED")]
        [InlineData("LISTENING")]
        [InlineData("TIME_WAIT")]
        [InlineData("CLOSE_WAIT")]
        public void NetworkConnectionInfo_SupportsDifferentStates(string state)
        {
            // Arrange
            var json = $@"{{
                ""pid"": 1234,
                ""process_name"": ""test.exe"",
                ""local_address"": ""0.0.0.0"",
                ""local_port"": 80,
                ""foreign_address"": ""0.0.0.0"",
                ""foreign_port"": 0,
                ""protocol"": ""TCP"",
                ""state"": ""{state}"",
                ""created_time"": ""2025-01-01 12:00:00""
            }}";

            // Act
            var connection = JsonSerializer.Deserialize<NetworkConnectionInfo>(json);

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(state, connection.State);
        }

        [Theory]
        [InlineData("TCP")]
        [InlineData("UDP")]
        [InlineData("TCPv6")]
        [InlineData("UDPv6")]
        public void NetworkConnectionInfo_SupportsDifferentProtocols(string protocol)
        {
            // Arrange
            var json = $@"{{
                ""pid"": 1234,
                ""process_name"": ""test.exe"",
                ""local_address"": ""0.0.0.0"",
                ""local_port"": 80,
                ""foreign_address"": ""0.0.0.0"",
                ""foreign_port"": 0,
                ""protocol"": ""{protocol}"",
                ""state"": ""LISTENING"",
                ""created_time"": ""2025-01-01 12:00:00""
            }}";

            // Act
            var connection = JsonSerializer.Deserialize<NetworkConnectionInfo>(json);

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(protocol, connection.Protocol);
        }

        [Fact]
        public void NetworkConnectionInfo_Serialization_ProducesCorrectJson()
        {
            // Arrange
            var connection = new NetworkConnectionInfo
            {
                Pid = 5678,
                ProcessName = "firefox.exe",
                LocalAddress = "127.0.0.1",
                LocalPort = 8080,
                ForeignAddress = "1.2.3.4",
                ForeignPort = 443,
                Protocol = "TCP",
                State = "ESTABLISHED",
                CreatedTime = "2025-01-02 14:30:00"
            };

            // Act
            var json = JsonSerializer.Serialize(connection);

            // Assert
            Assert.Contains("\"pid\":5678", json);
            Assert.Contains("\"process_name\":\"firefox.exe\"", json);
            Assert.Contains("\"local_address\":\"127.0.0.1\"", json);
            Assert.Contains("\"local_port\":8080", json);
            Assert.Contains("\"protocol\":\"TCP\"", json);
        }
    }
}
