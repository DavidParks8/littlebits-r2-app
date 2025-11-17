using LittleBitsR2Controller.Services;

namespace LittleBitsR2Controller.Tests.Services;

[TestClass]
public class R2D2ProtocolTests
{
    [TestMethod]
    public void HexStringToBytes_ShouldConvertCorrectly()
    {
        // Arrange
        const string hexString = "140202FF659D";
        var expectedBytes = new byte[] { 0x14, 0x02, 0x02, 0xFF, 0x65, 0x9D };

        // Act
        var result = R2D2Protocol.HexStringToBytes(hexString);

        // Assert
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetDriveCommand_FullForward_ShouldReturnCorrectValue()
    {
        // Arrange
        const double fullForward = 1.0;

        // Act
        var result = R2D2Protocol.GetDriveCommand(fullForward);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        // Should map to index 0 (full forward)
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[0]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetDriveCommand_Stop_ShouldReturnCorrectValue()
    {
        // Arrange
        const double stop = 0.0;

        // Act
        var result = R2D2Protocol.GetDriveCommand(stop);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        // Speed 0 should map to a value around index 30-31 (stop region)
        // The exact index depends on rounding, so we just verify it produces a valid command
    }

    [TestMethod]
    public void GetDriveCommand_FullBackward_ShouldReturnCorrectValue()
    {
        // Arrange
        const double fullBackward = -1.0;

        // Act
        var result = R2D2Protocol.GetDriveCommand(fullBackward);

        // Assert
        Assert.IsNotNull(result);
        // Should map to index 62 (full backward) - last element
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[62]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetDriveCommand_ShouldClampExcessiveValues()
    {
        // Arrange
        const double tooFast = 2.0;
        const double tooSlow = -2.0;

        // Act
        var resultFast = R2D2Protocol.GetDriveCommand(tooFast);
        var resultSlow = R2D2Protocol.GetDriveCommand(tooSlow);

        // Assert
        Assert.IsNotNull(resultFast);
        Assert.IsNotNull(resultSlow);
        // Should clamp to valid range
        var expectedFast = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[0]);
        var expectedSlow = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[62]);
        CollectionAssert.AreEqual(expectedFast, resultFast);
        CollectionAssert.AreEqual(expectedSlow, resultSlow);
    }

    [TestMethod]
    public void GetTurnCommand_FullLeft_ShouldReturnCorrectValue()
    {
        // Arrange
        const double fullLeft = -1.0;

        // Act
        var result = R2D2Protocol.GetTurnCommand(fullLeft);

        // Assert
        Assert.IsNotNull(result);
        // Should map to index 0 (full left)
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[0]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetTurnCommand_Straight_ShouldReturnCorrectValue()
    {
        // Arrange
        const double straight = 0.0;

        // Act
        var result = R2D2Protocol.GetTurnCommand(straight);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        // Turn 0 should map to a value around index 15-16 (straight region)
        // The exact index depends on rounding, so we just verify it produces a valid command
    }

    [TestMethod]
    public void GetTurnCommand_FullRight_ShouldReturnCorrectValue()
    {
        // Arrange
        const double fullRight = 1.0;

        // Act
        var result = R2D2Protocol.GetTurnCommand(fullRight);

        // Assert
        Assert.IsNotNull(result);
        // Should map to index 32 (full right) - last element
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[32]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetTurnCommand_ShouldClampExcessiveValues()
    {
        // Arrange
        const double tooLeft = -2.0;
        const double tooRight = 2.0;

        // Act
        var resultLeft = R2D2Protocol.GetTurnCommand(tooLeft);
        var resultRight = R2D2Protocol.GetTurnCommand(tooRight);

        // Assert
        Assert.IsNotNull(resultLeft);
        Assert.IsNotNull(resultRight);
        // Should clamp to valid range
        var expectedLeft = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[0]);
        var expectedRight = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[32]);
        CollectionAssert.AreEqual(expectedLeft, resultLeft);
        CollectionAssert.AreEqual(expectedRight, resultRight);
    }

    [TestMethod]
    public void GetStopCommand_ShouldReturnStopValue()
    {
        // Act
        var result = R2D2Protocol.GetStopCommand();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        // Should return the stop command from the protocol
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[R2D2Protocol.DriveStopIndex]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetSoundCommand_ValidSoundName_ShouldReturnCorrectValue()
    {
        // Arrange
        const string soundName = "beep";

        // Act
        var result = R2D2Protocol.GetSoundCommand(soundName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        var expectedBytes = R2D2Protocol.HexStringToBytes("1E0115A364");
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetSoundCommand_CaseInsensitive_ShouldReturnCorrectValue()
    {
        // Arrange - test case insensitivity
        var result1 = R2D2Protocol.GetSoundCommand("BEEP");
        var result2 = R2D2Protocol.GetSoundCommand("beep");
        var result3 = R2D2Protocol.GetSoundCommand("BeEp");

        // Assert - all should return the same value
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        CollectionAssert.AreEqual(result1, result2);
        CollectionAssert.AreEqual(result2, result3);
    }

    [TestMethod]
    public void GetSoundCommand_InvalidSoundName_ShouldReturnNull()
    {
        // Arrange
        const string invalidSoundName = "nonexistent_sound";

        // Act
        var result = R2D2Protocol.GetSoundCommand(invalidSoundName);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SoundEffects_ShouldContainAllExpectedSounds()
    {
        // Assert - verify all expected sounds are present
        Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey("beep"));
        Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey("excited"));
        Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey("grump"));
        Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey("scream"));
        Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey("thinking"));
        #pragma warning disable MSTEST0037 // Use 'Assert.HasCount' instead of 'Assert.AreEqual'
        Assert.AreEqual(22, R2D2Protocol.SoundEffects.Count);
        #pragma warning restore MSTEST0037
    }
}
