using System.Collections.Immutable;

namespace LittleBitsR2Controller.Services;

/// <summary>
/// R2D2 Bluetooth protocol constants and command values
/// Based on reverse-engineered protocol from https://github.com/meetar/littlebits-r2d2-controls
/// </summary>
public static class R2D2Protocol
{
    // Service and Characteristic UUIDs (from littlebits space rover/r2d2 control hub)
    public const string ServiceUuid = "d9d9e9e0-aa4e-4797-8151-cb41cedaf2ad";
    public const string CharacteristicUuid = "d9d9e9e1-aa4e-4797-8151-cb41cedaf2ad";

    /// <summary>
    /// Drive motor values - index 0 is full forward, index 31 is stop, index 62 is full backward
    /// </summary>
    public static readonly ImmutableArray<string> DriveValues = 
    [
        "140202FF659D", // 0 - full forward
        "140202FA3538",
        "140202F6F4B4",
        "140202F2B430",
        "140202ED57EE",
        "140202EA2709",
        "140202E6E685",
        "140202E3B620",
        "140202DF41FF",
        "140202DA115A",
        "140202D6D0D6",
        "140202D29052",
        "140202CE43EF",
        "140202CA036B",
        "140202C5F284",
        "140202C28263",
        "140202BE3D78",
        "140202BA7DFC",
        "140202B6BC70",
        "140202B2FCF4",
        "140202AB7FEC",
        "140202AA6FCD",
        "140202A6AE41",
        "140202A2EEC5",
        "140202A1DEA6",
        "1402029E191A",
        "1402029969FD",
        "140202969812",
        "14020292D896",
        "1402028D3B48",
        "1402028A4BAF",
        "140202897BCC", // 31 - stop
        "1402027DD457",
        "1402027994D3",
        "14020276653C",
        "1402027225B8",
        "1402026EF605",
        "1402026DC666",
        "1402026896C3",
        "1402026327A8",
        "1402026107EA",
        "1402025CE014",
        "14020259B0B1",
        "14020255713D",
        "140202502198",
        "1402024B82C2",
        "14020249A280",
        "14020245630C",
        "140202412388",
        "1402023BFC55",
        "14020239DC17",
        "140202337D5D",
        "140202304D3E",
        "1402022BEE64",
        "14020229CE26",
        "140202214F2E",
        "140202205F0F",
        "1402021BD837",
        "14020218E854",
        "1402021429D8",
        "1402020F8A82",
        "1402020CBAE1",
        "140202070B8A"  // 61 - full backward
    ];

    /// <summary>
    /// Turn values - index 0 is full left, index 16 is straight, index 32 is full right
    /// </summary>
    public static readonly ImmutableArray<string> TurnValues =
    [
        "140201FC00AD", // 0 - full left
        "140201EA725A",
        "140201E49394",
        "140201DF14AC",
        "140201D9746A",
        "140201D1F562",
        "140201CC36FE",
        "140201C697B4",
        "140201C0F772",
        "140201B918CC",
        "140201B4C961",
        "140201AE7A1A",
        "140201A7EB33",
        "140201A2BB96",
        "1402019C6C0B",
        "14020196CD41", // 15 - straight
        "14020190AD87",
        "1402018A1EFC",
        "14020184FF32",
        "1402017EB167",
        "14020178D1A1",
        "1402017270EB",
        "1402016C8314",
        "14020166225E",
        "140201604298",
        "1402015AD581",
        "14020154344F",
        "1402014F9715",
        "14020149F7D3",
        "1402014246B8",
        "1402013CD9E1",
        "1402013548C8",
        "140201002E3E"  // 31 - full right
    ];

    // Stop command indices
    public const int DriveStopIndex = 31;
    public const int TurnStraightIndex = 15;

    /// <summary>
    /// R2D2 sound effect commands
    /// Based on reverse-engineered protocol from https://github.com/meetar/littlebits-r2d2-controls
    /// </summary>
    public static readonly ImmutableDictionary<string, string> SoundEffects = new Dictionary<string, string>
    {
        { "grump", "1E011B42AA" },
        { "scold", "1E011A528B" },
        { "chitter", "1E011962E8" },
        { "chattering", "1E011872C9" },
        { "i love you", "1E01178326" },
        { "bleep", "1E01169307" },
        { "beep", "1E0115A364" },
        { "whistle", "1E0114B345" },
        { "descending", "1E0113C3A2" },
        { "excited", "1E0112D383" },
        { "cheery", "1E0111E3E0" },
        { "sad", "1E0110F3C1" },
        { "scream", "1E010F101F" },
        { "startup", "1E010E003E" },
        { "surprise", "1E010C207C" },
        { "story", "1E010A40BA" },
        { "wow", "1E010860F8" },
        { "thbt", "1E01068136" },
        { "worried", "1E0104A174" },
        { "dubious", "1E0102C1B2" },
        { "startup2", "1E0101F1D1" },
        { "thinking", "1E0100E1F0" }
    }.ToImmutableDictionary();

    /// <summary>
    /// Converts a hex string to a byte array
    /// </summary>
    public static byte[] HexStringToBytes(string hex)
    {
        int length = hex.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// Gets the drive command for a given speed value (-1.0 to 1.0)
    /// </summary>
    /// <param name="speed">Speed from -1.0 (full backward) to 1.0 (full forward), 0 is stop</param>
    public static byte[] GetDriveCommand(double speed)
    {
        // Clamp speed to valid range
        speed = Math.Clamp(speed, -1.0, 1.0);
        
        // Map speed to drive value index (0-62)
        // -1.0 -> 62 (full backward), 0 -> 31 (stop), 1.0 -> 0 (full forward)
        int index = (int)Math.Round((1.0 - speed) * 31.0);
        index = Math.Clamp(index, 0, DriveValues.Length - 1);
        
        return HexStringToBytes(DriveValues[index]);
    }

    /// <summary>
    /// Gets the turn command for a given turn value (-1.0 to 1.0)
    /// </summary>
    /// <param name="turn">Turn from -1.0 (full left) to 1.0 (full right), 0 is straight</param>
    public static byte[] GetTurnCommand(double turn)
    {
        // Clamp turn to valid range
        turn = Math.Clamp(turn, -1.0, 1.0);
        
        // Map turn to turn value index (0-32)
        // -1.0 -> 0 (full left), 0 -> 16 (straight), 1.0 -> 32 (full right)
        int index = (int)Math.Round((turn + 1.0) * 16.0);
        index = Math.Clamp(index, 0, TurnValues.Length - 1);
        
        return HexStringToBytes(TurnValues[index]);
    }

    /// <summary>
    /// Gets the stop command
    /// </summary>
    public static byte[] GetStopCommand()
    {
        return HexStringToBytes(DriveValues[DriveStopIndex]);
    }

    /// <summary>
    /// Gets a sound effect command by name
    /// </summary>
    /// <param name="soundName">Name of the sound effect (e.g., "beep", "excited")</param>
    /// <returns>Byte array for the sound command, or null if sound name not found</returns>
    public static byte[]? GetSoundCommand(string soundName)
    {
        if (SoundEffects.TryGetValue(soundName.ToLowerInvariant(), out var hexValue))
        {
            return HexStringToBytes(hexValue);
        }
        return null;
    }
}
