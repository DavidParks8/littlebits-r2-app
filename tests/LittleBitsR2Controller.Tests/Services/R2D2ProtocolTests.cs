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
        // Speed 0 should map to index 31 (stop): floor(31 * (1 - 0)) = 31
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[R2D2Protocol.DriveStopIndex]);
        CollectionAssert.AreEqual(expectedBytes, result);
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
        // Should map to index 2 (full left in original's range [2,32])
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[2]);
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
        // Turn 0 should map to index 17 (straight ahead per original formula)
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[17]);
        CollectionAssert.AreEqual(expectedBytes, result);
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
        // Should clamp to valid range [2, 32]
        var expectedLeft = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[2]);
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
    public void GetTurnStraightCommand_ShouldReturnStraightValue()
    {
        // Act
        var result = R2D2Protocol.GetTurnStraightCommand();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[R2D2Protocol.TurnStraightIndex]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    [DataRow(0.5, 15)]     // Half forward → index 15 (floor(31*(1-0.5)) = floor(15.5) = 15)
    [DataRow(0.25, 23)]    // Quarter forward → index 23 (floor(31*0.75) = 23)
    [DataRow(-0.5, 46)]    // Half backward → index 46 (floor(31*1.5) = 46)
    [DataRow(-0.25, 38)]   // Quarter backward → index 38 (floor(31*1.25) = 38)
    [DataRow(0.1, 27)]     // Slow forward (floor(31*0.9) = 27)
    [DataRow(-0.1, 34)]    // Slow backward (floor(31*1.1) = 34)
    public void GetDriveCommand_IntermediateValues_ShouldReturnCorrectIndex(double speed, int expectedIndex)
    {
        // Act
        var result = R2D2Protocol.GetDriveCommand(speed);

        // Assert
        Assert.IsNotNull(result);
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[expectedIndex]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    [DataRow(0.5, 25)]     // Half right → floor(15.5*0.5 + 17.5) = floor(25.25) = 25
    [DataRow(-0.5, 9)]     // Half left → floor(15.5*(-0.5) + 17.5) = floor(9.75) = 9
    [DataRow(0.25, 21)]    // Quarter right → floor(15.5*0.25 + 17.5) = floor(21.375) = 21
    [DataRow(-0.25, 13)]   // Quarter left → floor(15.5*(-0.25) + 17.5) = floor(13.625) = 13
    [DataRow(0.1, 19)]     // Slight right → floor(15.5*0.1 + 17.5) = floor(19.05) = 19
    [DataRow(-0.1, 15)]    // Slight left → floor(15.5*(-0.1) + 17.5) = floor(15.95) = 15
    public void GetTurnCommand_IntermediateValues_ShouldReturnCorrectIndex(double turn, int expectedIndex)
    {
        // Act
        var result = R2D2Protocol.GetTurnCommand(turn);

        // Assert
        Assert.IsNotNull(result);
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[expectedIndex]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetSoundCommand_ValidSoundName_ShouldReturnCorrectBytes()
    {
        // Arrange
        const string soundName = "beep";

        // Act
        var result = R2D2Protocol.GetSoundCommand(soundName);

        // Assert
        Assert.IsNotNull(result);
        var expectedBytes = R2D2Protocol.HexStringToBytes(R2D2Protocol.SoundEffects[soundName]);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void GetSoundCommand_InvalidSoundName_ShouldReturnNull()
    {
        // Arrange
        const string soundName = "nonexistent_sound";

        // Act
        var result = R2D2Protocol.GetSoundCommand(soundName);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetSoundCommand_AllSoundEffects_ShouldReturnValidBytes()
    {
        // Act & Assert
        foreach (var soundName in R2D2Protocol.SoundEffects.Keys)
        {
            var result = R2D2Protocol.GetSoundCommand(soundName);
            Assert.IsNotNull(result, $"Sound '{soundName}' should return valid bytes");
            Assert.IsNotEmpty(result, $"Sound '{soundName}' bytes should not be empty");
        }
    }

    [TestMethod]
    public void HexStringToBytes_EmptyString_ShouldReturnEmptyArray()
    {
        // Arrange
        const string hexString = "";

        // Act
        var result = R2D2Protocol.HexStringToBytes(hexString);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void HexStringToBytes_OddLength_ShouldThrowException()
    {
        // Arrange - odd length string will cause an exception
        const string hexString = "14020"; // 5 characters
        bool exceptionThrown = false;

        // Act
        try
        {
            R2D2Protocol.HexStringToBytes(hexString);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert - This should throw because Substring(i, 2) will go out of bounds
        Assert.IsTrue(exceptionThrown, "Expected ArgumentOutOfRangeException for odd-length hex string");
    }

    [TestMethod]
    public void DriveValues_ShouldHave63Elements()
    {
        // Assert
        Assert.HasCount(63, R2D2Protocol.DriveValues);
    }

    [TestMethod]
    public void TurnValues_ShouldHave33Elements()
    {
        // Assert
        Assert.HasCount(33, R2D2Protocol.TurnValues);
    }

    [TestMethod]
    public void SoundEffects_ShouldHave22Effects()
    {
        // Assert
        Assert.HasCount(22, R2D2Protocol.SoundEffects);
    }

    [TestMethod]
    public void GetDriveCommand_BoundaryValues_ShouldMapCorrectly()
    {
        // Test values very close to boundaries
        // At 0.999, floor(31*(1-0.999)) = floor(0.031) = 0 (full forward)
        var almostFullForward = R2D2Protocol.GetDriveCommand(0.999);
        var expectedForward = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[0]);
        CollectionAssert.AreEqual(expectedForward, almostFullForward);

        // At -0.999, floor(31*(1-(-0.999))) = floor(61.969) = 61
        var almostFullBackward = R2D2Protocol.GetDriveCommand(-0.999);
        var expectedBackward = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[61]);
        CollectionAssert.AreEqual(expectedBackward, almostFullBackward);

        // At 0.001, floor(31*(1-0.001)) = floor(30.969) = 30
        var barelyForward = R2D2Protocol.GetDriveCommand(0.001);
        var expectedBarelyForward = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[30]);
        CollectionAssert.AreEqual(expectedBarelyForward, barelyForward);
    }

    [TestMethod]
    public void GetTurnCommand_BoundaryValues_ShouldMapCorrectly()
    {
        // Test values very close to boundaries
        // At 0.999, floor(15.5*0.999 + 17.5) = floor(32.9945) = 32 (full right)
        var almostFullRight = R2D2Protocol.GetTurnCommand(0.999);
        var expectedRight = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[32]);
        CollectionAssert.AreEqual(expectedRight, almostFullRight);

        // At -0.999, floor(15.5*(-0.999) + 17.5) = floor(2.0055) = 2 (full left)
        var almostFullLeft = R2D2Protocol.GetTurnCommand(-0.999);
        var expectedLeft = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[2]);
        CollectionAssert.AreEqual(expectedLeft, almostFullLeft);
    }
}
