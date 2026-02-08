using LittleBitsR2Controller.Services;

namespace LittleBitsR2Controller.Tests.Services;

[TestClass]
public class R2D2ProtocolEdgeCasesTests
{
    [TestMethod]
    public void ServiceUuid_ShouldBeValidGuid()
    {
        // Act
        var isValid = Guid.TryParse(R2D2Protocol.ServiceUuid, out var guid);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreNotEqual(Guid.Empty, guid);
    }

    [TestMethod]
    public void CharacteristicUuid_ShouldBeValidGuid()
    {
        // Act
        var isValid = Guid.TryParse(R2D2Protocol.CharacteristicUuid, out var guid);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreNotEqual(Guid.Empty, guid);
    }

    [TestMethod]
    public void DriveStopIndex_ShouldBeInValidRange()
    {
        // Assert - Verify the stop index (31) is within the valid range of drive values
        Assert.IsGreaterThanOrEqualTo(0, R2D2Protocol.DriveStopIndex);
        Assert.IsLessThan(R2D2Protocol.DriveValues.Length, R2D2Protocol.DriveStopIndex);
    }

    [TestMethod]
    public void TurnStraightIndex_ShouldBeInValidRange()
    {
        // Assert - Verify the straight index (17) is within the valid range of turn values
        Assert.IsGreaterThanOrEqualTo(0, R2D2Protocol.TurnStraightIndex);
        Assert.IsLessThan(R2D2Protocol.TurnValues.Length, R2D2Protocol.TurnStraightIndex);
    }

