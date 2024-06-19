using DocDiffStd;
using NUnit.Framework;

namespace DocDiffTests;

[TestFixture]
public class BasicDocumentChangeTests
{
    [Test]
    public void can_detect_changes_per_word()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerWord).ToList();

        Assert.That(fragments.Count, Is.EqualTo(12));

        foreach (var change in fragments)
        {
            Console.WriteLine($"{change.Type}: '{change.Content}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_line()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerLine).ToList();

        Assert.That(fragments.Count, Is.EqualTo(6));

        foreach (var change in fragments)
        {
            Console.WriteLine($"{change.Type}: '{change.Content.Replace("\r"," ").Replace("\n"," ")}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_sentence()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerSentence).ToList();

        Assert.That(fragments.Count, Is.EqualTo(6));

        foreach (var change in fragments)
        {
            Console.WriteLine($"{change.Type}: '{change.Content.Replace("\r","").Replace("\n","")}'");
        }
    }
    
    [Test]
    public void can_detect_changes_per_character()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerCharacter).ToList();

        Assert.That(fragments.Count, Is.EqualTo(30));

        foreach (var change in fragments)
        {
            Console.WriteLine($"{change.Type}: '{change.Content}'");
        }
    }

    [Test]
    public void can_get_i_u_d_codes_for_fragments()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerCharacter).ToList();

        foreach (var change in fragments)
        {
            switch (change.Type)
            {
                case Differences.FragmentType.Unchanged:
                    Assert.That(change.TypeString, Is.EqualTo("u"));
                    break;
                case Differences.FragmentType.Deleted:
                    Assert.That(change.TypeString, Is.EqualTo("d"));
                    break;
                case Differences.FragmentType.Inserted:
                    Assert.That(change.TypeString, Is.EqualTo("i"));
                    break;
                default:
                    Assert.Fail("unknown type");
                    break;
            }
        }
    }

    [Test]
    public void can_get_positions_for_fragments()
    {
        var fragments = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerCharacter).ToList();

        var pos = 0;
        foreach (var change in fragments)
        {
            switch (change.Type)
            {
                case Differences.FragmentType.Unchanged:
                    Assert.That(change.Position, Is.EqualTo(pos));
                    pos += change.Length;
                    break;
                case Differences.FragmentType.Deleted:
                    Assert.That(change.Position, Is.EqualTo(pos));
                    break;
                case Differences.FragmentType.Inserted:
                    Assert.That(change.Position, Is.EqualTo(pos));
                    pos += change.Length;
                    break;
                default:
                    Assert.Fail("unknown type");
                    break;
            }
        }
    }
}