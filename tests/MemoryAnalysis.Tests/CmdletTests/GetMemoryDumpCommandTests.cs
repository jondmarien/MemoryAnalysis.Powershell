using Xunit;
using PowerShell.MemoryAnalysis.Cmdlets;
using PowerShell.MemoryAnalysis.Services;
using System.Management.Automation;

namespace MemoryAnalysis.Tests.CmdletTests
{
    public class GetMemoryDumpCommandTests
    {
        [Fact]
        public void Constructor_ShouldInitialize()
        {
            // Arrange & Act
            var cmdlet = new GetMemoryDumpCommand();

            // Assert
            Assert.NotNull(cmdlet);
        }

        [Fact]
        public void Path_Property_CanBeSet()
        {
            // Arrange
            var cmdlet = new GetMemoryDumpCommand();
            var testPath = @"C:\test\memory.dmp";

            // Act
            cmdlet.Path = testPath;

            // Assert
            Assert.Equal(testPath, cmdlet.Path);
        }

        [Fact]
        public void Path_Parameter_HasCorrectAttributes()
        {
            // Arrange
            var property = typeof(GetMemoryDumpCommand).GetProperty("Path");
            
            // Act
            var parameterAttribute = property?.GetCustomAttributes(typeof(ParameterAttribute), false)
                .FirstOrDefault() as ParameterAttribute;

            // Assert
            Assert.NotNull(parameterAttribute);
            Assert.True(parameterAttribute?.Mandatory);
            Assert.Equal(0, parameterAttribute?.Position);
        }

        [Fact]
        public void Validate_Parameter_IsSwitchParameter()
        {
            // Arrange
            var property = typeof(GetMemoryDumpCommand).GetProperty("Validate");
            
            // Assert
            Assert.NotNull(property);
            Assert.Equal(typeof(SwitchParameter), property.PropertyType);
        }

        [Fact]
        public void Cmdlet_HasCorrectCmdletAttribute()
        {
            // Arrange
            var cmdletAttribute = typeof(GetMemoryDumpCommand)
                .GetCustomAttributes(typeof(CmdletAttribute), false)
                .FirstOrDefault() as CmdletAttribute;

            // Assert
            Assert.NotNull(cmdletAttribute);
            Assert.Equal("Get", cmdletAttribute?.VerbName);
            Assert.Equal("MemoryDump", cmdletAttribute?.NounName);
        }
    }
}
