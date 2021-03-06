/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace dnlib.DotNet {
	/// <summary>
	/// Hashes some data according to a <see cref="AssemblyHashAlgorithm"/>
	/// </summary>
	struct AssemblyHash : IDisposable {
		HashAlgorithm hasher;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>If <paramref name="hashAlgo"/> is an unsupported hash algorithm, then
		/// <see cref="AssemblyHashAlgorithm.SHA1"/> will be used as the hash algorithm.</remarks>
		/// <param name="hashAlgo">The algorithm to use</param>
		public AssemblyHash(AssemblyHashAlgorithm hashAlgo) {
			switch (hashAlgo) {
			case AssemblyHashAlgorithm.MD5:
				hasher = MD5.Create();
				break;

			case AssemblyHashAlgorithm.None:
			case AssemblyHashAlgorithm.MD2:
			case AssemblyHashAlgorithm.MD4:
			case AssemblyHashAlgorithm.SHA1:
			case AssemblyHashAlgorithm.MAC:
			case AssemblyHashAlgorithm.SSL3_SHAMD5:
			case AssemblyHashAlgorithm.HMAC:
			case AssemblyHashAlgorithm.TLS1PRF:
			case AssemblyHashAlgorithm.HASH_REPLACE_OWF:
			default:
				hasher = SHA1.Create();
				break;

			case AssemblyHashAlgorithm.SHA_256:
				hasher = SHA256.Create();
				break;

			case AssemblyHashAlgorithm.SHA_384:
				hasher = SHA384.Create();
				break;

			case AssemblyHashAlgorithm.SHA_512:
				hasher = SHA512.Create();
				break;
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (hasher != null)
				((IDisposable)hasher).Dispose();
		}

		/// <summary>
		/// Hash data
		/// </summary>
		/// <remarks>If <paramref name="hashAlgo"/> is an unsupported hash algorithm, then
		/// <see cref="AssemblyHashAlgorithm.SHA1"/> will be used as the hash algorithm.</remarks>
		/// <param name="data">The data</param>
		/// <param name="hashAlgo">The algorithm to use</param>
		/// <returns>Hashed data or null if <paramref name="data"/> was <c>null</c></returns>
		public static byte[] Hash(byte[] data, AssemblyHashAlgorithm hashAlgo) {
			if (data == null)
				return null;

			using (var asmHash = new AssemblyHash(hashAlgo)) {
				asmHash.Hash(data);
				return asmHash.ComputeHash();
			}
		}

		/// <summary>
		/// Hash data
		/// </summary>
		/// <param name="data">Data</param>
		public void Hash(byte[] data) {
			Hash(data, 0, data.Length);
		}

		/// <summary>
		/// Hash data
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="offset">Offset</param>
		/// <param name="length">Length</param>
		public void Hash(byte[] data, int offset, int length) {
			if (hasher.TransformBlock(data, offset, length, data, offset) != length)
				throw new IOException("Could not calculate hash");
		}

		/// <summary>
		/// Hash stream data
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="length">Number of bytes to hash</param>
		/// <param name="buffer">Temp buffer</param>
		public void Hash(Stream stream, uint length, byte[] buffer) {
			while (length > 0) {
				int len = length > (uint)buffer.Length ? buffer.Length : (int)length;
				if (stream.Read(buffer, 0, len) != len)
					throw new IOException("Could not read data");
				Hash(buffer, 0, len);
				length -= (uint)len;
			}
		}

		/// <summary>
		/// Computes the hash
		/// </summary>
		public byte[] ComputeHash() {
			hasher.TransformFinalBlock(new byte[0], 0, 0);
			return hasher.Hash;
		}

		/// <summary>
		/// Creates a public key token from the hash of some <paramref name="publicKeyData"/>
		/// </summary>
		/// <remarks>A public key is hashed, and the last 8 bytes of the hash, in reverse
		/// order, is used as the public key token</remarks>
		/// <param name="publicKeyData">The data</param>
		/// <returns>A new <see cref="PublicKeyToken"/> instance</returns>
		public static PublicKeyToken CreatePublicKeyToken(byte[] publicKeyData) {
			if (publicKeyData == null)
				return new PublicKeyToken();
			var hash = Hash(publicKeyData, AssemblyHashAlgorithm.SHA1);
			byte[] pkt = new byte[8];
			for (int i = 0; i < pkt.Length && i < hash.Length; i++)
				pkt[i] = hash[hash.Length - i - 1];
			return new PublicKeyToken(pkt);
		}
	}
}
