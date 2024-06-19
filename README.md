Doc Diff
========

What is it?
-----------

A file difference/comparison class. Performs much the same function as the Diff programs used in version control.
Compares two strings in chunks as determined by a Regex splitter.

### Differences

`Differences` takes two documents and emits a set of difference fragments.

Each fragment is either `deleted`, `inserted`, or `unchanged` between the two versions.
These fragments are more suited to visual display than change analysis.

There are a few splitters provided: `Differences.PerSentence`, `Differences.PerLine`, `Differences.PerWord`, `Differences.PerCharacter`.
Smaller splits result in more complex diffs, and use more memory and compute resource. `PerWord` or `PerLine` is probably best for most cases.

```csharp
Changes = new Differences(oldVersion, newVersion, Differences.PerWord);

foreach (var change in fragments)
{
    Console.WriteLine($"{change.Type}: '{change.Content}'");
}
```

```html
<style>
    .i {color:black; background-color:#80FF80; padding:0; margin:0;}
    .d {color:#FFa0a0; background-color:inherit; padding:0; margin:0;}
    .u {color:#707070; background-color:inherit; padding:0; margin:0;}
</style>

<div class="text-center">
    @foreach (var change in Model.Changes)
    {
        <span class="@change.TypeString">@change.Content</span>
    }
</div>
```

### DiffCode

This is a basic patch/match tool.

The tools in `DiffCode` can be used to analyse differences, and store/retrieve various revisions of files.
The Decode/Encode methods have variants which are suitable for data transmission and database storage.

```csharp
var changes = new Differences(left, right, Differences.PerWord);

var encodedChanges = DiffCode.StorageDiffCode(changes);

// Store 'encoded' and 'left'.
// We can then regenerate 'right':

var right = DiffCode.BuildRevision(left, encodedChanges);
```

Please note
-----------

Regex splitters omit the matched parts unless they are in capturing groups. See that the examples in Default.aspx.cs (from the static 'Diff') are all in capturing groups.
