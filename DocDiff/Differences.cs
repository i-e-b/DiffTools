using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DocDiff {
	public class Differences : IEnumerable<Fragment> {
		/// <summary>Maximum RAM to use for optimisation search, in bytes</summary>
		public int AcceptableRAM {
			get {
				const int megs = 500;
				return megs * 1024 * 256; // the 256 is because we're working with 4 byte ints
			}
		}

		/// <summary>A Splitter that compares whole lines</summary>
		public static Regex PerLine { get { return new Regex(@"($)", RegexOptions.Multiline); } }

		/// <summary>A Splitter that compares by word</summary>
		public static Regex PerWord { get { return new Regex(@"(\w+)"); } }

		/// <summary>A Splitter that compares groups split by non alpha-numeric chars</summary>
		public static Regex PerSentence { get { return new Regex(@"([^\w\d\s])"); } }

		/// <summary>A Splitter that compares every character individually</summary>
		public static Regex PerCharacter { get { return new Regex(@"(.)"); } }

		/// <summary>
		/// Prepare a set of differences from 'Old' to 'New', in chunks determined
		/// by the 'Splitter' regex.
		/// </summary>
		/// <param name="Old">Old version string (changes from this are 'deletions')</param>
		/// <param name="New">New version string (changes to this are 'insertions')</param>
		/// <param name="Splitter">An expression to break up the two strings by.
		/// Regex.Split() method is used.</param>
		public Differences (string Old, string New, Regex Splitter) {
			if (Splitter == null
				|| Old == null
				|| New == null) throw new ArgumentException("Required parameter was null");
			LeftFragments = Splitter.Split(Old);
			RightFragments = Splitter.Split(New);

			var leftData = new DiffData(DiffCodes(LeftFragments, false));
			var rightData = new DiffData(DiffCodes(RightFragments, false));

			int max = leftData.Length + rightData.Length + 1;
			int memory = 2 * max + 2;
			if ((memory * 2) <= AcceptableRAM) {
				// vector for the (0,0) to (x,y) search
				var downVector = new int[memory];
				// vector for the (u,v) to (N,M) search
				var upVector = new int[memory];

				LCS(leftData, 0, leftData.Length, rightData, 0, rightData.Length, downVector, upVector);
			} else {
				throw new OutOfMemoryException("Memory usage for LCS would exceed specified parameters.");
			}

			Optimize(leftData);
			Optimize(rightData);

			FragmentDifferences = CreateDiffs(leftData, rightData);
		}

		#region Inner Workings
		protected string[] LeftFragments, RightFragments;
		protected DiffChange[] FragmentDifferences;
		/// <summary>
		/// If a sequence of modified lines starts with a line that contains the same content
		/// as the line that appends the changes, the difference sequence is modified so that the
		/// appended line and not the starting line is marked as modified.
		/// This leads to more readable diff sequences when comparing text files by line.
		/// </summary>
		private static void Optimize (DiffData Data) {
			int startPos = 0;
			while (startPos < Data.Length) {
				while ((startPos < Data.Length) && (Data.Modified[startPos] == false))
					startPos++;
				int endPos = startPos;
				while ((endPos < Data.Length) && (Data.Modified[endPos]))
					endPos++;

				if ((endPos < Data.Length) && (Data.Data[startPos] == Data.Data[endPos])) {
					Data.Modified[startPos] = false;
					Data.Modified[endPos] = true;
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
				for (int n = 0; n < fragments.Length; n++) {
					codes[n] = fragments[n].ToLowerInvariant().GetHashCode();
				}
			} else {
				for (int n = 0; n < fragments.Length; n++) {
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
		private static SMSRD SMS (DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB,
		  int[] DownVector, int[] UpVector) {
			SMSRD ret;
			int MAX = DataA.Length + DataB.Length + 1;
			int DownK = LowerA - LowerB, UpK = UpperA - UpperB;
			int Delta = (UpperA - LowerA) - (UpperB - LowerB);
			bool oddDelta = (Delta & 1) != 0;
			int DownOffset = MAX - DownK;
			int UpOffset = MAX - UpK;
			int MaxD = ((UpperA - LowerA + UpperB - LowerB) / 2) + 1;
			DownVector[DownOffset + DownK + 1] = LowerA;
			UpVector[UpOffset + UpK - 1] = UpperA;

			unchecked { // helps speed up the tons of array access being done, at the expense of boundary safety.
				for (int D = 0; D <= MaxD; D++) {
					for (int k = DownK - D; k <= DownK + D; k += 2) {
						int x;
						if (k == DownK - D) {
							x = DownVector[DownOffset + k + 1];
						} else {
							x = DownVector[DownOffset + k - 1] + 1;
							if ((k < DownK + D) && (DownVector[DownOffset + k + 1] >= x))
								x = DownVector[DownOffset + k + 1];
						}
						int y = x - k;
						while ((x < UpperA) && (y < UpperB) && (DataA.Data[x] == DataB.Data[y])) {
							x++; y++;
						}
						DownVector[DownOffset + k] = x;
						if (!oddDelta || (UpK - D >= k) || (k >= UpK + D)) continue;
						if (UpVector[UpOffset + k] > DownVector[DownOffset + k]) continue;
						ret.x = DownVector[DownOffset + k];
						ret.y = DownVector[DownOffset + k] - k;
						return (ret);
					}
					for (int k = UpK - D; k <= UpK + D; k += 2) {
						int x;
						if (k == UpK + D) {
							x = UpVector[UpOffset + k - 1];
						} else {
							x = UpVector[UpOffset + k + 1] - 1;
							if ((k > UpK - D) && (UpVector[UpOffset + k - 1] < x))
								x = UpVector[UpOffset + k - 1];
						}
						int y = x - k;
						while ((x > LowerA) && (y > LowerB) && (DataA.Data[x - 1] == DataB.Data[y - 1])) {
							x--; y--;
						}
						UpVector[UpOffset + k] = x;
						if ((oddDelta || (DownK - D > k)) || (k > DownK + D)) continue;
						if (UpVector[UpOffset + k] > DownVector[DownOffset + k]) continue;
						ret.x = DownVector[DownOffset + k];
						ret.y = DownVector[DownOffset + k] - k;
						return (ret);
					}
				}
			}
			throw new Exception("SMS: Boundary exception!");
		}


		/// <summary>
		/// This is the divide-and-conquer implementation of the longest common subsequence (LCS) 
		/// algorithm.
		/// The published algorithm passes recursively parts of the A and B sequences.
		/// To avoid copying these arrays the lower and upper bounds are passed while the sequences stay constant.
		/// </summary>
		private static void LCS (DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB, int[] DownVector, int[] UpVector) {
			unchecked {
				while (LowerA < UpperA && LowerB < UpperB && DataA.Data[LowerA] == DataB.Data[LowerB]) {
					LowerA++; LowerB++;
				}
				while (LowerA < UpperA && LowerB < UpperB && DataA.Data[UpperA - 1] == DataB.Data[UpperB - 1]) {
					--UpperA; --UpperB;
				}
				if (LowerA == UpperA) {
					while (LowerB < UpperB)
						DataB.Modified[LowerB++] = true;
				} else if (LowerB == UpperB) {
					while (LowerA < UpperA)
						DataA.Modified[LowerA++] = true;
				} else {
					SMSRD smsrd = SMS(DataA, LowerA, UpperA, DataB, LowerB, UpperB, DownVector, UpVector);
					LCS(DataA, LowerA, smsrd.x, DataB, LowerB, smsrd.y, DownVector, UpVector);
					LCS(DataA, smsrd.x, UpperA, DataB, smsrd.y, UpperB, DownVector, UpVector);
				}
			}
		}


		/// <summary>Scan the tables of which lines are inserted and deleted,
		/// producing an edit script in forward order.</summary>
		private static DiffChange[] CreateDiffs (DiffData DataA, DiffData DataB) {
			var a = new List<DiffChange>();

			int lineA = 0;
			int lineB = 0;
			while (lineA < DataA.Length || lineB < DataB.Length) {
				if ((lineA < DataA.Length) && (!DataA.Modified[lineA])
				  && (lineB < DataB.Length) && (!DataB.Modified[lineB])) {
					lineA++;
					lineB++;

				} else {
					int startA = lineA;
					int startB = lineB;

					while (lineA < DataA.Length && (lineB >= DataB.Length || DataA.Modified[lineA])) lineA++;
					while (lineB < DataB.Length && (lineA >= DataA.Length || DataB.Modified[lineB])) lineB++;

					if ((startA < lineA) || (startB < lineB)) {
						var aItem = new DiffChange { StartA = startA, StartB = startB, deletedA = lineA - startA, insertedB = lineB - startB };
						a.Add(aItem);
					}
				}
			}

			return a.ToArray();
		}

		#region Infrastructure junk
		public IEnumerator<Fragment> GetEnumerator () {
			return OutputDiffs();
		}

		IEnumerator IEnumerable.GetEnumerator () {
			return OutputDiffs();
		}

		private IEnumerator<Fragment> OutputDiffs () {
			if (FragmentDifferences == null || FragmentDifferences.Length <= 0) { } else {
				int n = 0;
				int p = 0;
				foreach (DiffChange aItem in FragmentDifferences) {
					// write unchanged lines
					var fragsOld = new Fragment(FragmentType.Unchanged, p);
					while ((n < aItem.StartB) && (n < RightFragments.Length)) {
						fragsOld.Join(RightFragments[n]);
						p += RightFragments[n].Length;
						n++;
					}
					if (!string.IsNullOrEmpty(fragsOld.SplitPart)) yield return fragsOld;

					// write deleted lines
					var fragsGone = new Fragment(FragmentType.Deleted, p);
					for (int m = 0; m < aItem.deletedA; m++) {
						fragsGone.Join(LeftFragments[aItem.StartA + m]);
					}
					if (!string.IsNullOrEmpty(fragsGone.SplitPart)) yield return fragsGone;

					// write inserted lines
					var fragsAdd = new Fragment(FragmentType.Inserted, p);
					while (n < aItem.StartB + aItem.insertedB) {
						fragsAdd.Join(RightFragments[n]);
						p += RightFragments[n].Length;
						n++;
					}
					if (!string.IsNullOrEmpty(fragsAdd.SplitPart)) yield return fragsAdd;
				}

				// write rest of unchanged lines
				var fragsRunoff = new Fragment(FragmentType.Unchanged, p);
				while (n < RightFragments.Length) {
					fragsRunoff.Join(RightFragments[n]);
					n++;
				}
				if (!string.IsNullOrEmpty(fragsRunoff.SplitPart)) yield return fragsRunoff;
			}
		}

		/// <summary>Shortest Middle Snake Return Data</summary>
		private struct SMSRD {
			internal int x, y;
		}

		/// <summary>
		/// Type of comparison fragment, either 'unchanged', 'inserted' or 'deleted'.
		/// </summary>
		public enum FragmentType {
			Unchanged, Deleted, Inserted
		}

		#endregion
		#endregion
	}
}
