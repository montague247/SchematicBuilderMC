namespace SchematicBuilderMC.Core;

/// <summary>
/// Extension methods for convenient conversion and property configuration from block identifier strings.
/// </summary>
public static class BlockStateExtensions
{
    /// <summary>
    /// Creates a new <see cref="BlockState"/> from the block identifier with the specified property set.
    /// </summary>
    /// <param name="identifier">The block identifier (e.g. <c>Minecraft.Blocks.Chest</c>).</param>
    /// <param name="key">The property key (e.g. "facing").</param>
    /// <param name="value">The property value (e.g. "south").</param>
    /// <returns>A new <see cref="BlockState"/> instance with the specified property.</returns>
    public static BlockState WithProperty(this string identifier, string key, string value) =>
        new BlockState(identifier).WithProperty(key, value);

    /// <summary>
    /// Converts a block identifier string to a <see cref="BlockState"/>.
    /// </summary>
    /// <param name="identifier">The block identifier (e.g. <c>Minecraft.Blocks.Cobblestone</c>).</param>
    /// <returns>A <see cref="BlockState"/> corresponding to the identifier.</returns>
    public static BlockState ToBlockState(this string identifier) =>
        BlockState.Parse(identifier);
}
