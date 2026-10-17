using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Game.Memory.Adventure.AdventureStageTests;

[TestClass]
public class AdventureStageEncodeTests
{
    private static AdventureStage Stage(
        bool cleared = true,
        bool ap = false,
        int stars = 0,
        uint score = 0,
        int act = 1,
        int stage = 1
    ) => new(
        Act: act,
        Stage: stage,
        GameCleared: cleared,
        ApUnlocked: ap,
        Stars: stars,
        Visited: false,
        Score: score
    );

    [TestMethod]
    public void Encode_ForcesClearedBit()
    {
        StageRecord r = AdventureEncode.Encode(Stage(cleared: false), rawTag: 0);
        Assert.AreEqual(1u, r.Tag & 0b1);
    }

    [TestMethod]
    public void Encode_ApUnlocked_SetsBit31()
    {
        StageRecord r = AdventureEncode.Encode(Stage(ap: true), rawTag: 0);
        Assert.AreEqual(1u << 31, r.Tag & (1u << 31));
    }

    [TestMethod]
    public void Encode_ApLocked_ClearsBit31()
    {
        StageRecord r = AdventureEncode.Encode(Stage(ap: false), rawTag: 1u << 31);
        Assert.AreEqual(0u, r.Tag & (1u << 31));
    }

    [DataTestMethod]
    [DataRow(0, 0b000u)]
    [DataRow(1, 0b010u)]
    [DataRow(2, 0b100u)]
    [DataRow(3, 0b110u)]
    public void Encode_WritesStarsToBits1And2(int stars, uint expectedStarBits)
    {
        StageRecord r = AdventureEncode.Encode(Stage(stars: stars), rawTag: 0);
        Assert.AreEqual(expectedStarBits, r.Tag & (0b11u << 1));
    }

    [TestMethod]
    public void Encode_OverwritesStaleStarsWhenLowering()
    {
        StageRecord r = AdventureEncode.Encode(Stage(stars: 1), rawTag: 0b110);
        Assert.AreEqual(0b010u, r.Tag & (0b11u << 1));
    }

    [TestMethod]
    public void Encode_PreservesUnknownBits()
    {
        uint unknown = (0b11u << 3) | (1u << 8) | (1u << 15);
        StageRecord r = AdventureEncode.Encode(Stage(stars: 2), rawTag: unknown);
        Assert.AreEqual(0b11u << 3, r.Tag & (0b11u << 3), "medal bits lost (medal is from bayoen-stars, no idea what it actually sets)");
        Assert.AreEqual(1u << 8, r.Tag & (1u << 8), "visited bit lost");
        Assert.AreEqual(1u << 15, r.Tag & (1u << 15), "other high bit lost");
    }

    [TestMethod]
    public void Encode_PassesScoreThrough()
    {
        Assert.AreEqual(999u, AdventureEncode.Encode(Stage(score: 999), 0).Score);
    }

    [TestMethod]
    public void Encode_RoundTrip()
    {
        uint original = 0b1 | 0b100 | (1u << 31) | (0b11u << 3);
        AdventureStage decoded = AdventureDecode.Decode(0, new StageRecord { Tag = original, Score = 7 });
        StageRecord reencoded = AdventureEncode.Encode(decoded, original);
        Assert.AreEqual(original, reencoded.Tag);
        Assert.AreEqual(7u, reencoded.Score);
    }

    [TestMethod]
    public void Encode_RoundTrip_Flipped()
    {
        AdventureStage original = Stage(cleared: true, ap: true, stars: 2, score: 55);
        AdventureStage result = AdventureDecode.Decode(0, AdventureEncode.Encode(original, rawTag: 0));
        Assert.IsTrue(result.GameCleared);
        Assert.IsTrue(result.ApUnlocked);
        Assert.AreEqual(2, result.Stars);
        Assert.AreEqual(55u, result.Score);
    }

    [DataTestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(4)]
    [DataRow(int.MaxValue)]
    public void Encode_StarsOutOfRange_Throws(int stars)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => AdventureEncode.Encode(Stage(stars: stars), rawTag: 0));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(10)]
    public void Encode_ActNotOutOfRange_DoesntThrow(int act) =>
        AdventureEncode.Encode(Stage(act: act), rawTag: 0);

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(11)]
    public void Encode_ActOutOfRange_Throws(int act)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => AdventureEncode.Encode(Stage(act: act), rawTag: 0));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(10)]
    public void Encode_StageNotOutOfRange_DoesntThrow(int stage) =>
        AdventureEncode.Encode(Stage(stage: stage), rawTag: 0);

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(11)]
    public void Encode_StageOutOfRange_Throws(int stage)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => AdventureEncode.Encode(Stage(stage: stage), rawTag: 0));
    }
}
