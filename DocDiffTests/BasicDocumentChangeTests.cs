using DocDiffStd;
using NUnit.Framework;

namespace DocDiffTests;

[TestFixture]
public class BasicDocumentChangeTests
{
    [Test]
    public void can_detect_changes_per_word()
    {
        var differences = new Differences(ShortLeft, ShortRight, Differences.PerWord).ToList();

        Assert.That(differences.Count, Is.EqualTo(12));

        foreach (var change in differences)
        {
            Console.WriteLine($"{change.Type}: '{change.SplitPart}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_line()
    {
        var differences = new Differences(ShortLeft, ShortRight, Differences.PerLine).ToList();

        Assert.That(differences.Count, Is.EqualTo(6));

        foreach (var change in differences)
        {
            Console.WriteLine($"{change.Type}: '{change.SplitPart.Replace("\r"," ").Replace("\n"," ")}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_sentence()
    {
        var differences = new Differences(ShortLeft, ShortRight, Differences.PerSentence).ToList();

        Assert.That(differences.Count, Is.EqualTo(6));

        foreach (var change in differences)
        {
            Console.WriteLine($"{change.Type}: '{change.SplitPart.Replace("\r","").Replace("\n","")}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_character()
    {
        var differences = new Differences(ShortLeft, ShortRight, Differences.PerCharacter).ToList();

        Assert.That(differences.Count, Is.EqualTo(30));

        foreach (var change in differences)
        {
            Console.WriteLine($"{change.Type}: '{change.SplitPart}'");
        }
    }
    
    #region Samples
    private const string ShortLeft = @"There now is your insular city of the Manhattoes, belted round by
wharves as Indian isles by coral reefs—commerce surrounds it with her
surf. Right and left, the streets take you waterward. Its extreme
downtown is the battery, where that noble mole is washed by waves, and
cooled by breezes, which a few hours previous were out of sight of
land. Look at the crowds of water-gazers there.";

    private const string ShortRight = @"Here now is your island city of the Manhatten, belted round by
wharves as Indian isles by coral reefs—commerce surrounds it with her
surf. Right and left, the streets take you to the sea. Its extreme
downtown is the battery, where that noble mole is washed by waves, and
cooled by breezes, which a few hours previous were out of sight of
land. Look at the crowds of water-gazers there.";

    #endregion Samples
}