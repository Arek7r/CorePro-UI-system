// Provides a public method for reconstructing an AudioHandle from its raw packed
// value. Used exclusively by the auto-generated AudioConstants.cs file so that
// generated code remains clean and readable.
//
// This is the only place that constructs an AudioHandle from a raw long -
// normal user code uses AudioHandle.None or the [AudioSFX]/[AudioMusic] attributes.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CorePro.AudioSystem
{
    /// <summary>
    /// Reconstructs an <see cref="AudioHandle"/> from its raw packed value.
    /// Intended for use by auto-generated constants files only.
    /// </summary>
    public static class AudioHandleFactory
    {
        /// <summary>
        /// Returns an <see cref="AudioHandle"/> whose internal state matches
        /// <paramref name="packed"/>.
        /// </summary>
        /// <remarks>
        /// The packed value encodes both the library instance ID and the entry index.
        /// Values are generated at edit-time by <c>AudioConstantsGenerator</c> and
        /// baked into the generated constants file - do not call this method manually.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AudioHandle FromPacked(long packed)
        {
            // MemoryMarshal.Read reinterprets the bytes of `packed` as an AudioHandle.
            // AudioHandle is a single-field struct { long _packed } so the layout
            // is identical - this is a pure bit-cast with no allocation and no unsafe keyword.
            return MemoryMarshal.Read<AudioHandle>(
                MemoryMarshal.AsBytes(
                    MemoryMarshal.CreateReadOnlySpan(ref packed, 1)));
        }
    }
}
