using LittleBitsR2Controller.Services;

namespace LittleBitsR2Controller.Tests.Services;

[TestClass]
public class BluetoothDeviceTests
{
    [TestMethod]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var device = new BluetoothDevice("device-123", "R2D2-Test");

        // Assert
        Assert.AreEqual("device-123", device.Id);
        Assert.AreEqual("R2D2-Test", device.Name);
    }

    [TestMethod]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var device2 = new BluetoothDevice("device-123", "R2D2-Test");

        // Assert
        Assert.AreEqual(device1, device2);
        Assert.IsTrue(device1 == device2);
        Assert.IsFalse(device1 != device2);
    }

    [TestMethod]
    public void Equality_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var device2 = new BluetoothDevice("device-456", "R2D2-Test");

        // Assert
        Assert.AreNotEqual(device1, device2);
        Assert.IsFalse(device1 == device2);
        Assert.IsTrue(device1 != device2);
    }

    [TestMethod]
    public void Equality_WithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test1");
        var device2 = new BluetoothDevice("device-123", "R2D2-Test2");

        // Assert
        Assert.AreNotEqual(device1, device2);
    }

    [TestMethod]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var device2 = new BluetoothDevice("device-123", "R2D2-Test");

        // Assert
        Assert.AreEqual(device1.GetHashCode(), device2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldIncludeIdAndName()
    {
        // Arrange
        var device = new BluetoothDevice("device-123", "R2D2-Test");

        // Act
        var result = device.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("device-123", result);
        Assert.Contains("R2D2-Test", result);
    }

    [TestMethod]
    public void Deconstruction_ShouldWork()
    {
        // Arrange
        var device = new BluetoothDevice("device-123", "R2D2-Test");

        // Act
        var (id, name) = device;

        // Assert
        Assert.AreEqual("device-123", id);
        Assert.AreEqual("R2D2-Test", name);
    }

    [TestMethod]
    public void With_ShouldCreateNewInstanceWithChangedProperty()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");

        // Act
        var device2 = device1 with { Name = "R2D2-Modified" };

        // Assert
        Assert.AreEqual("device-123", device2.Id);
        Assert.AreEqual("R2D2-Modified", device2.Name);
        Assert.AreNotEqual(device1, device2);
    }

    [TestMethod]
    public void With_ShouldNotModifyOriginal()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var originalName = device1.Name;

        // Act
        var device2 = device1 with { Name = "R2D2-Modified" };

        // Assert
        Assert.AreEqual(originalName, device1.Name);
        Assert.AreNotEqual(device1.Name, device2.Name);
    }

    [TestMethod]
    public void Constructor_WithEmptyStrings_ShouldWork()
    {
        // Arrange & Act
        var device = new BluetoothDevice("", "");

        // Assert
        Assert.AreEqual("", device.Id);
        Assert.AreEqual("", device.Name);
    }

    [TestMethod]
    public void Constructor_WithLongStrings_ShouldWork()
    {
        // Arrange
        var longId = new string('A', 1000);
        var longName = new string('B', 1000);

        // Act
        var device = new BluetoothDevice(longId, longName);

        // Assert
        Assert.AreEqual(longId, device.Id);
        Assert.AreEqual(longName, device.Name);
    }

    [TestMethod]
    public void Constructor_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var specialId = "device-123!@#$%^&*()";
        var specialName = "R2D2 Test 🤖";

        // Act
        var device = new BluetoothDevice(specialId, specialName);

        // Assert
        Assert.AreEqual(specialId, device.Id);
        Assert.AreEqual(specialName, device.Name);
    }

    [TestMethod]
    public void Equality_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var device = new BluetoothDevice("device-123", "R2D2-Test");

        // Assert
        Assert.IsFalse(device.Equals(null));
        Assert.IsFalse(device == null);
        Assert.IsTrue(device != null);
    }

    [TestMethod]
    public void Equality_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var device = new BluetoothDevice("device-123", "R2D2-Test");
        var otherObject = "Not a BluetoothDevice";

        // Assert
        Assert.IsFalse(device.Equals(otherObject));
    }

    [TestMethod]
    public void HashSet_ShouldNotContainDuplicates()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var device2 = new BluetoothDevice("device-123", "R2D2-Test");
        var hashSet = new HashSet<BluetoothDevice>();

        // Act
        hashSet.Add(device1);
        hashSet.Add(device2);

        // Assert
        Assert.HasCount(1, hashSet);
    }

    [TestMethod]
    public void Dictionary_ShouldUseSameKeyForEqualDevices()
    {
        // Arrange
        var device1 = new BluetoothDevice("device-123", "R2D2-Test");
        var device2 = new BluetoothDevice("device-123", "R2D2-Test");
        var dictionary = new Dictionary<BluetoothDevice, string>();

        // Act
        dictionary[device1] = "First";
        dictionary[device2] = "Second";

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("Second", dictionary[device1]);
    }
}
