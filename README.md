My own diff project, plus Neil Fraser's diff-match-patch in C# split into classes

Doc Diff
========

What is it?
-----------

A file difference/comparison class. Performs much the same function as the Diff programs used in version control.
Compares two strings in chunks as determined by a Regex splitter.

Output from the class in as an Enumerator. It is more suited to visual display than change analysis.

The active ingredient is [/Code/Diff.cs] in the [System.Text] namespace.

DiffCode
--------

The tools in [DiffCode.cs] can be used to analyse differences, and store/retrieve various revisions of files.
The Decode/Encode methods have variants which are suitable for data transmission and database storage.


Please note
-----------

Regex splitters omit the matched parts unless they are in capturing groups. See that the examples in Default.aspx.cs (from the static 'Diff') are all in capturing groups.

*Todo*
 - Split up diff-match-patch into a sensible class split
 - unit tests around DocDiff
 - Comparible performance tests around both libraries, see if there is anything useful to join.