/*
 * Copyright 2008 Google Inc. All Rights Reserved.
 * Author: fraser@google.com (Neil Fraser)
 * Author: anteru@developer.shelter13.net (Matthaeus G. Chajdas)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Diff Match and Patch -- Test Harness
 * http://code.google.com/p/google-diff-match-patch/
 */

using System;
using System.Collections.Generic;
using System.Text;
using DiffMatchPatch;
using NUnit.Framework;

namespace nicTest {
	[TestFixture]
	public class diff_match_patchTest : diff_match_patch {
		private static IEnumerable<string> diff_rebuildtexts(IEnumerable<Diff> diffs) {
			string[] text = {"", ""};
			foreach (Diff myDiff in diffs) {
				if (myDiff.operation != Operation.Insert) {
					text[0] += myDiff.text;
				}
				if (myDiff.operation != Operation.Delete) {
					text[1] += myDiff.text;
				}
			}
			return text;
		}

		[Test]
		public void diff_bisectTest() {
			var dmp = new diff_match_patchTest();
			// Normal.
			const string a = "cat";
			const string b = "map";
			// Since the resulting diff hasn't been normalized, it would be ok if
			// the insertion and deletion pairs are swapped.
			// If the order changes, tweak this test as required.
			var diffs = new List<Diff>{new Diff(Operation.Delete, "c"), new Diff(Operation.Insert, "m"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "t"), new Diff(Operation.Insert, "p")};
			Assert.AreEqual(diffs, dmp.diff_bisect(a, b, DateTime.MaxValue));

			// Timeout.
			diffs = new List<Diff>{new Diff(Operation.Delete, "cat"), new Diff(Operation.Insert, "map")};
			Assert.AreEqual(diffs, dmp.diff_bisect(a, b, DateTime.MinValue));
		}

