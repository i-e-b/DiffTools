using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DocDiffStd;

/// <summary>
/// Document differences between to chunks of text
/// </summary>
public class Differences : IEnumerable<Fragment> {
	/// <summary>Maximum RAM to use for optimisation search, in bytes</summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public static int AcceptableRam {
		get {
			const int megs = 500;
			return megs * 1024 * 256; // the 256 is because we're working with 4 byte ints
		}
	}

	/// <summary>A Splitter that compares whole lines</summary>
	public static Regex PerLine => new("($)", RegexOptions.Multiline);

	/// <summary>A Splitter that compares by word</summary>
	public static Regex PerWord => new(@"(\w+)");

	/// <summary>A Splitter that compares groups split by non alpha-numeric chars</summary>
	public static Regex PerSentence => new(@"([^\w\d\s])");

	/// <summary>A Splitter that compares every character individually</summary>
	public static Regex PerCharacter => new("(.)");

	/// <summary>
	/// Prepare a set of differences from 'Old' to 'New', in chunks determined
	/// by the 'Splitter' regex.
	/// </summary>
	/// <param name="oldText">Old version string (changes from this are 'deletions')</param>
	/// <param name="newText">New version string (changes to this are 'insertions')</param>
	/// <param name="splitter">An expression to break up the two strings by.
	/// Regex.Split() method is used.</param>
	public Differences (string oldText, string newText, Regex splitter) {
		if (splitter == null
		    || oldText == null
		    || newText == null) throw new ArgumentException("Required parameter was null");
		_leftFragments = splitter.Split(oldText);
		_rightFragments = splitter.Split(newText);

		var leftData = new DiffData(DiffCodes(_leftFragments, false));
		var rightData = new DiffData(DiffCodes(_rightFragments, false));

		var max = leftData.Length + rightData.Length + 1;
		var memory = 2 * max + 2;
		if ((memory * 2) <= AcceptableRam) {
			// vector for the (0,0) to (x,y) search
			var downVector = new int[memory];
			// vector for the (u,v) to (N,M) search
			var upVector = new int[memory];

			LongestCommonSubsequence(leftData, 0, leftData.Length, rightData, 0, rightData.Length, downVector, upVector);
		} else {
			throw new OutOfMemoryException("Memory usage for LCS would exceed specified parameters.");
		}

		Optimize(leftData);
		Optimize(rightData);

		_fragmentDifferences = CreateDiffs(leftData, rightData);
	}

	#region Inner Workings
	/// <summary> difference fragments</summary>
	private readonly string[] _leftFragments;

	/// <summary> difference fragments</summary>
	private readonly string[] _rightFragments;

	private readonly DiffChange[] _fragmentDifferences;
	/// <summary>
	/// If a sequence of modified lines starts with a line that contains the same content
	/// as the line that appends the changes, the difference sequence is modified so that the
	/// appended line and not the starting line is marked as modified.
	/// This leads to more readable diff sequences when comparing text files by line.
	/// </summary>
	private static void Optimize (DiffData data) {
		var startPos = 0;
		while (startPos < data.Length) {
			while ((startPos < data.Length) && (data.Modified[startPos] == false))
				startPos++;
			var endPos = startPos;
			while ((endPos < data.Length) && (data.Modified[endPos]))
				endPos++;

			if ((endPos < data.Length) && (data.Data[startPos] == data.Data[endPos])) {
				data.Modified[startPos] = false;
				data.Modified[endPos] = true;
			} else {
				startPos = endPos;
			}
		}
	}

	/// <summary>
	/// Convert an array of strings into an array of hash codes
	/// </summary>
	private static int[] DiffCodes (string[] fragments, bool ignoreCase) {
		var codes = new int[fragments.Length];
		if (ignoreCase) {
			for (var n = 0; n < fragments.Length; n++) {
				codes[n] = fragments[n].ToLowerInvariant().GetHashCode();
			}
		} else {
			for (var n = 0; n < fragments.Length; n++) {
				codes[n] = fragments[n].GetHashCode();
			}
		}

		return codes;
	}

	/// <summary>
	/// This is the algorithm to find the Shortest Middle Snake (SMS).
	/// Eugene Myers, Algorithmica Vol. 1 No. 2, 1986, p 251.
	/// Implementation based on one by Matthias Hertel.
	/// </summary><remarks>There are no comments. If you don't get it, read a paper -- you won't get
	/// much from reading the code.</remarks>
	private static SMSRD ShortestMiddleSnake (DiffData dataA, int lowerA, int upperA, DiffData dataB, int lowerB, int upperB, int[] downVector, int[] upVector) {
		
		var max = dataA.Length + dataB.Length + 1;
		int downK = lowerA - lowerB, upK = upperA - upperB;
		var delta = (upperA - lowerA) - (upperB - lowerB);
		var oddDelta = (delta & 1) != 0;
		var downOffset = max - downK;
		var upOffset = max - upK;
		var maxD = ((upperA - lowerA + upperB - lowerB) / 2) + 1;
		downVector[downOffset + downK + 1] = lowerA;
		upVector[upOffset + upK - 1] = upperA;

		unchecked { // helps speed up the tons of array access being done, at the expense of boundary safety.
			for (var d = 0; d <= maxD; d++) {
				SMSRD ret;
				for (var k = downK - d; k <= downK + d; k += 2) {
					int x;
					if (k == downK - d) {
						x = downVector[downOffset + k + 1];
					} else {
						x = downVector[downOffset + k - 1] + 1;
						if ((k < downK + d) && (downVector[downOffset + k + 1] >= x))
							x = downVector[downOffset + k + 1];
					}
					var y = x - k;
					while ((x < upperA) && (y < upperB) && (dataA.Data[x] == dataB.Data[y])) {
						x++; y++;
					}
					downVector[downOffset + k] = x;
					if (!oddDelta || (upK - d >= k) || (k >= upK + d)) continue;
					if (upVector[upOffset + k] > downVector[downOffset + k]) continue;
					ret.x = downVector[downOffset + k];
					ret.y = downVector[downOffset + k] - k;
					return (ret);
				}
				for (var k = upK - d; k <= upK + d; k += 2) {
					int x;
					if (k == upK + d) {
						x = upVector[upOffset + k - 1];
					} else {
						x = upVector[upOffset + k + 1] - 1;
						if ((k > upK - d) && (upVector[upOffset + k - 1] < x))
							x = upVector[upOffset + k - 1];
					}
					var y = x - k;
					while ((x > lowerA) && (y > lowerB) && (dataA.Data[x - 1] == dataB.Data[y - 1])) {
						x--; y--;
					}
					upVector[upOffset + k] = x;
					if ((oddDelta || (downK - d > k)) || (k > downK + d)) continue;
					if (upVector[upOffset + k] > downVector[downOffset + k]) continue;
					ret.x = downVector[downOffset + k];
					ret.y = downVector[downOffset + k] - k;
					return (ret);
				}
			}
		}
		throw new Exception("SMS: Boundary exception!"); // Should always hit one of the return cases in the loop above
	}


	/// <summary>
	/// This is the divide-and-conquer implementation of the longest common subsequence (LCS) 
	/// algorithm.
	/// The published algorithm passes recursively parts of the A and B sequences.
	/// To avoid copying these arrays the lower and upper bounds are passed while the sequences stay constant.
	/// </summary>
	private static void LongestCommonSubsequence (DiffData dataA, int lowerA, int upperA, DiffData dataB, int lowerB, int upperB, int[] downVector, int[] upVector) {
		unchecked {
			while (lowerA < upperA && lowerB < upperB && dataA.Data[lowerA] == dataB.Data[lowerB]) {
				lowerA++; lowerB++;
			}
			while (lowerA < upperA && lowerB < upperB && dataA.Data[upperA - 1] == dataB.Data[upperB - 1]) {
				--upperA; --upperB;
			}
			if (lowerA == upperA) {
				while (lowerB < upperB)
					dataB.Modified[lowerB++] = true;
			} else if (lowerB == upperB) {
				while (lowerA < upperA)
					dataA.Modified[lowerA++] = true;
			} else {
				var smsrd = ShortestMiddleSnake(dataA, lowerA, upperA, dataB, lowerB, upperB, downVector, upVector);
				LongestCommonSubsequence(dataA, lowerA, smsrd.x, dataB, lowerB, smsrd.y, downVector, upVector);
				LongestCommonSubsequence(dataA, smsrd.x, upperA, dataB, smsrd.y, upperB, downVector, upVector);
			}
		}
	}


	/// <summary>Scan the tables of which lines are inserted and deleted,
	/// producing an edit script in forward order.</summary>
	private static DiffChange[] CreateDiffs (DiffData dataA, DiffData dataB) {
		var a = new List<DiffChange>();

		var lineA = 0;
		var lineB = 0;
		while (lineA < dataA.Length || lineB < dataB.Length) {
			if ((lineA < dataA.Length) && (!dataA.Modified[lineA])
			                           && (lineB < dataB.Length) && (!dataB.Modified[lineB])) {
				lineA++;
				lineB++;

			} else {
				var startA = lineA;
				var startB = lineB;

				while (lineA < dataA.Length && (lineB >= dataB.Length || dataA.Modified[lineA])) lineA++;
				while (lineB < dataB.Length && (lineA >= dataA.Length || dataB.Modified[lineB])) lineB++;

				if ((startA < lineA) || (startB < lineB)) {
					var aItem = new DiffChange { StartA = startA, StartB = startB, DeletedA = lineA - startA, InsertedB = lineB - startB };
					a.Add(aItem);
				}
			}
		}

		return a.ToArray();
	}

	#region Infrastructure junk
	/// <summary>
	/// Enumerate change fragments between the two versions
	/// </summary>
	public IEnumerator<Fragment> GetEnumerator () {
		return OutputDiffs();
	}

	IEnumerator IEnumerable.GetEnumerator () {
		return OutputDiffs();
	}

	private IEnumerator<Fragment> OutputDiffs () {
		if (_fragmentDifferences.Length <= 0) { } else {
			var n = 0;
			var p = 0;
			foreach (var aItem in _fragmentDifferences) {
				// write unchanged lines
				var fragsOld = new Fragment(FragmentType.Unchanged, p);
				while ((n < aItem.StartB) && (n < _rightFragments.Length)) {
					fragsOld.Join(_rightFragments[n]);
					p += _rightFragments[n].Length;
					n++;
				}
				if (!string.IsNullOrEmpty(fragsOld.SplitPart)) yield return fragsOld;

				// write deleted lines
				var fragsGone = new Fragment(FragmentType.Deleted, p);
				for (var m = 0; m < aItem.DeletedA; m++) {
					fragsGone.Join(_leftFragments[aItem.StartA + m]);
				}
				if (!string.IsNullOrEmpty(fragsGone.SplitPart)) yield return fragsGone;

				// write inserted lines
				var fragsAdd = new Fragment(FragmentType.Inserted, p);
				while (n < aItem.StartB + aItem.InsertedB) {
					fragsAdd.Join(_rightFragments[n]);
					p += _rightFragments[n].Length;
					n++;
				}
				if (!string.IsNullOrEmpty(fragsAdd.SplitPart)) yield return fragsAdd;
			}

			// write rest of unchanged lines
			var fragsRunoff = new Fragment(FragmentType.Unchanged, p);
			while (n < _rightFragments.Length) {
				fragsRunoff.Join(_rightFragments[n]);
				n++;
			}
			if (!string.IsNullOrEmpty(fragsRunoff.SplitPart)) yield return fragsRunoff;
		}
	}

	/// <summary>Shortest Middle Snake Return Data</summary>
	// ReSharper disable once InconsistentNaming
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private struct SMSRD {
		internal int x, y;
	}

	/// <summary>
	/// Type of comparison fragment, either 'unchanged', 'inserted' or 'deleted'.
	/// </summary>
	public enum FragmentType {
		/// <summary>
		/// Fragment is the same between both documents
		/// </summary>
		Unchanged,
		
		/// <summary>
		/// Fragment exists in the old document, but not in the new one
		/// </summary>
		Deleted,
		
		/// <summary>
		/// Fragment exists in the new document, but not in the old one
		/// </summary>
		Inserted
	}

	#endregion
	#endregion
}