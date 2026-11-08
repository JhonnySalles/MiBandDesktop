using System;
using Xunit;
using MiBandDesktop.Core.Protocol;

namespace MiBandDesktop.Tests;

public class MiBandCryptoTests
{
    [Fact]
    public void TestEcdhKeyPairAndSharedSecret()
    {
        var (privA, pubA) = HuamiCryptoHelper.GenerateKeyPair();
        var (privB, pubB) = HuamiCryptoHelper.GenerateKeyPair();

        Assert.Equal(24, privA.Length);
        Assert.Equal(48, pubA.Length);
        Assert.Equal(24, privB.Length);
        Assert.Equal(48, pubB.Length);

        var secretA = HuamiCryptoHelper.CalculateSharedSecret(privA, pubB);
        var secretB = HuamiCryptoHelper.CalculateSharedSecret(privB, pubA);

        Assert.Equal(48, secretA.Length);
        Assert.Equal(48, secretB.Length);
        Assert.Equal(secretA, secretB);
    }

    [Fact]
    public void TestCrc32()
    {
        byte[] testData = [0x01, 0x02, 0x03, 0x04];
        uint crc = HuamiCryptoHelper.ComputeCrc32(testData);
        Assert.True(crc > 0);
    }
}