		[Test]
		public void diff_charsToLinesTest() {
			var dmp = new diff_match_patchTest();
			// Convert chars up to lines.
			var diffs = new List<Diff>{
				new Diff(Operation.Equal, "\u0001\u0002\u0001"),
				new Diff(Operation.Insert, "\u0002\u0001\u0002")
			};
			var tmpVector = new List<string>{"", "alpha\n", "beta\n"};
			dmp.diff_charsToLines(diffs, tmpVector);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "alpha\nbeta\nalpha\n"),
				new Diff(Operation.Insert, "beta\nalpha\nbeta\n")
			}, diffs);

			// More than 256 to reveal any 8-bit limitations.
			const int n = 300;
			tmpVector.Clear();
			var lineList = new StringBuilder();
			var charList = new StringBuilder();
			for (int x = 1; x < n + 1; x++) {
				tmpVector.Add(x + "\n");
				lineList.Append(x + "\n");
				charList.Append(Convert.ToChar(x));
			}
			Assert.AreEqual(n, tmpVector.Count);
			string lines = lineList.ToString();
			string chars = charList.ToString();
			Assert.AreEqual(n, chars.Length);
			tmpVector.Insert(0, "");
			diffs = new List<Diff>{new Diff(Operation.Delete, chars)};
			dmp.diff_charsToLines(diffs, tmpVector);
			CollectionAssert.AreEqual(new List<Diff>
			                          {new Diff(Operation.Delete, lines)}, diffs);
		}

		[Test]
		public void diff_cleanupEfficiencyTest() {
			var dmp = new diff_match_patchTest{Diff_EditCost = 4};
			// Cleanup operationally trivial equalities.
			// Null case.
			var diffs = new List<Diff>();
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>(), diffs);

			// No elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "wxyz"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "34")
			};
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "wxyz"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "34")
			}, diffs);

			// Four-edit elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "xyz"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "34")
			};
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abxyzcd"),
				new Diff(Operation.Insert, "12xyz34")
			}, diffs);

			// Three-edit elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "x"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "34")
			};
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "xcd"),
				new Diff(Operation.Insert, "12x34")
			}, diffs);

			// Backpass elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "xy"),
				new Diff(Operation.Insert, "34"),
				new Diff(Operation.Equal, "z"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "56")
			};
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abxyzcd"),
				new Diff(Operation.Insert, "12xy34z56")
			}, diffs);

			// High cost elimination.
			dmp.Diff_EditCost = 5;
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "12"),
				new Diff(Operation.Equal, "wxyz"),
				new Diff(Operation.Delete, "cd"),
				new Diff(Operation.Insert, "34")
			};
			dmp.diff_cleanupEfficiency(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abwxyzcd"),
				new Diff(Operation.Insert, "12wxyz34")
			}, diffs);
			dmp.Diff_EditCost = 4;
		}

		[Test]
		public void diff_cleanupMergeTest() {
			var dmp = new diff_match_patchTest();
			// Cleanup a messy diff.
			// Null case.
			var diffs = new List<Diff>();
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>(), diffs);

			// No change case.
			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Insert, "c")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Insert, "c")}, diffs);

			// Merge equalities.
			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Equal, "b"), new Diff(Operation.Equal, "c")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "abc")}, diffs);

			// Merge deletions.
			diffs = new List<Diff>{new Diff(Operation.Delete, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Delete, "c")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Delete, "abc")}, diffs);

			// Merge insertions.
			diffs = new List<Diff>{new Diff(Operation.Insert, "a"), new Diff(Operation.Insert, "b"), new Diff(Operation.Insert, "c")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Insert, "abc")}, diffs);

			// Merge interweave.
			diffs = new List<Diff>{new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "b"), new Diff(Operation.Delete, "c"), new Diff(Operation.Insert, "d"), new Diff(Operation.Equal, "e"), new Diff(Operation.Equal, "f")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Delete, "ac"), new Diff(Operation.Insert, "bd"), new Diff(Operation.Equal, "ef")}, diffs);

			// Prefix and suffix detection.
			diffs = new List<Diff>{new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "abc"), new Diff(Operation.Delete, "dc")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "d"), new Diff(Operation.Insert, "b"), new Diff(Operation.Equal, "c")}, diffs);

			// Prefix and suffix detection with equalities.
			diffs = new List<Diff>{new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "abc"), new Diff(Operation.Delete, "dc"), new Diff(Operation.Equal, "y")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "xa"), new Diff(Operation.Delete, "d"), new Diff(Operation.Insert, "b"), new Diff(Operation.Equal, "cy")}, diffs);

			// Slide edit left.
			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "ba"), new Diff(Operation.Equal, "c")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Insert, "ab"), new Diff(Operation.Equal, "ac")}, diffs);

			// Slide edit right.
			diffs = new List<Diff>{new Diff(Operation.Equal, "c"), new Diff(Operation.Insert, "ab"), new Diff(Operation.Equal, "a")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "ca"), new Diff(Operation.Insert, "ba")}, diffs);

			// Slide edit left recursive.
			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Equal, "c"), new Diff(Operation.Delete, "ac"), new Diff(Operation.Equal, "x")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Delete, "abc"), new Diff(Operation.Equal, "acx")}, diffs);

			// Slide edit right recursive.
			diffs = new List<Diff>{new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "ca"), new Diff(Operation.Equal, "c"), new Diff(Operation.Delete, "b"), new Diff(Operation.Equal, "a")};
			dmp.diff_cleanupMerge(diffs);
			CollectionAssert.AreEqual(new List<Diff>{new Diff(Operation.Equal, "xca"), new Diff(Operation.Delete, "cba")}, diffs);
		}

		[Test]
		public void diff_cleanupSemanticLosslessTest() {
			var dmp = new diff_match_patchTest();
			// Slide diffs to match logical boundaries.
			// Null case.
			var diffs = new List<Diff>();
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>(), diffs);

			// Blank lines.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "AAA\r\n\r\nBBB"),
				new Diff(Operation.Insert, "\r\nDDD\r\n\r\nBBB"),
				new Diff(Operation.Equal, "\r\nEEE")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "AAA\r\n\r\n"),
				new Diff(Operation.Insert, "BBB\r\nDDD\r\n\r\n"),
				new Diff(Operation.Equal, "BBB\r\nEEE")
			}, diffs);

			// Line boundaries.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "AAA\r\nBBB"),
				new Diff(Operation.Insert, " DDD\r\nBBB"),
				new Diff(Operation.Equal, " EEE")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "AAA\r\n"),
				new Diff(Operation.Insert, "BBB DDD\r\n"),
				new Diff(Operation.Equal, "BBB EEE")
			}, diffs);

			// Word boundaries.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "The c"),
				new Diff(Operation.Insert, "ow and the c"),
				new Diff(Operation.Equal, "at.")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "The "),
				new Diff(Operation.Insert, "cow and the "),
				new Diff(Operation.Equal, "cat.")
			}, diffs);

			// Alphanumeric boundaries.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "The-c"),
				new Diff(Operation.Insert, "ow-and-the-c"),
				new Diff(Operation.Equal, "at.")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "The-"),
				new Diff(Operation.Insert, "cow-and-the-"),
				new Diff(Operation.Equal, "cat.")
			}, diffs);

			// Hitting the start.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "a"),
				new Diff(Operation.Delete, "a"),
				new Diff(Operation.Equal, "ax")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "a"),
				new Diff(Operation.Equal, "aax")
			}, diffs);

			// Hitting the end.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "xa"),
				new Diff(Operation.Delete, "a"),
				new Diff(Operation.Equal, "a")
			};
			dmp.diff_cleanupSemanticLossless(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "xaa"),
				new Diff(Operation.Delete, "a")
			}, diffs);
		}

		[Test]
		public void diff_cleanupSemanticTest() {
			var dmp = new diff_match_patchTest();
			// Cleanup semantically trivial equalities.
			// Null case.
			var diffs = new List<Diff>();
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>(), diffs);

			// No elimination #1.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "cd"),
				new Diff(Operation.Equal, "12"),
				new Diff(Operation.Delete, "e")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Insert, "cd"),
				new Diff(Operation.Equal, "12"),
				new Diff(Operation.Delete, "e")
			}, diffs);

			// No elimination #2.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Insert, "ABC"),
				new Diff(Operation.Equal, "1234"),
				new Diff(Operation.Delete, "wxyz")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Insert, "ABC"),
				new Diff(Operation.Equal, "1234"),
				new Diff(Operation.Delete, "wxyz")
			}, diffs);

			// Simple elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "a"),
				new Diff(Operation.Equal, "b"),
				new Diff(Operation.Delete, "c")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Insert, "b")
			}, diffs);

			// Backpass elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "ab"),
				new Diff(Operation.Equal, "cd"),
				new Diff(Operation.Delete, "e"),
				new Diff(Operation.Equal, "f"),
				new Diff(Operation.Insert, "g")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abcdef"),
				new Diff(Operation.Insert, "cdfg")
			}, diffs);

			// Multiple eliminations.
			diffs = new List<Diff>{
				new Diff(Operation.Insert, "1"),
				new Diff(Operation.Equal, "A"),
				new Diff(Operation.Delete, "B"),
				new Diff(Operation.Insert, "2"),
				new Diff(Operation.Equal, "_"),
				new Diff(Operation.Insert, "1"),
				new Diff(Operation.Equal, "A"),
				new Diff(Operation.Delete, "B"),
				new Diff(Operation.Insert, "2")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "AB_AB"),
				new Diff(Operation.Insert, "1A2_1A2")
			}, diffs);

			// Word boundaries.
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "The c"),
				new Diff(Operation.Delete, "ow and the c"),
				new Diff(Operation.Equal, "at.")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Equal, "The "),
				new Diff(Operation.Delete, "cow and the "),
				new Diff(Operation.Equal, "cat.")
			}, diffs);

			// No overlap elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "abcxx"),
				new Diff(Operation.Insert, "xxdef")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abcxx"),
				new Diff(Operation.Insert, "xxdef")
			}, diffs);

			// Overlap elimination.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "abcxxx"),
				new Diff(Operation.Insert, "xxxdef")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Equal, "xxx"),
				new Diff(Operation.Insert, "def")
			}, diffs);

			// Two overlap eliminations.
			diffs = new List<Diff>{
				new Diff(Operation.Delete, "abcd1212"),
				new Diff(Operation.Insert, "1212efghi"),
				new Diff(Operation.Equal, "----"),
				new Diff(Operation.Delete, "A3"),
				new Diff(Operation.Insert, "3BC")
			};
			dmp.diff_cleanupSemantic(diffs);
			CollectionAssert.AreEqual(new List<Diff>{
				new Diff(Operation.Delete, "abcd"),
				new Diff(Operation.Equal, "1212"),
				new Diff(Operation.Insert, "efghi"),
				new Diff(Operation.Equal, "----"),
				new Diff(Operation.Delete, "A"),
				new Diff(Operation.Equal, "3"),
				new Diff(Operation.Insert, "BC")
			}, diffs);
		}

		[Test]
		public void diff_commonOverlapTest() {
			var dmp = new diff_match_patchTest();
			// Detect any suffix/prefix overlap.
			// Null case.
			Assert.AreEqual(0, dmp.diff_commonOverlap("", "abcd"));

			// Whole case.
			Assert.AreEqual(3, dmp.diff_commonOverlap("abc", "abcd"));

			// No overlap.
			Assert.AreEqual(0, dmp.diff_commonOverlap("123456", "abcd"));

			// Overlap.
			Assert.AreEqual(3, dmp.diff_commonOverlap("123456xxx", "xxxabcd"));

			// Unicode.
			// Some overly clever languages (C#) may treat ligatures as equal to their
			// component letters.  E.g. U+FB01 == 'fi'
			Assert.AreEqual(0, dmp.diff_commonOverlap("fi", "\ufb01i"));
		}

		[Test]
		public void diff_commonPrefixTest() {
			var dmp = new diff_match_patchTest();
			// Detect any common suffix.
			// Null case.
			Assert.AreEqual(0, dmp.diff_commonPrefix("abc", "xyz"));

			// Non-null case.
			Assert.AreEqual(4, dmp.diff_commonPrefix("1234abcdef", "1234xyz"));

			// Whole case.
			Assert.AreEqual(4, dmp.diff_commonPrefix("1234", "1234xyz"));
		}

		[Test]
		public void diff_commonSuffixTest() {
			var dmp = new diff_match_patchTest();
			// Detect any common suffix.
			// Null case.
			Assert.AreEqual(0, dmp.diff_commonSuffix("abc", "xyz"));

			// Non-null case.
			Assert.AreEqual(4, dmp.diff_commonSuffix("abcdef1234", "xyz1234"));

			// Whole case.
			Assert.AreEqual(4, dmp.diff_commonSuffix("1234", "xyz1234"));
		}

		[Test]
		public void diff_deltaTest() {
			var dmp = new diff_match_patchTest();
			// Convert a diff into delta string.
			var diffs = new List<Diff>{
				new Diff(Operation.Equal, "jump"),
				new Diff(Operation.Delete, "s"),
				new Diff(Operation.Insert, "ed"),
				new Diff(Operation.Equal, " over "),
				new Diff(Operation.Delete, "the"),
				new Diff(Operation.Insert, "a"),
				new Diff(Operation.Equal, " lazy"),
				new Diff(Operation.Insert, "old dog")
			};
			string text1 = dmp.diff_text1(diffs);
			Assert.AreEqual("jumps over the lazy", text1);

			string delta = dmp.diff_toDelta(diffs);
			Assert.AreEqual("=4\t-1\t+ed\t=6\t-3\t+a\t=5\t+old dog", delta);

			// Convert delta string into a diff.
			CollectionAssert.AreEqual(diffs, dmp.diff_fromDelta(text1, delta));

			// Generates error (19 < 20).
			try {
				dmp.diff_fromDelta(text1 + "x", delta);
				Assert.Fail("diff_fromDelta: Too long.");
			} catch (ArgumentException) {
				// Exception expected.
			}

			// Generates error (19 > 18).
			try {
				dmp.diff_fromDelta(text1.Substring(1), delta);
				Assert.Fail("diff_fromDelta: Too short.");
			} catch (ArgumentException) {
				// Exception expected.
			}

			// Generates error (%c3%xy invalid Unicode).
			try {
				dmp.diff_fromDelta("", "+%c3%xy");
				Assert.Fail("diff_fromDelta: Invalid character.");
			} catch (ArgumentException) {
				// Exception expected.
			}

			// Test deltas with special characters.
			const char zero = (char)0;
			const char one = (char)1;
			const char two = (char)2;
			diffs = new List<Diff>{
				new Diff(Operation.Equal, "\u0680 " + zero + " \t %"),
				new Diff(Operation.Delete, "\u0681 " + one + " \n ^"),
				new Diff(Operation.Insert, "\u0682 " + two + " \\ |")
			};
			text1 = dmp.diff_text1(diffs);
			Assert.AreEqual("\u0680 " + zero + " \t %\u0681 " + one + " \n ^", text1);

			delta = dmp.diff_toDelta(diffs);
			// Lowercase, due to UrlEncode uses lower.
			Assert.AreEqual("=7\t-7\t+%da%82 %02 %5c %7c", delta, "diff_toDelta: Unicode.");

			CollectionAssert.AreEqual(diffs, dmp.diff_fromDelta(text1, delta), "diff_fromDelta: Unicode.");

			// Verify pool of unchanged characters.
			diffs = new List<Diff>{
				new Diff(Operation.Insert, "A-Z a-z 0-9 - _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # ")
			};
			string text2 = dmp.diff_text2(diffs);
			Assert.AreEqual("A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ", text2, "diff_text2: Unchanged characters.");

			delta = dmp.diff_toDelta(diffs);
			Assert.AreEqual("+A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ", delta, "diff_toDelta: Unchanged characters.");

			// Convert delta string into a diff.
			CollectionAssert.AreEqual(diffs, dmp.diff_fromDelta("", delta), "diff_fromDelta: Unchanged characters.");
		}

		[Test]
		public void diff_halfmatchTest() {
			var dmp = new diff_match_patchTest{Diff_Timeout = 1};
			// No match.
			Assert.IsNull(dmp.diff_halfMatch("1234567890", "abcdef"));

			Assert.IsNull(dmp.diff_halfMatch("12345", "23"));

			// Single Match.
			CollectionAssert.AreEqual(new[]{"12", "90", "a", "z", "345678"}, dmp.diff_halfMatch("1234567890", "a345678z"));

			CollectionAssert.AreEqual(new[]{"a", "z", "12", "90", "345678"}, dmp.diff_halfMatch("a345678z", "1234567890"));

			CollectionAssert.AreEqual(new[]{"abc", "z", "1234", "0", "56789"}, dmp.diff_halfMatch("abc56789z", "1234567890"));

			CollectionAssert.AreEqual(new[]{"a", "xyz", "1", "7890", "23456"}, dmp.diff_halfMatch("a23456xyz", "1234567890"));

			// Multiple Matches.
			CollectionAssert.AreEqual(new[]{"12123", "123121", "a", "z", "1234123451234"}, dmp.diff_halfMatch("121231234123451234123121", "a1234123451234z"));

			CollectionAssert.AreEqual(new[]{"", "-=-=-=-=-=", "x", "", "x-=-=-=-=-=-=-="}, dmp.diff_halfMatch("x-=-=-=-=-=-=-=-=-=-=-=-=", "xx-=-=-=-=-=-=-="));

			CollectionAssert.AreEqual(new[]{"-=-=-=-=-=", "", "", "y", "-=-=-=-=-=-=-=y"}, dmp.diff_halfMatch("-=-=-=-=-=-=-=-=-=-=-=-=y", "-=-=-=-=-=-=-=yy"));

			// Non-optimal halfmatch.
			// Optimal diff would be -q+x=H-i+e=lloHe+Hu=llo-Hew+y not -qHillo+x=HelloHe-w+Hulloy
			CollectionAssert.AreEqual(new[]{"qHillo", "w", "x", "Hulloy", "HelloHe"}, dmp.diff_halfMatch("qHilloHelloHew", "xHelloHeHulloy"));

			// Optimal no halfmatch.
			dmp.Diff_Timeout = 0;
			Assert.IsNull(dmp.diff_halfMatch("qHilloHelloHew", "xHelloHeHulloy"));
		}

		[Test]
		public void diff_levenshteinTest() {
			var dmp = new diff_match_patchTest();
			var diffs = new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Insert, "1234"),
				new Diff(Operation.Equal, "xyz")
			};
			Assert.AreEqual(4, dmp.diff_levenshtein(diffs), "diff_levenshtein: Levenshtein with trailing equality.");

			diffs = new List<Diff>{
				new Diff(Operation.Equal, "xyz"),
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Insert, "1234")
			};
			Assert.AreEqual(4, dmp.diff_levenshtein(diffs), "diff_levenshtein: Levenshtein with leading equality.");

			diffs = new List<Diff>{
				new Diff(Operation.Delete, "abc"),
				new Diff(Operation.Equal, "xyz"),
				new Diff(Operation.Insert, "1234")
			};
			Assert.AreEqual(7, dmp.diff_levenshtein(diffs), "diff_levenshtein: Levenshtein with middle equality.");
		}

		[Test]
		public void diff_linesToCharsTest() {
			var dmp = new diff_match_patchTest();
			// Convert lines down to characters.
			var tmpVector = new List<string>{"", "alpha\n", "beta\n"};
			Object[] result = dmp.diff_linesToChars("alpha\nbeta\nalpha\n", "beta\nalpha\nbeta\n");
			Assert.AreEqual("\u0001\u0002\u0001", result[0]);
			Assert.AreEqual("\u0002\u0001\u0002", result[1]);
			CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

			tmpVector.Clear();
			tmpVector.Add("");
			tmpVector.Add("alpha\r\n");
			tmpVector.Add("beta\r\n");
			tmpVector.Add("\r\n");
			result = dmp.diff_linesToChars("", "alpha\r\nbeta\r\n\r\n\r\n");
			Assert.AreEqual("", result[0]);
			Assert.AreEqual("\u0001\u0002\u0003\u0003", result[1]);
			CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

			tmpVector.Clear();
			tmpVector.Add("");
			tmpVector.Add("a");
			tmpVector.Add("b");
			result = dmp.diff_linesToChars("a", "b");
			Assert.AreEqual("\u0001", result[0]);
			Assert.AreEqual("\u0002", result[1]);
			CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

			// More than 256 to reveal any 8-bit limitations.
			const int n = 300;
			tmpVector.Clear();
			var lineList = new StringBuilder();
			var charList = new StringBuilder();
			for (int x = 1; x < n + 1; x++) {
				tmpVector.Add(x + "\n");
				lineList.Append(x + "\n");
				charList.Append(Convert.ToChar(x));
			}
			Assert.AreEqual(n, tmpVector.Count);
			string lines = lineList.ToString();
			string chars = charList.ToString();
			Assert.AreEqual(n, chars.Length);
			tmpVector.Insert(0, "");
			result = dmp.diff_linesToChars(lines, "");
			Assert.AreEqual(chars, result[0]);
			Assert.AreEqual("", result[1]);
			CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);
		}

		[Test]
		public void diff_mainTest() {
			var dmp = new diff_match_patchTest();
			// Perform a trivial diff.
			var diffs = new List<Diff>();
			CollectionAssert.AreEqual(diffs, dmp.diff_main("", "", false), "diff_main: Null case.");

			diffs = new List<Diff>{new Diff(Operation.Equal, "abc")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("abc", "abc", false), "diff_main: Equality.");

			diffs = new List<Diff>{new Diff(Operation.Equal, "ab"), new Diff(Operation.Insert, "123"), new Diff(Operation.Equal, "c")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("abc", "ab123c", false), "diff_main: Simple insertion.");

			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "123"), new Diff(Operation.Equal, "bc")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("a123bc", "abc", false), "diff_main: Simple deletion.");

			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "123"), new Diff(Operation.Equal, "b"), new Diff(Operation.Insert, "456"), new Diff(Operation.Equal, "c")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("abc", "a123b456c", false), "diff_main: Two insertions.");

			diffs = new List<Diff>{new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "123"), new Diff(Operation.Equal, "b"), new Diff(Operation.Delete, "456"), new Diff(Operation.Equal, "c")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("a123b456c", "abc", false), "diff_main: Two deletions.");

			// Perform a real diff.
			// Switch off the timeout.
			dmp.Diff_Timeout = 0;
			diffs = new List<Diff>{new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "b")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("a", "b", false), "diff_main: Simple case #1.");

			diffs = new List<Diff>{new Diff(Operation.Delete, "Apple"), new Diff(Operation.Insert, "Banana"), new Diff(Operation.Equal, "s are a"), new Diff(Operation.Insert, "lso"), new Diff(Operation.Equal, " fruit.")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("Apples are a fruit.", "Bananas are also fruit.", false), "diff_main: Simple case #2.");

			diffs = new List<Diff>{new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "\u0680"), new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "\t"), new Diff(Operation.Insert, new string(new[]{(char)0}))};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("ax\t", "\u0680x" + (char)0, false), "diff_main: Simple case #3.");

			diffs = new List<Diff>{new Diff(Operation.Delete, "1"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "y"), new Diff(Operation.Equal, "b"), new Diff(Operation.Delete, "2"), new Diff(Operation.Insert, "xab")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("1ayb2", "abxab", false), "diff_main: Overlap #1.");

			diffs = new List<Diff>{new Diff(Operation.Insert, "xaxcx"), new Diff(Operation.Equal, "abc"), new Diff(Operation.Delete, "y")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("abcy", "xaxcxabc", false), "diff_main: Overlap #2.");

			diffs = new List<Diff>{new Diff(Operation.Delete, "ABCD"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "="), new Diff(Operation.Insert, "-"), new Diff(Operation.Equal, "bcd"), new Diff(Operation.Delete, "="), new Diff(Operation.Insert, "-"), new Diff(Operation.Equal, "efghijklmnopqrs"), new Diff(Operation.Delete, "EFGHIJKLMNOefg")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("ABCDa=bcd=efghijklmnopqrsEFGHIJKLMNOefg", "a-bcd-efghijklmnopqrs", false), "diff_main: Overlap #3.");

			diffs = new List<Diff>{new Diff(Operation.Insert, " "), new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "nd"), new Diff(Operation.Equal, " [[Pennsylvania]]"), new Diff(Operation.Delete, " and [[New")};
			CollectionAssert.AreEqual(diffs, dmp.diff_main("a [[Pennsylvania]] and [[New", " and [[Pennsylvania]]", false), "diff_main: Large equality.");

			dmp.Diff_Timeout = 0.1f; // 100ms
			string a = "`Twas brillig, and the slithy toves\nDid gyre and gimble in the wabe:\nAll mimsy were the borogoves,\nAnd the mome raths outgrabe.\n";
			string b = "I am the very model of a modern major general,\nI've information vegetable, animal, and mineral,\nI know the kings of England, and I quote the fights historical,\nFrom Marathon to Waterloo, in order categorical.\n";
			// Increase the text lengths by 1024 times to ensure a timeout.
			for (int x = 0; x < 10; x++) {
				a = a + a;
				b = b + b;
			}
			DateTime startTime = DateTime.Now;
			dmp.diff_main(a, b);
			DateTime endTime = DateTime.Now;
			// Test that we took at least the timeout period.
			Assert.IsTrue(new TimeSpan(((long)(dmp.Diff_Timeout * 1000)) * 10000) <= endTime - startTime);
			// Test that we didn't take forever (be forgiving).
			// Theoretically this test could fail very occasionally if the
			// OS task swaps or locks up for a second at the wrong moment.
			Assert.IsTrue(new TimeSpan(((long)(dmp.Diff_Timeout * 1000)) * 10000 * 2) > endTime - startTime);
			dmp.Diff_Timeout = 0;

			// Test the linemode speedup.
			// Must be long to pass the 100 char cutoff.
			a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
			b = "abcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\n";
			CollectionAssert.AreEqual(dmp.diff_main(a, b, true), dmp.diff_main(a, b, false), "diff_main: Simple line-mode.");

			a = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
			b = "abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghij";
			CollectionAssert.AreEqual(dmp.diff_main(a, b, true), dmp.diff_main(a, b, false), "diff_main: Single line-mode.");

			a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
			b = "abcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n";
			IEnumerable<string> texts_linemode = diff_rebuildtexts(dmp.diff_main(a, b, true));
			IEnumerable<string> texts_textmode = diff_rebuildtexts(dmp.diff_main(a, b, false));
			CollectionAssert.AreEqual(texts_textmode, texts_linemode, "diff_main: Overlap line-mode.");

			// Test null inputs -- not needed because nulls can't be passed in C#.
		}

		[Test]
		public void diff_prettyHtmlTest() {
			var dmp = new diff_match_patchTest();
			// Pretty print.
			var diffs = new List<Diff>{
				new Diff(Operation.Equal, "a\n"),
				new Diff(Operation.Delete, "<B>b</B>"),
				new Diff(Operation.Insert, "c&d")
			};
			Assert.AreEqual("<span>a&para;<br></span><del style=\"background:#ffe6e6;\">&lt;B&gt;b&lt;/B&gt;</del><ins style=\"background:#e6ffe6;\">c&amp;d</ins>",
			                dmp.diff_prettyHtml(diffs));
		}

		[Test]
		public void diff_textTest() {
			var dmp = new diff_match_patchTest();
			// Compute the source and destination texts.
			var diffs = new List<Diff>{
				new Diff(Operation.Equal, "jump"),
				new Diff(Operation.Delete, "s"),
				new Diff(Operation.Insert, "ed"),
				new Diff(Operation.Equal, " over "),
				new Diff(Operation.Delete, "the"),
				new Diff(Operation.Insert, "a"),
				new Diff(Operation.Equal, " lazy")
			};
			Assert.AreEqual("jumps over the lazy", dmp.diff_text1(diffs));

			Assert.AreEqual("jumped over a lazy", dmp.diff_text2(diffs));
		}

		[Test]
		public void diff_xIndexTest() {
			var dmp = new diff_match_patchTest();
			// Translate a location in text1 to text2.
			var diffs = new List<Diff>{
				new Diff(Operation.Delete, "a"),
				new Diff(Operation.Insert, "1234"),
				new Diff(Operation.Equal, "xyz")
			};
			Assert.AreEqual(5, dmp.diff_xIndex(diffs, 2), "diff_xIndex: Translation on equality.");

			diffs = new List<Diff>{
				new Diff(Operation.Equal, "a"),
				new Diff(Operation.Delete, "1234"),
				new Diff(Operation.Equal, "xyz")
			};
			Assert.AreEqual(1, dmp.diff_xIndex(diffs, 3), "diff_xIndex: Translation on deletion.");
		}

		[Test]
		public void match_alphabetTest() {
			var dmp = new diff_match_patchTest();
			// Initialise the bitmasks for Bitap.
			var bitmask = new Dictionary<char, int>{{'a', 4}, {'b', 2}, {'c', 1}};
			CollectionAssert.AreEqual(bitmask, dmp.match_alphabet("abc"), "match_alphabet: Unique.");

			bitmask.Clear();
			bitmask.Add('a', 37);
			bitmask.Add('b', 18);
			bitmask.Add('c', 8);
			CollectionAssert.AreEqual(bitmask, dmp.match_alphabet("abcaba"), "match_alphabet: Duplicates.");
		}

		[Test]
		public void match_bitapTest() {
			var dmp = new diff_match_patchTest{Match_Distance = 100, Match_Threshold = 0.5f};

			// Bitap algorithm.
			Assert.AreEqual(5, dmp.match_bitap("abcdefghijk", "fgh", 5), "match_bitap: Exact match #1.");

			Assert.AreEqual(5, dmp.match_bitap("abcdefghijk", "fgh", 0), "match_bitap: Exact match #2.");

			Assert.AreEqual(4, dmp.match_bitap("abcdefghijk", "efxhi", 0), "match_bitap: Fuzzy match #1.");

			Assert.AreEqual(2, dmp.match_bitap("abcdefghijk", "cdefxyhijk", 5), "match_bitap: Fuzzy match #2.");

			Assert.AreEqual(-1, dmp.match_bitap("abcdefghijk", "bxy", 1), "match_bitap: Fuzzy match #3.");

			Assert.AreEqual(2, dmp.match_bitap("123456789xx0", "3456789x0", 2), "match_bitap: Overflow.");

			Assert.AreEqual(0, dmp.match_bitap("abcdef", "xxabc", 4), "match_bitap: Before start match.");

			Assert.AreEqual(3, dmp.match_bitap("abcdef", "defyy", 4), "match_bitap: Beyond end match.");

			Assert.AreEqual(0, dmp.match_bitap("abcdef", "xabcdefy", 0), "match_bitap: Oversized pattern.");

			dmp.Match_Threshold = 0.4f;
			Assert.AreEqual(4, dmp.match_bitap("abcdefghijk", "efxyhi", 1), "match_bitap: Threshold #1.");

			dmp.Match_Threshold = 0.3f;
			Assert.AreEqual(-1, dmp.match_bitap("abcdefghijk", "efxyhi", 1), "match_bitap: Threshold #2.");

			dmp.Match_Threshold = 0.0f;
			Assert.AreEqual(1, dmp.match_bitap("abcdefghijk", "bcdef", 1), "match_bitap: Threshold #3.");

			dmp.Match_Threshold = 0.5f;
			Assert.AreEqual(0, dmp.match_bitap("abcdexyzabcde", "abccde", 3), "match_bitap: Multiple select #1.");

			Assert.AreEqual(8, dmp.match_bitap("abcdexyzabcde", "abccde", 5), "match_bitap: Multiple select #2.");

			dmp.Match_Distance = 10; // Strict location.
			Assert.AreEqual(-1, dmp.match_bitap("abcdefghijklmnopqrstuvwxyz", "abcdefg", 24), "match_bitap: Distance test #1.");

			Assert.AreEqual(0, dmp.match_bitap("abcdefghijklmnopqrstuvwxyz", "abcdxxefg", 1), "match_bitap: Distance test #2.");

			dmp.Match_Distance = 1000; // Loose location.
			Assert.AreEqual(0, dmp.match_bitap("abcdefghijklmnopqrstuvwxyz", "abcdefg", 24), "match_bitap: Distance test #3.");
		}

		[Test]
		public void match_mainTest() {
			var dmp = new diff_match_patchTest();
			// Full match.
			Assert.AreEqual(0, dmp.match_main("abcdef", "abcdef", 1000), "match_main: Equality.");

			Assert.AreEqual(-1, dmp.match_main("", "abcdef", 1), "match_main: Null text.");

			Assert.AreEqual(3, dmp.match_main("abcdef", "", 3), "match_main: Null pattern.");

			Assert.AreEqual(3, dmp.match_main("abcdef", "de", 3), "match_main: Exact match.");

			Assert.AreEqual(3, dmp.match_main("abcdef", "defy", 4), "match_main: Beyond end match.");

			Assert.AreEqual(0, dmp.match_main("abcdef", "abcdefy", 0), "match_main: Oversized pattern.");

			dmp.Match_Threshold = 0.7f;
			Assert.AreEqual(4, dmp.match_main("I am the very model of a modern major general.", " that berry ", 5), "match_main: Complex match.");
			dmp.Match_Threshold = 0.5f;

			// Test null inputs -- not needed because nulls can't be passed in C#.
		}

		[Test]
		public void patch_addContextTest() {
			var dmp = new diff_match_patchTest{Patch_Margin = 4};
			Patch p = dmp.patch_fromText("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
			dmp.patch_addContext(p, "The quick brown fox jumps over the lazy dog.");
			Assert.AreEqual("@@ -17,12 +17,18 @@\n fox \n-jump\n+somersault\n s ov\n", p.ToString(), "patch_addContext: Simple case.");

			p = dmp.patch_fromText("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
			dmp.patch_addContext(p, "The quick brown fox jumps.");
			Assert.AreEqual("@@ -17,10 +17,16 @@\n fox \n-jump\n+somersault\n s.\n", p.ToString(), "patch_addContext: Not enough trailing context.");

			p = dmp.patch_fromText("@@ -3 +3,2 @@\n-e\n+at\n")[0];
			dmp.patch_addContext(p, "The quick brown fox jumps.");
			Assert.AreEqual("@@ -1,7 +1,8 @@\n Th\n-e\n+at\n  qui\n", p.ToString(), "patch_addContext: Not enough leading context.");

			p = dmp.patch_fromText("@@ -3 +3,2 @@\n-e\n+at\n")[0];
			dmp.patch_addContext(p, "The quick brown fox jumps.  The quick brown fox crashes.");
			Assert.AreEqual("@@ -1,27 +1,28 @@\n Th\n-e\n+at\n  quick brown fox jumps. \n", p.ToString(), "patch_addContext: Ambiguity.");
		}

		[Test]
		public void patch_addPaddingTest() {
			var dmp = new diff_match_patchTest();
			List<Patch> patches = dmp.patch_make("", "test");
			Assert.AreEqual("@@ -0,0 +1,4 @@\n+test\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges full.");
			dmp.patch_addPadding(patches);
			Assert.AreEqual("@@ -1,8 +1,12 @@\n %01%02%03%04\n+test\n %01%02%03%04\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges full.");

			patches = dmp.patch_make("XY", "XtestY");
			Assert.AreEqual("@@ -1,2 +1,6 @@\n X\n+test\n Y\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges partial.");
			dmp.patch_addPadding(patches);
			Assert.AreEqual("@@ -2,8 +2,12 @@\n %02%03%04X\n+test\n Y%01%02%03\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges partial.");

			patches = dmp.patch_make("XXXXYYYY", "XXXXtestYYYY");
			Assert.AreEqual("@@ -1,8 +1,12 @@\n XXXX\n+test\n YYYY\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges none.");
			dmp.patch_addPadding(patches);
			Assert.AreEqual("@@ -5,8 +5,12 @@\n XXXX\n+test\n YYYY\n",
			                dmp.patch_toText(patches),
			                "patch_addPadding: Both edges none.");
		}

		[Test]
		public void patch_applyTest() {
			var dmp = new diff_match_patchTest{Match_Distance = 1000, Match_Threshold = 0.5f, Patch_DeleteThreshold = 0.5f};
			List<Patch> patches = dmp.patch_make("", "");
			Object[] results = dmp.patch_apply(patches, "Hello world.");
			var boolArray = (bool[])results[1];
			string resultStr = results[0] + "\t" + boolArray.Length;
			Assert.AreEqual("Hello world.\t0", resultStr, "patch_apply: Null case.");

			patches = dmp.patch_make("The quick brown fox jumps over the lazy dog.", "That quick brown fox jumped over a lazy dog.");
			results = dmp.patch_apply(patches, "The quick brown fox jumps over the lazy dog.");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("That quick brown fox jumped over a lazy dog.\tTrue\tTrue", resultStr, "patch_apply: Exact match.");

			results = dmp.patch_apply(patches, "The quick red rabbit jumps over the tired tiger.");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("That quick red rabbit jumped over a tired tiger.\tTrue\tTrue", resultStr, "patch_apply: Partial match.");

			results = dmp.patch_apply(patches, "I am the very model of a modern major general.");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("I am the very model of a modern major general.\tFalse\tFalse", resultStr, "patch_apply: Failed match.");

			patches = dmp.patch_make("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
			results = dmp.patch_apply(patches, "x123456789012345678901234567890-----++++++++++-----123456789012345678901234567890y");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("xabcy\tTrue\tTrue", resultStr, "patch_apply: Big delete, small change.");

			patches = dmp.patch_make("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
			results = dmp.patch_apply(patches, "x12345678901234567890---------------++++++++++---------------12345678901234567890y");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("xabc12345678901234567890---------------++++++++++---------------12345678901234567890y\tFalse\tTrue", resultStr, "patch_apply: Big delete, big change 1.");

			dmp.Patch_DeleteThreshold = 0.6f;
			patches = dmp.patch_make("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
			results = dmp.patch_apply(patches, "x12345678901234567890---------------++++++++++---------------12345678901234567890y");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("xabcy\tTrue\tTrue", resultStr, "patch_apply: Big delete, big change 2.");
			dmp.Patch_DeleteThreshold = 0.5f;

			dmp.Match_Threshold = 0.0f;
			dmp.Match_Distance = 0;
			patches = dmp.patch_make("abcdefghijklmnopqrstuvwxyz--------------------1234567890", "abcXXXXXXXXXXdefghijklmnopqrstuvwxyz--------------------1234567YYYYYYYYYY890");
			results = dmp.patch_apply(patches, "ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567890");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
			Assert.AreEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567YYYYYYYYYY890\tFalse\tTrue", resultStr, "patch_apply: Compensate for failed patch.");
			dmp.Match_Threshold = 0.5f;
			dmp.Match_Distance = 1000;

			patches = dmp.patch_make("", "test");
			string patchStr = dmp.patch_toText(patches);
			dmp.patch_apply(patches, "");
			Assert.AreEqual(patchStr, dmp.patch_toText(patches), "patch_apply: No side effects.");

			patches = dmp.patch_make("The quick brown fox jumps over the lazy dog.", "Woof");
			patchStr = dmp.patch_toText(patches);
			dmp.patch_apply(patches, "The quick brown fox jumps over the lazy dog.");
			Assert.AreEqual(patchStr, dmp.patch_toText(patches), "patch_apply: No side effects with major delete.");

			patches = dmp.patch_make("", "test");
			results = dmp.patch_apply(patches, "");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0];
			Assert.AreEqual("test\tTrue", resultStr, "patch_apply: Edge exact match.");

			patches = dmp.patch_make("XY", "XtestY");
			results = dmp.patch_apply(patches, "XY");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0];
			Assert.AreEqual("XtestY\tTrue", resultStr, "patch_apply: Near edge exact match.");

			patches = dmp.patch_make("y", "y123");
			results = dmp.patch_apply(patches, "x");
			boolArray = (bool[])results[1];
			resultStr = results[0] + "\t" + boolArray[0];
			Assert.AreEqual("x123\tTrue", resultStr, "patch_apply: Edge partial match.");
		}

		[Test]
		public void patch_fromTextTest() {
			var dmp = new diff_match_patchTest();
			Assert.IsTrue(dmp.patch_fromText("").Count == 0, "patch_fromText: #0.");

			const string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0alaz\n";
			Assert.AreEqual(strp, dmp.patch_fromText(strp)[0].ToString(), "patch_fromText: #1.");

			Assert.AreEqual("@@ -1 +1 @@\n-a\n+b\n", dmp.patch_fromText("@@ -1 +1 @@\n-a\n+b\n")[0].ToString(), "patch_fromText: #2.");

			Assert.AreEqual("@@ -1,3 +0,0 @@\n-abc\n", dmp.patch_fromText("@@ -1,3 +0,0 @@\n-abc\n")[0].ToString(), "patch_fromText: #3.");

			Assert.AreEqual("@@ -0,0 +1,3 @@\n+abc\n", dmp.patch_fromText("@@ -0,0 +1,3 @@\n+abc\n")[0].ToString(), "patch_fromText: #4.");

			// Generates error.
			try {
				dmp.patch_fromText("Bad\nPatch\n");
				Assert.Fail("patch_fromText: #5.");
			} catch (ArgumentException) {
				// Exception expected.
			}
		}

		[Test]
		public void patch_makeTest() {
			var dmp = new diff_match_patchTest();
			List<Patch> patches = dmp.patch_make("", "");
			Assert.AreEqual("", dmp.patch_toText(patches), "patch_make: Null case.");

			string text1 = "The quick brown fox jumps over the lazy dog.";
			string text2 = "That quick brown fox jumped over a lazy dog.";
			string expectedPatch = "@@ -1,8 +1,7 @@\n Th\n-at\n+e\n  qui\n@@ -21,17 +21,18 @@\n jump\n-ed\n+s\n  over \n-a\n+the\n  laz\n";
			// The second patch must be "-21,17 +21,18", not "-22,17 +21,18" due to rolling context.
			patches = dmp.patch_make(text2, text1);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Text2+Text1 inputs.");

			expectedPatch = "@@ -1,11 +1,12 @@\n Th\n-e\n+at\n  quick b\n@@ -22,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n";
			patches = dmp.patch_make(text1, text2);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Text1+Text2 inputs.");

			List<Diff> diffs = dmp.diff_main(text1, text2, false);
			patches = dmp.patch_make(diffs);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Diff input.");

			patches = dmp.patch_make(text1, diffs);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Text1+Diff inputs.");

			patches = dmp.patch_make(text1, text2, diffs);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Text1+Text2+Diff inputs (deprecated).");

			patches = dmp.patch_make("`1234567890-=[]\\;',./", "~!@#$%^&*()_+{}|:\"<>?");
			Assert.AreEqual("@@ -1,21 +1,21 @@\n-%601234567890-=%5b%5d%5c;',./\n+~!@#$%25%5e&*()_+%7b%7d%7c:%22%3c%3e?\n",
			                dmp.patch_toText(patches),
			                "patch_toText: Character encoding.");

			diffs = new List<Diff>{
				new Diff(Operation.Delete, "`1234567890-=[]\\;',./"),
				new Diff(Operation.Insert, "~!@#$%^&*()_+{}|:\"<>?")
			};
			CollectionAssert.AreEqual(diffs,
			                          dmp.patch_fromText("@@ -1,21 +1,21 @@\n-%601234567890-=%5B%5D%5C;',./\n+~!@#$%25%5E&*()_+%7B%7D%7C:%22%3C%3E?\n")[0].diffs,
			                          "patch_fromText: Character decoding.");

			text1 = "";
			for (int x = 0; x < 100; x++) {
				text1 += "abcdef";
			}
			text2 = text1 + "123";
			expectedPatch = "@@ -573,28 +573,31 @@\n cdefabcdefabcdefabcdefabcdef\n+123\n";
			patches = dmp.patch_make(text1, text2);
			Assert.AreEqual(expectedPatch, dmp.patch_toText(patches), "patch_make: Long string with repeats.");

			// Test null inputs -- not needed because nulls can't be passed in C#.
		}

		[Test]
		public void patch_patchObjTest() {
			// Patch Object.
			var p = new Patch{
				start1 = 20, start2 = 21, length1 = 18, length2 = 17, diffs = new List<Diff>{
					new Diff(Operation.Equal, "jump"),
					new Diff(Operation.Delete, "s"),
					new Diff(Operation.Insert, "ed"),
					new Diff(Operation.Equal, " over "),
					new Diff(Operation.Delete, "the"),
					new Diff(Operation.Insert, "a"),
					new Diff(Operation.Equal, "\nlaz")
				}
			};
			const string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0alaz\n";
			Assert.AreEqual(strp, p.ToString(), "Patch: toString.");
		}

		[Test]
		public void patch_splitMaxTest() {
			// Assumes that Match_MaxBits is 32.
			var dmp = new diff_match_patchTest();

			List<Patch> patches = dmp.patch_make("abcdefghijklmnopqrstuvwxyz01234567890", "XabXcdXefXghXijXklXmnXopXqrXstXuvXwxXyzX01X23X45X67X89X0");
			dmp.patch_splitMax(patches);
			Assert.AreEqual("@@ -1,32 +1,46 @@\n+X\n ab\n+X\n cd\n+X\n ef\n+X\n gh\n+X\n ij\n+X\n kl\n+X\n mn\n+X\n op\n+X\n qr\n+X\n st\n+X\n uv\n+X\n wx\n+X\n yz\n+X\n 012345\n@@ -25,13 +39,18 @@\n zX01\n+X\n 23\n+X\n 45\n+X\n 67\n+X\n 89\n+X\n 0\n", dmp.patch_toText(patches));

			patches = dmp.patch_make("abcdef1234567890123456789012345678901234567890123456789012345678901234567890uvwxyz", "abcdefuvwxyz");
			string oldToText = dmp.patch_toText(patches);
			dmp.patch_splitMax(patches);
			Assert.AreEqual(oldToText, dmp.patch_toText(patches));

			patches = dmp.patch_make("1234567890123456789012345678901234567890123456789012345678901234567890", "abc");
			dmp.patch_splitMax(patches);
			Assert.AreEqual("@@ -1,32 +1,4 @@\n-1234567890123456789012345678\n 9012\n@@ -29,32 +1,4 @@\n-9012345678901234567890123456\n 7890\n@@ -57,14 +1,3 @@\n-78901234567890\n+abc\n", dmp.patch_toText(patches));

			patches = dmp.patch_make("abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1", "abcdefghij , h : 1 , t : 1 abcdefghij , h : 1 , t : 1 abcdefghij , h : 0 , t : 1");
			dmp.patch_splitMax(patches);
			Assert.AreEqual("@@ -2,32 +2,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n@@ -29,32 +29,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n", dmp.patch_toText(patches));
		}

		[Test]
		public void patch_toTextTest() {
			var dmp = new diff_match_patchTest();
			string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n";
			List<Patch> patches = dmp.patch_fromText(strp);
			string result = dmp.patch_toText(patches);
			Assert.AreEqual(strp, result);

			strp = "@@ -1,9 +1,9 @@\n-f\n+F\n oo+fooba\n@@ -7,9 +7,9 @@\n obar\n-,\n+.\n  tes\n";
			patches = dmp.patch_fromText(strp);
			result = dmp.patch_toText(patches);
			Assert.AreEqual(strp, result);
		}
	}
}
