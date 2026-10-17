using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;

namespace UnitTests.Game.Memory.Adventure.AdventureStageTests;

[TestClass]
public class AdventureStageDecodeTests
{
    private static StageRecord Raw(uint tag, uint score = 0) => new() { Tag = tag, Score = score };

    [DataTestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(100)]
    [DataRow(int.MaxValue)]
    public void Decode_OutOfRangeIndex_Throws(int index)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => AdventureDecode.Decode(index, Raw(0)));
    }

    [TestMethod]
    public void Decode_Bit0_IsGameCleared()
    {
        AdventureStage set = AdventureDecode.Decode(0, Raw(0b1));
        Assert.IsTrue(set.GameCleared);
        Assert.AreEqual(0, set.Stars);
        Assert.IsFalse(set.Visited);
        Assert.IsFalse(set.ApUnlocked);

        Assert.IsFalse(AdventureDecode.Decode(0, Raw(0b0)).GameCleared);
    }

    [TestMethod]
    public void Decode_Bit31_IsApUnlocked()
    {
        Assert.IsTrue(AdventureDecode.Decode(0, Raw(1u << 31)).ApUnlocked);
        Assert.IsFalse(AdventureDecode.Decode(0, Raw(0)).ApUnlocked);
    }

    [TestMethod]
    public void Decode_Bit8_IsVisited()
    {
        Assert.IsTrue(AdventureDecode.Decode(0, Raw(1u << 8)).Visited);
        Assert.IsFalse(AdventureDecode.Decode(0, Raw(0)).Visited);
    }

    [DataTestMethod]
    [DataRow(0u, 0)]
    [DataRow(0b010u, 1)]
    [DataRow(0b100u, 2)]
    [DataRow(0b110u, 3)]
    public void Decode_Bits1And2_AreStars(uint tag, int expectedStars)
    {
        Assert.AreEqual(expectedStars, AdventureDecode.Decode(0, Raw(tag)).Stars);
    }

    [TestMethod]
    public void Decode_PassesScoreThrough()
    {
        Assert.AreEqual(123456u, AdventureDecode.Decode(0, Raw(0, 123456u)).Score);
        Assert.AreEqual(uint.MaxValue, AdventureDecode.Decode(0, Raw(0, uint.MaxValue)).Score);
    }

    [TestMethod]
    public void Decode_AllFieldsSet()
    {
        uint tag = 0b1 | 0b110 | (1u << 8) | (1u << 31);
        AdventureStage s = AdventureDecode.Decode(0, Raw(tag, 42));
        Assert.IsTrue(s.GameCleared);
        Assert.AreEqual(3, s.Stars);
        Assert.IsTrue(s.Visited);
        Assert.IsTrue(s.ApUnlocked);
        Assert.AreEqual(42u, s.Score);
    }

    [TestMethod]
    public void Decode_IgnoresUnmappedBits()
    {
        uint noise = 0b0111_1000 | (0x7FFFu << 9);
        AdventureStage s = AdventureDecode.Decode(0, Raw(noise));
        Assert.IsFalse(s.GameCleared);
        Assert.AreEqual(0, s.Stars);
        Assert.IsFalse(s.Visited);
        Assert.IsFalse(s.ApUnlocked);
    }

    [TestMethod]
    public void Decode_StarsMaxAtThree()
    {
        AdventureStage s = AdventureDecode.Decode(0, Raw(0xFFFFFFFF));
        Assert.AreEqual(3, s.Stars);
        Assert.IsTrue(s.GameCleared);
        Assert.IsTrue(s.Visited);
        Assert.IsTrue(s.ApUnlocked);
    }

    [DataTestMethod]
    [DataRow(0, 1, 1)]
    [DataRow(9, 1, 10)]
    [DataRow(10, 2, 1)]
    [DataRow(64, 7, 5)]
    [DataRow(99, 10, 10)]
    public void Decode_IndexMapsToActStage(int index, int expectedAct, int expectedStage)
    {
        AdventureStage s = AdventureDecode.Decode(index, Raw(0));
        Assert.AreEqual(expectedAct, s.Act);
        Assert.AreEqual(expectedStage, s.Stage);
    }
}
