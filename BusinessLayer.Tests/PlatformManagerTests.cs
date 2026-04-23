using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using Moq;
using Xunit;

namespace BusinessLayer.Tests
{
    /// <summary>
    /// Unit tests for the PlatformManager class.
    /// Tests cover adapter retrieval and error handling for different platform types.
    /// </summary>
    public class PlatformManagerTests
    {
        private readonly Mock<IVirtualizationAdapter> _mockCMLAdapter;
        private readonly Mock<IVirtualizationAdapter> _mockEVEAdapter;
        private readonly PlatformManager _platformManager;

        public PlatformManagerTests()
        {
            _mockCMLAdapter = new Mock<IVirtualizationAdapter>();
            _mockCMLAdapter.Setup(a => a.PlatformName).Returns(PlatformType.CML);

            _mockEVEAdapter = new Mock<IVirtualizationAdapter>();
            _mockEVEAdapter.Setup(a => a.PlatformName).Returns(PlatformType.EVE);

            var adapters = new List<IVirtualizationAdapter>
            {
                _mockCMLAdapter.Object,
                _mockEVEAdapter.Object
            };

            _platformManager = new PlatformManager(adapters);
        }

        #region GetAdapter Tests

        [Fact]
        public void GetAdapter_WithCMLPlatform_ReturnsCMLAdapter()
        {
            // Act
            var result = _platformManager.GetAdapter(PlatformType.CML);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PlatformType.CML, result.PlatformName);
            Assert.Same(_mockCMLAdapter.Object, result);
        }

        [Fact]
        public void GetAdapter_WithEVEPlatform_ReturnsEVEAdapter()
        {
            // Act
            var result = _platformManager.GetAdapter(PlatformType.EVE);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PlatformType.EVE, result.PlatformName);
            Assert.Same(_mockEVEAdapter.Object, result);
        }

        [Fact]
        public void GetAdapter_WithUnknownPlatform_ThrowsException()
        {
            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _platformManager.GetAdapter(PlatformType.Unknown));
            Assert.Contains("Unknown or unsupported platform", exception.Message);
        }

        [Fact]
        public void GetAdapter_WithNoPlatformAdapters_ThrowsException()
        {
            // Arrange
            var emptyAdapters = new List<IVirtualizationAdapter>();
            var manager = new PlatformManager(emptyAdapters);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => manager.GetAdapter(PlatformType.CML));
            Assert.Contains("Unknown or unsupported platform", exception.Message);
        }

        #endregion
    }
}
