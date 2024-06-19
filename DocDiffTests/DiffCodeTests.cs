using DocDiffStd;
using NUnit.Framework;

namespace DocDiffTests;

[TestFixture]
public class DiffCodeTests
{
    [Test]
    public void create_a_text_diff_code_from_document_changes_and_reapply()
    {
        var code = DiffCode.BuildDiffCode(new Differences(Samples.LongLeft, Samples.LongRight, Differences.PerWord));

        Console.WriteLine($"Encoded to {code.Length} chars");
        Console.WriteLine(code);

        var right = DiffCode.BuildRevision(Samples.LongLeft, code);
        
        Console.WriteLine(right);
        
        Assert.That(right, Is.EqualTo(Samples.LongRight));
    }

    [Test]
    public void create_a_binary_patch_file_from_document_changes_and_reapply()
    {
        var frags = new Differences(Samples.LongLeft, Samples.LongRight, Differences.PerWord);
        var code = DiffCode.StorageDiffCode(frags);

        Console.WriteLine($"Encoded to {code.Length} bytes");

        var right = DiffCode.BuildRevision(Samples.LongLeft, code);
        
        Assert.That(right, Is.EqualTo(Samples.LongRight));
    }

    [Test]
    public void create_a_url_safe_patch_from_document_changes_and_reapply()
    {
        var code = DiffCode.CompressedDiffCode(new Differences(Samples.LongLeft, Samples.LongRight, Differences.PerWord));

        Console.WriteLine($"Encoded to {code.Length} chars");
        Console.WriteLine(code);

        var right = DiffCode.BuildRevision(Samples.LongLeft, code);
        
        Console.WriteLine(right);
        
        Assert.That(right, Is.EqualTo(Samples.LongRight));
    }

    [Test]
    public void rebuild_difference_fragments_from_a_small_text_diff_code()
    {
        var diff = new Differences(Samples.ShortLeft, Samples.ShortRight, Differences.PerWord);
        var originalFrags = diff.ToList();
        Console.WriteLine(string.Join("\r\n", originalFrags.Select(f=>f.ToString())));
        Console.WriteLine("==================================================");
        
        var code = DiffCode.BuildDiffCode(diff);

        Console.WriteLine($"Encoded to {code.Length} chars");
        Console.WriteLine(code);

        var recoveredFrags = DiffCode.BuildChanges(Samples.ShortLeft, code);
        var recoveredText = DiffCode.BuildRevision(Samples.ShortLeft, DiffCode.BuildDiffCode(recoveredFrags));

        Console.WriteLine("==================================================");
        Console.WriteLine(string.Join("\r\n", recoveredFrags.Select(f=>f.ToString())));
        
        Assert.That(recoveredText, Is.EqualTo(Samples.ShortRight));
        Assert.That(recoveredFrags, Is.EqualTo(originalFrags).AsCollection);
    }
    
    [Test]
    public void rebuild_difference_fragments_from_a_large_text_diff_code()
    {
        var diff = new Differences(Samples.LongLeft, Samples.LongRight, Differences.PerWord);
        var originalFrags = diff.ToList();
        
        Console.WriteLine(string.Join("\r\n", originalFrags.Take(20).Select(f=>f.ToString())));
        Console.WriteLine("==================================================");
        
        var code = DiffCode.BuildDiffCode(diff);

        var recoveredFrags = DiffCode.BuildChanges(Samples.LongLeft, code);
        var recoveredText = DiffCode.BuildRevision(Samples.LongLeft, DiffCode.BuildDiffCode(recoveredFrags));
        
        Console.WriteLine(string.Join("\r\n", recoveredFrags.Take(20).Select(f=>f.ToString())));
        
        Assert.That(recoveredText, Is.EqualTo(Samples.LongRight));
        Assert.That(recoveredFrags, Is.EqualTo(originalFrags).AsCollection);
    }
}