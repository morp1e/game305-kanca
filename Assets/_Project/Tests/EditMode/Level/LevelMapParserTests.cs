using System;
using Game.Editor.Level;
using NUnit.Framework;

namespace Game.Tests.EditMode.Level
{
    public sealed class LevelMapParserTests
    {
        [Test] public void Parse_RowsAreStoredTopDownAndWorldYStartsAtBottom() { var map=LevelMapParser.Parse("S#\n##"); Assert.AreEqual("S#",map.Rows[0]); Assert.AreEqual('#',LevelMapParser.GetCell(map,0,0)); Assert.AreEqual('S',LevelMapParser.GetCell(map,0,1)); }
        [Test] public void Parse_ReadsCommentsAndHeaders() { var map=LevelMapParser.Parse(";ders\n@name Level_01\n@killHeight -12\nS"); Assert.AreEqual(1,map.Comments.Count); Assert.AreEqual("Level_01",map.Headers["name"]); Assert.AreEqual(-12f,map.KillHeight); }
        [Test] public void Parse_RequiresExactlyOnePlayer() { Assert.Throws<FormatException>(()=>LevelMapParser.Parse("##")); Assert.Throws<FormatException>(()=>LevelMapParser.Parse("SS")); }
        [Test] public void IsTopTile_UsesEmptyCellAbove() { var map=LevelMapParser.Parse("# .\nS##"); Assert.IsFalse(LevelMapParser.IsTopTile(map,0,0)); Assert.IsTrue(LevelMapParser.IsTopTile(map,1,0)); }
        [Test] public void GetCell_PadsShortRowsWithEmptyCells() { var map=LevelMapParser.Parse("S\n###"); Assert.AreEqual(3,map.Width); Assert.AreEqual(' ',LevelMapParser.GetCell(map,2,1)); }
        [Test] public void Parse_ReportsUnknownCharacterWithLineAndColumn() { var map=LevelMapParser.Parse("S?", "S#".ToCharArray()); Assert.AreEqual(1,map.Warnings.Count); StringAssert.Contains("satır 1, sütun 2",map.Warnings[0]); }
        [Test] public void Parse_TrailingNewlineDoesNotAddAnExtraMapRow() { var map=LevelMapParser.Parse("S\n"); Assert.AreEqual(1,map.Height); }
    }
}
