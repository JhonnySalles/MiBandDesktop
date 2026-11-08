using System;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace MiBandDesktop.Core.Protocol;

public class HuamiCryptoHelper
{
    private static X9ECParameters GetParameters()
    {
        return Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("sect163r2")
               ?? NistNamedCurves.GetByName("B-163")
               ?? Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp160r1");
    }

    public static (byte[] privateKey, byte[] publicKey) GenerateKeyPair()
    {
        X9ECParameters ecP = GetParameters();
        ECDomainParameters ecSpec = new(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
        ECKeyPairGenerator g = new();
        g.Init(new ECKeyGenerationParameters(ecSpec, new SecureRandom()));
        AsymmetricCipherKeyPair pair = g.GenerateKeyPair();

        var priv = (ECPrivateKeyParameters)pair.Private;
        var pub = (ECPublicKeyParameters)pair.Public;

        byte[] privBytes = priv.D.ToByteArrayUnsigned();
        if (privBytes.Length < 24)
        {
            byte[] padded = new byte[24];
            Array.Copy(privBytes, 0, padded, 24 - privBytes.Length, privBytes.Length);
            privBytes = padded;
        }

        byte[] xBytes = pub.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned();
        byte[] yBytes = pub.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned();

        byte[] pubBytes = new byte[48];
        Array.Copy(xBytes, 0, pubBytes, 24 - Math.Min(24, xBytes.Length), Math.Min(24, xBytes.Length));
        Array.Copy(yBytes, 0, pubBytes, 24 + 24 - Math.Min(24, yBytes.Length), Math.Min(24, yBytes.Length));

        return (privBytes, pubBytes);
    }

    public static byte[] CalculateSharedSecret(byte[] privateKeyBytes, byte[] remotePublicKeyBytes)
    {
        X9ECParameters ecP = GetParameters();
        ECDomainParameters ecSpec = new(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());

        BigInteger d = new(1, privateKeyBytes);
        ECPrivateKeyParameters privParams = new(d, ecSpec);

        byte[] xBytes = new byte[24];
        byte[] yBytes = new byte[24];
        Array.Copy(remotePublicKeyBytes, 0, xBytes, 0, 24);
        Array.Copy(remotePublicKeyBytes, 24, yBytes, 0, 24);

        BigInteger x = new(1, xBytes);
        BigInteger y = new(1, yBytes);
        Org.BouncyCastle.Math.EC.ECPoint q = ecSpec.Curve.CreatePoint(x, y);
        ECPublicKeyParameters pubParams = new(q, ecSpec);

        ECDHBasicAgreement agreement = new();
        agreement.Init(privParams);
        BigInteger secret = agreement.CalculateAgreement(pubParams);

        byte[] secretBytes = secret.ToByteArrayUnsigned();
        byte[] result = new byte[48];
        Array.Copy(secretBytes, 0, result, 48 - Math.Min(48, secretBytes.Length), Math.Min(48, secretBytes.Length));
        return result;
    }

    public static byte[] Aes128CbcEncrypt(byte[] key, byte[] iv, byte[] plainData)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainData, 0, plainData.Length);
        }
        return ms.ToArray();
    }

    public static byte[] Aes128EcbEncrypt(byte[] key, byte[] plainData)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainData, 0, plainData.Length);
        }
        return ms.ToArray();
    }

    public static byte[] Aes128EcbDecrypt(byte[] key, byte[] cipherData)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherData, 0, cipherData.Length);
        }
        return ms.ToArray();
    }

    public static uint ComputeCrc32(byte[] data)
    {
        return Crc32.HashToUInt32(data);
    }
}

