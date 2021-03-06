using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
using Libplanet.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace Libplanet
{
    /// <summary>
    /// An identifier of 20 bytes (or 40 letters in hexadecimal, commonly with
    /// a prefix <c>0x</c>) that refers to a unique account.
    /// <para>It is derived from the corresponding <see cref="PublicKey"/>
    /// of an account, but as a derivation loses information, it is always
    /// unidirectional.</para>
    /// <para>The address derivation from a public key is as follows:</para>
    /// <list type="number">
    /// <item><description>Calculates the Keccak-256, which is a previous form
    /// of SHA-3 before NIST standardized it and does not follow
    /// <a href="http://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.202.pdf"
    /// >FIPS-202</a>, of the corresponding <see cref="PublicKey"/>.
    /// </description></item>
    /// <item><description>Takes only the last 20 bytes of the calculated
    /// Keccak-256 hash.</description></item>
    /// <item><description>When the address needs to be shown to end users,
    /// displays these 20 bytes in hexadecimal, with a prefix <c>0x</c>.
    /// </description></item>
    /// </list>
    /// <para>Since the scheme of the address derivation and the <see
    /// cref="PrivateKey"/>/<see cref="PublicKey"/> is the same to
    /// <a href="https://www.ethereum.org/">Ethereum</a>, Ethereum addresses
    /// can be used by Libplanet-backed games/apps too.</para>
    /// </summary>
    /// <remarks>Every <see cref="Address"/> value is immutable.</remarks>
    /// <seealso cref="PublicKey"/>
    #pragma warning disable CS0282
    [Uno.GeneratedEquality]
    public partial struct Address
    #pragma warning restore CS0282
    {
        private ImmutableArray<byte> _byteArray;

        /// <summary>
        /// Creates an <see cref="Address"/> instance from the given <see
        /// cref="byte"/> array (i.e., <paramref name="address"/>).
        /// </summary>
        /// <param name="address">An array of 20 <see cref="byte"/>s which
        /// represents an <see cref="Address"/>.  This must not be <c>null</c>.
        /// </param>
        /// <exception cref="NullReferenceException">Thrown when <c>null</c> was
        /// passed to <paramref name="address"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the given <paramref
        /// name="address"/> array did not lengthen 20 bytes.</exception>
        /// <remarks>A valid <see cref="byte"/> array which represents an
        /// <see cref="Address"/> can be gotten using <see cref="ToByteArray()"
        /// /> method.</remarks>
        /// <seealso cref="ToByteArray()"/>
        public Address(byte[] address)
        {
            if (address == null)
            {
                throw new NullReferenceException("address must not be null");
            }

            if (address.Length != 20)
            {
                throw new ArgumentException("address must be 20 bytes");
            }

            _byteArray = address.ToImmutableArray();

            #pragma warning disable CS0103
            /* Suppress CS0171.
            See also https://github.com/nventive/Uno.CodeGen/pull/91
            */
            _computedHashCode = null;
            _computedKeyHashCode = null;
            #pragma warning restore CS0103
        }

        /// <summary>
        /// Derives the corresponding <see cref="Address"/> from a <see
        /// cref="PublicKey"/>.
        /// <para>Note that there is an equivalent extension method
        /// <see cref="AddressExtension.ToAddress(PublicKey)"/>, which enables
        /// a code like <c>publicKey.ToAddress()</c> instead of
        /// <c>new Address(publicKey)</c>, for convenience.</para>
        /// </summary>
        /// <param name="publicKey">A <see cref="PublicKey"/> to derive
        /// the corresponding <see cref="Address"/> from.</param>
        /// <seealso cref="AddressExtension.ToAddress(PublicKey)"/>
        public Address(PublicKey publicKey)
            : this(DeriveAddress(publicKey))
        {
        }

        /// <summary>
        /// An immutable array of 20 <see cref="byte"/>s that represent this
        /// <see cref="Address"/>.
        /// </summary>
        /// <remarks>This is immutable.  For a mutable array, call <see
        /// cref="ToByteArray()"/> method.</remarks>
        /// <seealso cref="ToByteArray()"/>
        [Uno.EqualityKey]
        public ImmutableArray<byte> ByteArray
        {
            get
            {
                if (_byteArray.IsDefault)
                {
                    _byteArray = new byte[20].ToImmutableArray();
                }

                return _byteArray;
            }
        }

        /// <summary>
        /// Gets a mutable array of 20 <see cref="byte"/>s that represent
        /// this <see cref="Address"/>.
        /// </summary>
        /// <returns>A new mutable array which represents this
        /// <see cref="Address"/>.  Since it is created every time the method
        /// is called, any mutation on that does not affect internal states of
        /// this <see cref="Address"/>.</returns>
        /// <seealso cref="ByteArray"/>
        /// <seealso cref="Address(byte[])"/>
        [Pure]
        public byte[] ToByteArray() => ByteArray.ToArray();

        /// <summary>
        /// Gets a hexadecimal string of 40 letters that represent this
        /// <see cref="Address"/>.
        /// </summary>
        /// <example>A returned string looks like
        /// <c>87ae4774e20963fd6cac967cf47adcf880c3e89b</c>.</example>
        /// <returns>A hexadecimal string of 40 letters that represent
        /// this <see cref="Address"/>.  Note that it does not start with
        /// a prefix.</returns>
        /// <remarks>As the returned string has no prefix, for
        /// <c>0x</c>-prefixed hexadecimal, call <see cref="ToString()"/>
        /// method instead.</remarks>
        /// <seealso cref="ToString()"/>
        [Pure]
        public string ToHex()
        {
            return ByteUtil.Hex(ToByteArray());
        }

        /// <summary>
        /// Gets a <c>0x</c>-prefixed hexadecimal string of 42 letters that
        /// represent this <see cref="Address"/>.
        /// </summary>
        /// <example>A returned string looks like
        /// <c>0x87ae4774e20963fd6cac967cf47adcf880c3e89b</c>.</example>
        /// <returns>A <c>0x</c>-hexadecimal string of 42 letters that represent
        /// this <see cref="Address"/>.</returns>
        /// <remarks>As the returned string is <c>0x</c>-prefixed, for
        /// hexadecimal without prefix, call <see cref="ToHex()"/> method
        /// instead.</remarks>
        /// <seealso cref="ToHex()"/>
        [Pure]
        public override string ToString()
        {
            return $"0x{ToHex()}";
        }

        private static byte[] DeriveAddress(PublicKey key)
        {
            byte[] hashPayload = key.Format(false).Skip(1).ToArray();
            var digest = new KeccakDigest(256);
            var output = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(hashPayload, 0, hashPayload.Length);
            digest.DoFinal(output, 0);

            return output.Skip(output.Length - 20).ToArray();
        }
    }
}