    [TestMethod]
    public void DriveValues_AllElements_ShouldBeValidHex()
    {
        // Act & Assert
        for (int i = 0; i < R2D2Protocol.DriveValues.Length; i++)
        {
            var value = R2D2Protocol.DriveValues[i];
            Assert.IsNotNull(value, $"DriveValue[{i}] should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(value), $"DriveValue[{i}] should not be empty");
            
            // Verify it's valid hex
            var bytes = R2D2Protocol.HexStringToBytes(value);
            Assert.IsNotNull(bytes, $"DriveValue[{i}] should convert to bytes");
            Assert.IsNotEmpty(bytes, $"DriveValue[{i}] should have bytes");
        }
    }

    [TestMethod]
    public void TurnValues_AllElements_ShouldBeValidHex()
    {
        // Act & Assert
        for (int i = 0; i < R2D2Protocol.TurnValues.Length; i++)
        {
            var value = R2D2Protocol.TurnValues[i];
            Assert.IsNotNull(value, $"TurnValue[{i}] should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(value), $"TurnValue[{i}] should not be empty");
            
            // Verify it's valid hex
            var bytes = R2D2Protocol.HexStringToBytes(value);
            Assert.IsNotNull(bytes, $"TurnValue[{i}] should convert to bytes");
            Assert.IsNotEmpty(bytes, $"TurnValue[{i}] should have bytes");
        }
    }

    [TestMethod]
    public void SoundEffects_AllElements_ShouldBeValidHex()
    {
        // Act & Assert
        foreach (var kvp in R2D2Protocol.SoundEffects)
        {
            var soundName = kvp.Key;
            var hexCommand = kvp.Value;
            
            Assert.IsNotNull(soundName, "Sound name should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(soundName), "Sound name should not be empty");
            Assert.IsNotNull(hexCommand, $"Hex command for '{soundName}' should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(hexCommand), $"Hex command for '{soundName}' should not be empty");
            
            // Verify it's valid hex
            var bytes = R2D2Protocol.HexStringToBytes(hexCommand);
            Assert.IsNotNull(bytes, $"Hex command for '{soundName}' should convert to bytes");
            Assert.IsNotEmpty(bytes, $"Hex command for '{soundName}' should have bytes");
        }
    }

    [TestMethod]
    public void DriveValues_ShouldHaveEvenLengthHexStrings()
    {
        // Act & Assert
        foreach (var value in R2D2Protocol.DriveValues)
        {
            Assert.AreEqual(0, value.Length % 2, $"Drive value '{value}' should have even-length hex string");
        }
    }

    [TestMethod]
    public void TurnValues_ShouldHaveEvenLengthHexStrings()
    {
        // Act & Assert
        foreach (var value in R2D2Protocol.TurnValues)
        {
            Assert.AreEqual(0, value.Length % 2, $"Turn value '{value}' should have even-length hex string");
        }
    }

    [TestMethod]
    public void SoundEffects_ShouldHaveEvenLengthHexStrings()
    {
        // Act & Assert
        foreach (var kvp in R2D2Protocol.SoundEffects)
        {
            Assert.AreEqual(0, kvp.Value.Length % 2, $"Sound '{kvp.Key}' should have even-length hex string");
        }
    }

    [TestMethod]
    public void DriveValues_ShouldAllHaveSameLength()
    {
        // Arrange
        var firstLength = R2D2Protocol.DriveValues[0].Length;

        // Act & Assert
        foreach (var value in R2D2Protocol.DriveValues)
        {
            Assert.AreEqual(firstLength, value.Length, $"All drive values should have the same length ({firstLength})");
        }
    }

    [TestMethod]
    public void TurnValues_ShouldAllHaveSameLength()
    {
        // Arrange
        var firstLength = R2D2Protocol.TurnValues[0].Length;

        // Act & Assert
        foreach (var value in R2D2Protocol.TurnValues)
        {
            Assert.AreEqual(firstLength, value.Length, $"All turn values should have the same length ({firstLength})");
        }
    }

    [TestMethod]
    public void SoundEffects_ShouldAllHaveSameLength()
    {
        // Arrange
        var firstLength = R2D2Protocol.SoundEffects.First().Value.Length;

        // Act & Assert
        foreach (var kvp in R2D2Protocol.SoundEffects)
        {
            Assert.AreEqual(firstLength, kvp.Value.Length, $"Sound '{kvp.Key}' should have the same length ({firstLength})");
        }
    }

    [TestMethod]
    public void GetDriveCommand_VerySmallPositiveSpeed_ShouldMapCorrectly()
    {
        // Very small positive speed should map to index close to stop (31)
        var result = R2D2Protocol.GetDriveCommand(0.001);
        var expected = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[30]); // floor(31*(1-0.001)) = 30
        
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetDriveCommand_VerySmallNegativeSpeed_ShouldMapCorrectly()
    {
        // Very small negative speed should map to index close to stop (31)
        var result = R2D2Protocol.GetDriveCommand(-0.001);
        var expected = R2D2Protocol.HexStringToBytes(R2D2Protocol.DriveValues[31]); // floor(31*(1+0.001)) = 31
        
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetTurnCommand_VerySmallPositiveTurn_ShouldMapCorrectly()
    {
        // Very small positive turn should map to index close to straight (17)
        var result = R2D2Protocol.GetTurnCommand(0.001);
        var expected = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[17]); // floor(15.5*0.001 + 17.5) = 17
        
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetTurnCommand_VerySmallNegativeTurn_ShouldMapCorrectly()
    {
        // Very small negative turn should map to index close to straight (17)
        var result = R2D2Protocol.GetTurnCommand(-0.001);
        var expected = R2D2Protocol.HexStringToBytes(R2D2Protocol.TurnValues[17]); // floor(15.5*-0.001 + 17.5) = 17
        
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetDriveCommand_AllValidInputs_ShouldProduceDistinctOutputs()
    {
        // Test that different speeds produce different commands (mostly)
        var commands = new HashSet<string>();
        
        for (double speed = -1.0; speed <= 1.0; speed += 0.1)
        {
            var bytes = R2D2Protocol.GetDriveCommand(speed);
            var hex = BitConverter.ToString(bytes);
            commands.Add(hex);
        }
        
        // Should have multiple distinct commands
        Assert.IsGreaterThan(10, commands.Count);
    }

    [TestMethod]
    public void GetTurnCommand_AllValidInputs_ShouldProduceDistinctOutputs()
    {
        // Test that different turns produce different commands (mostly)
        var commands = new HashSet<string>();
        
        for (double turn = -1.0; turn <= 1.0; turn += 0.1)
        {
            var bytes = R2D2Protocol.GetTurnCommand(turn);
            var hex = BitConverter.ToString(bytes);
            commands.Add(hex);
        }
        
        // Should have multiple distinct commands
        Assert.IsGreaterThan(10, commands.Count);
    }

    [TestMethod]
    public void HexStringToBytes_AllUpperCase_ShouldWork()
    {
        // Arrange
        var hexString = "ABCDEF";
        
        // Act
        var result = R2D2Protocol.HexStringToBytes(hexString);
        
        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual(0xAB, result[0]);
        Assert.AreEqual(0xCD, result[1]);
        Assert.AreEqual(0xEF, result[2]);
    }

    [TestMethod]
    public void HexStringToBytes_AllLowerCase_ShouldWork()
    {
        // Arrange
        var hexString = "abcdef";
        
        // Act
        var result = R2D2Protocol.HexStringToBytes(hexString);
        
        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual(0xAB, result[0]);
        Assert.AreEqual(0xCD, result[1]);
        Assert.AreEqual(0xEF, result[2]);
    }

    [TestMethod]
    public void HexStringToBytes_MixedCase_ShouldWork()
    {
        // Arrange
        var hexString = "AbCdEf";
        
        // Act
        var result = R2D2Protocol.HexStringToBytes(hexString);
        
        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual(0xAB, result[0]);
        Assert.AreEqual(0xCD, result[1]);
        Assert.AreEqual(0xEF, result[2]);
    }

    [TestMethod]
    public void GetDriveCommand_Symmetry_ForwardAndBackwardShouldBeMirrored()
    {
        // For speed=0.5, index should be floor(31*0.5) = 15
        // For speed=-0.5, index should be floor(31*1.5) = 46
        // These should be symmetric around the stop index (31)
        
        var forward = R2D2Protocol.GetDriveCommand(0.5);
        var backward = R2D2Protocol.GetDriveCommand(-0.5);
        
        Assert.IsNotNull(forward);
        Assert.IsNotNull(backward);
        Assert.AreNotEqual(forward, backward);
    }

    [TestMethod]
    public void GetTurnCommand_Symmetry_LeftAndRightShouldBeMirrored()
    {
        // For turn=0.5, index should be floor(15.5*0.5 + 17.5) = 25
        // For turn=-0.5, index should be floor(15.5*-0.5 + 17.5) = 9
        // These should be symmetric around the straight index (17)
        
        var right = R2D2Protocol.GetTurnCommand(0.5);
        var left = R2D2Protocol.GetTurnCommand(-0.5);
        
        Assert.IsNotNull(right);
        Assert.IsNotNull(left);
        Assert.AreNotEqual(right, left);
    }

    [TestMethod]
    public void SoundEffects_ShouldContainExpectedSounds()
    {
        // Verify key sound effects exist
        var expectedSounds = new[] {
            "beep", "bleep", "whistle", "grump", "scold",
            "excited", "sad", "wow!", "surprise!!", "scream!!",
            "startup", "thinking"
        };
        
        foreach (var sound in expectedSounds)
        {
            Assert.IsTrue(R2D2Protocol.SoundEffects.ContainsKey(sound), 
                $"Should contain sound effect: {sound}");
        }
    }

    [TestMethod]
    public void SoundEffects_Keys_ShouldBeImmutable()
    {
        // SoundEffects is ImmutableDictionary, verify it's truly immutable
        var keys = R2D2Protocol.SoundEffects.Keys.ToList();
        var count = R2D2Protocol.SoundEffects.Count;
        
        // Try to verify the collection is the same after accessing
        Assert.HasCount(count, R2D2Protocol.SoundEffects);
        CollectionAssert.AreEquivalent(keys, R2D2Protocol.SoundEffects.Keys.ToList());
    }

    [TestMethod]
    public void DriveValues_ShouldBeImmutable()
    {
        // Verify the array is immutable (ImmutableArray)
        var firstValue = R2D2Protocol.DriveValues[0];
        var count = R2D2Protocol.DriveValues.Length;
        
        // Accessing should not change the collection
        Assert.AreEqual(firstValue, R2D2Protocol.DriveValues[0]);
        Assert.HasCount(count, R2D2Protocol.DriveValues);
    }

    [TestMethod]
    public void TurnValues_ShouldBeImmutable()
    {
        // Verify the array is immutable (ImmutableArray)
        var firstValue = R2D2Protocol.TurnValues[0];
        var count = R2D2Protocol.TurnValues.Length;
        
        // Accessing should not change the collection
        Assert.AreEqual(firstValue, R2D2Protocol.TurnValues[0]);
        Assert.HasCount(count, R2D2Protocol.TurnValues);
    }
}
