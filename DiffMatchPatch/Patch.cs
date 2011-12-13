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
 * Diff Match and Patch
 * http://code.google.com/p/google-diff-match-patch/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DiffMatchPatch {
	/// <summary>
	/// Diff, patch and match controller
	/// </summary>
	public partial class diff_match_patch : DMPSettings {
		//  PATCH FUNCTIONS

		/**
		 * Increase the context until it is unique,
		 * but don't let the pattern expand beyond Match_MaxBits.
		 * @param patch The patch to grow.
		 * @param text Source text.
		 */
		protected void patch_addContext (Patch patch, string text) {
			if (text.Length == 0) {
				return;
			}
			string pattern = text.Substring(patch.start2, patch.length1);
			int padding = 0;

			// Look for the first and last matches of pattern in text.  If two
			// different matches are found, increase the pattern length.
			while (text.IndexOf(pattern, StringComparison.Ordinal)
				!= text.LastIndexOf(pattern, StringComparison.Ordinal)
				&& pattern.Length < Match_MaxBits - Patch_Margin - Patch_Margin) {
				padding += Patch_Margin;
				pattern = text.JavaSubstring(Math.Max(0, patch.start2 - padding),
					Math.Min(text.Length, patch.start2 + patch.length1 + padding));
			}
			// Add one chunk for good luck.
			padding += Patch_Margin;

			// Add the prefix.
			string prefix = text.JavaSubstring(Math.Max(0, patch.start2 - padding),
			  patch.start2);
			if (prefix.Length != 0) {
				patch.diffs.Insert(0, new Diff(Operation.Equal, prefix));
			}
			// Add the suffix.
			string suffix = text.JavaSubstring(patch.start2 + patch.length1,
				Math.Min(text.Length, patch.start2 + patch.length1 + padding));
			if (suffix.Length != 0) {
				patch.diffs.Add(new Diff(Operation.Equal, suffix));
			}

			// Roll back the start points.
			patch.start1 -= prefix.Length;
			patch.start2 -= prefix.Length;
			// Extend the lengths.
			patch.length1 += prefix.Length + suffix.Length;
			patch.length2 += prefix.Length + suffix.Length;
		}

		/**
		 * Compute a list of patches to turn text1 into text2.
		 * A set of diffs will be computed.
		 * @param text1 Old text.
		 * @param text2 New text.
		 * @return List of Patch objects.
		 */
		public List<Patch> patch_make (string text1, string text2) {
			// Check for null inputs not needed since null can't be passed in C#.
			// No diffs provided, comAdde our own.
			List<Diff> diffs = diff_main(text1, text2, true);
			if (diffs.Count > 2) {
				diff_cleanupSemantic(diffs);
				diff_cleanupEfficiency(diffs);
			}
			return patch_make(text1, diffs);
		}

		/**
		 * Compute a list of patches to turn text1 into text2.
		 * text1 will be derived from the provided diffs.
		 * @param diffs Array of diff tuples for text1 to text2.
		 * @return List of Patch objects.
		 */
		public List<Patch> patch_make (List<Diff> diffs) {
			// Check for null inputs not needed since null can't be passed in C#.
			// No origin string provided, comAdde our own.
			string text1 = diff_text1(diffs);
			return patch_make(text1, diffs);
		}

		/**
		 * Compute a list of patches to turn text1 into text2.
		 * text2 is ignored, diffs are the delta between text1 and text2.
		 * @param text1 Old text
		 * @param text2 Ignored.
		 * @param diffs Array of diff tuples for text1 to text2.
		 * @return List of Patch objects.
		 * @deprecated Prefer patch_make(string text1, List<Diff> diffs).
		 */
		public List<Patch> patch_make (string text1, string text2,
			List<Diff> diffs) {
			return patch_make(text1, diffs);
		}

		/**
		 * Compute a list of patches to turn text1 into text2.
		 * text2 is not provided, diffs are the delta between text1 and text2.
		 * @param text1 Old text.
		 * @param diffs Array of diff tuples for text1 to text2.
		 * @return List of Patch objects.
		 */
		public List<Patch> patch_make (string text1, List<Diff> diffs) {
			// Check for null inputs not needed since null can't be passed in C#.
			var patches = new List<Patch>();
			if (diffs.Count == 0) {
				return patches;  // Get rid of the null case.
			}
			var patch = new Patch();
			int char_count1 = 0;  // Number of characters into the text1 string.
			int char_count2 = 0;  // Number of characters into the text2 string.
			// Start with text1 (prepatch_text) and apply the diffs until we arrive at
			// text2 (postpatch_text). We recreate the patches one by one to determine
			// context info.
			string prepatch_text = text1;
			string postpatch_text = text1;
			foreach (Diff aDiff in diffs) {
				if (patch.diffs.Count == 0 && aDiff.operation != Operation.Equal) {
					// A new patch starts here.
					patch.start1 = char_count1;
					patch.start2 = char_count2;
				}

				switch (aDiff.operation) {
					case Operation.Insert:
						patch.diffs.Add(aDiff);
						patch.length2 += aDiff.text.Length;
						postpatch_text = postpatch_text.Insert(char_count2, aDiff.text);
						break;
					case Operation.Delete:
						patch.length1 += aDiff.text.Length;
						patch.diffs.Add(aDiff);
						postpatch_text = postpatch_text.Remove(char_count2,
							aDiff.text.Length);
						break;
					case Operation.Equal:
						if (aDiff.text.Length <= 2 * Patch_Margin
							&& patch.diffs.Count() != 0 && aDiff != diffs.Last()) {
							// Small equality inside a patch.
							patch.diffs.Add(aDiff);
							patch.length1 += aDiff.text.Length;
							patch.length2 += aDiff.text.Length;
						}

						if (aDiff.text.Length >= 2 * Patch_Margin) {
							// Time for a new patch.
							if (patch.diffs.Count != 0) {
								patch_addContext(patch, prepatch_text);
								patches.Add(patch);
								patch = new Patch();
								// Unlike Unidiff, our patch lists have a rolling context.
								// http://code.google.com/p/google-diff-match-patch/wiki/Unidiff
								// Update prepatch text & pos to reflect the application of the
								// just completed patch.
								prepatch_text = postpatch_text;
								char_count1 = char_count2;
							}
						}
						break;
				}

				// Update the current character count.
				if (aDiff.operation != Operation.Insert) {
					char_count1 += aDiff.text.Length;
				}
				if (aDiff.operation != Operation.Delete) {
					char_count2 += aDiff.text.Length;
				}
			}
			// Pick up the leftover patch if not empty.
			if (patch.diffs.Count != 0) {
				patch_addContext(patch, prepatch_text);
				patches.Add(patch);
			}

			return patches;
		}

		/**
		 * Given an array of patches, return another array that is identical.
		 * @param patches Array of patch objects.
		 * @return Array of patch objects.
		 */
		public List<Patch> patch_deepCopy (List<Patch> patches) {
			var patchesCopy = new List<Patch>();
			foreach (Patch aPatch in patches) {
				var patchCopy = new Patch();
				foreach (Diff aDiff in aPatch.diffs) {
					var diffCopy = new Diff(aDiff.operation, aDiff.text);
					patchCopy.diffs.Add(diffCopy);
				}
				patchCopy.start1 = aPatch.start1;
				patchCopy.start2 = aPatch.start2;
				patchCopy.length1 = aPatch.length1;
				patchCopy.length2 = aPatch.length2;
				patchesCopy.Add(patchCopy);
			}
			return patchesCopy;
		}

		/**
		 * Merge a set of patches onto the text.  Return a patched text, as well
		 * as an array of true/false values indicating which patches were applied.
		 * @param patches Array of patch objects
		 * @param text Old text.
		 * @return Two element Object array, containing the new text and an array of
		 *      bool values.
		 */
		public Object[] patch_apply (List<Patch> patches, string text) {
			if (patches.Count == 0) {
				return new Object[] { text, new bool[0] };
			}

			// Deep copy the patches so that no changes are made to originals.
			patches = patch_deepCopy(patches);

			string nullPadding = patch_addPadding(patches);
			text = nullPadding + text + nullPadding;
			patch_splitMax(patches);

			int x = 0;
			// delta keeps track of the offset between the expected and actual
			// location of the previous patch.  If there are patches expected at
			// positions 10 and 20, but the first patch was found at 12, delta is 2
			// and the second patch has an effective expected position of 22.
			int delta = 0;
			var results = new bool[patches.Count];
			foreach (Patch aPatch in patches) {
				int expected_loc = aPatch.start2 + delta;
				string text1 = diff_text1(aPatch.diffs);
				int start_loc;
				int end_loc = -1;
				if (text1.Length > Match_MaxBits) {
					// patch_splitMax will only provide an oversized pattern
					// in the case of a monster delete.
					start_loc = match_main(text,
						text1.Substring(0, Match_MaxBits), expected_loc);
					if (start_loc != -1) {
						end_loc = match_main(text,
							text1.Substring(text1.Length - Match_MaxBits),
							expected_loc + text1.Length - Match_MaxBits);
						if (end_loc == -1 || start_loc >= end_loc) {
							// Can't find valid trailing context.  Drop this patch.
							start_loc = -1;
						}
					}
				} else {
					start_loc = match_main(text, text1, expected_loc);
				}
				if (start_loc == -1) {
					// No match found.  :(
					results[x] = false;
					// Subtract the delta for this failed patch from subsequent patches.
					delta -= aPatch.length2 - aPatch.length1;
				} else {
					// Found a match.  :)
					results[x] = true;
					delta = start_loc - expected_loc;
					string text2 = text.JavaSubstring(start_loc,
					                                  (end_loc == -1) ? Math.Min(start_loc + text1.Length, text.Length) : Math.Min(end_loc + Match_MaxBits, text.Length));
					if (text1 == text2) {
						// Perfect match, just shove the Replacement text in.
						text = text.Substring(0, start_loc) + diff_text2(aPatch.diffs)
							+ text.Substring(start_loc + text1.Length);
					} else {
						// Imperfect match.  Run a diff to get a framework of equivalent
						// indices.
						List<Diff> diffs = diff_main(text1, text2, false);
						if (text1.Length > Match_MaxBits
							&& diff_levenshtein(diffs) / (float)text1.Length
							> Patch_DeleteThreshold) {
							// The end points match, but the content is unacceptably bad.
							results[x] = false;
						} else {
							diff_cleanupSemanticLossless(diffs);
							int index1 = 0;
							foreach (Diff aDiff in aPatch.diffs) {
								if (aDiff.operation != Operation.Equal) {
									int index2 = diff_xIndex(diffs, index1);
									if (aDiff.operation == Operation.Insert) {
										// Insertion
										text = text.Insert(start_loc + index2, aDiff.text);
									} else if (aDiff.operation == Operation.Delete) {
										// Deletion
										text = text.Remove(start_loc + index2, diff_xIndex(diffs,
											index1 + aDiff.text.Length) - index2);
									}
								}
								if (aDiff.operation != Operation.Delete) {
									index1 += aDiff.text.Length;
								}
							}
						}
					}
				}
				x++;
			}
			// Strip the padding off.
			text = text.Substring(nullPadding.Length, text.Length
				- 2 * nullPadding.Length);
			return new Object[] { text, results };
		}

		/**
		 * Add some padding on text start and end so that edges can match something.
		 * Intended to be called only from within patch_apply.
		 * @param patches Array of patch objects.
		 * @return The padding string added to each side.
		 */
		public string patch_addPadding (List<Patch> patches) {
			short paddingLength = Patch_Margin;
			string nullPadding = string.Empty;
			for (short x = 1; x <= paddingLength; x++) {
				nullPadding += (char)x;
			}

			// Bump all the patches forward.
			foreach (Patch aPatch in patches) {
				aPatch.start1 += paddingLength;
				aPatch.start2 += paddingLength;
			}

			// Add some padding on start of first diff.
			Patch patch = patches.First();
			List<Diff> diffs = patch.diffs;
			if (diffs.Count == 0 || diffs.First().operation != Operation.Equal) {
				// Add nullPadding equality.
				diffs.Insert(0, new Diff(Operation.Equal, nullPadding));
				patch.start1 -= paddingLength;  // Should be 0.
				patch.start2 -= paddingLength;  // Should be 0.
				patch.length1 += paddingLength;
				patch.length2 += paddingLength;
			} else if (paddingLength > diffs.First().text.Length) {
				// Grow first equality.
				Diff firstDiff = diffs.First();
				int extraLength = paddingLength - firstDiff.text.Length;
				firstDiff.text = nullPadding.Substring(firstDiff.text.Length)
					+ firstDiff.text;
				patch.start1 -= extraLength;
				patch.start2 -= extraLength;
				patch.length1 += extraLength;
				patch.length2 += extraLength;
			}

			// Add some padding on end of last diff.
			patch = patches.Last();
			diffs = patch.diffs;
			if (diffs.Count == 0 || diffs.Last().operation != Operation.Equal) {
				// Add nullPadding equality.
				diffs.Add(new Diff(Operation.Equal, nullPadding));
				patch.length1 += paddingLength;
				patch.length2 += paddingLength;
			} else if (paddingLength > diffs.Last().text.Length) {
				// Grow last equality.
				Diff lastDiff = diffs.Last();
				int extraLength = paddingLength - lastDiff.text.Length;
				lastDiff.text += nullPadding.Substring(0, extraLength);
				patch.length1 += extraLength;
				patch.length2 += extraLength;
			}

			return nullPadding;
		}

		/**
		 * Look through the patches and break up any which are longer than the
		 * maximum limit of the match algorithm.
		 * Intended to be called only from within patch_apply.
		 * @param patches List of Patch objects.
		 */
		public void patch_splitMax (List<Patch> patches) {
			const short patch_size = Match_MaxBits;
			for (int x = 0; x < patches.Count; x++) {
				if (patches[x].length1 > patch_size) {
					Patch bigpatch = patches[x];
					// Remove the big old patch.
					patches.Splice(x--, 1);
					int start1 = bigpatch.start1;
					int start2 = bigpatch.start2;
					string precontext = string.Empty;
					while (bigpatch.diffs.Count != 0) {
						// Create one of several smaller patches.
						var patch = new Patch();
						bool empty = true;
						patch.start1 = start1 - precontext.Length;
						patch.start2 = start2 - precontext.Length;
						if (precontext.Length != 0) {
							patch.length1 = patch.length2 = precontext.Length;
							patch.diffs.Add(new Diff(Operation.Equal, precontext));
						}
						while (bigpatch.diffs.Count != 0
							&& patch.length1 < patch_size - Patch_Margin) {
							Operation diff_type = bigpatch.diffs[0].operation;
							if (bigpatch.diffs[0].text == null) throw new Exception("Diff text null");
							string diff_text = bigpatch.diffs[0].text;
							if (diff_type == Operation.Insert) {
								// Insertions are harmless.
								patch.length2 += diff_text.Length;
								start2 += diff_text.Length;
								patch.diffs.Add(bigpatch.diffs.First());
								bigpatch.diffs.RemoveAt(0);
								empty = false;
							} else if (diff_type == Operation.Delete && patch.diffs.Count == 1
								&& patch.diffs.First().operation == Operation.Equal
								&& diff_text.Length > 2 * patch_size) {
								// This is a large deletion.  Let it pass in one chunk.
								patch.length1 += diff_text.Length;
								start1 += diff_text.Length;
								empty = false;
								patch.diffs.Add(new Diff(diff_type, diff_text));
								bigpatch.diffs.RemoveAt(0);
							} else {
								// Deletion or equality.  Only take as much as we can stomach.
								diff_text = diff_text.Substring(0, Math.Min(diff_text.Length,
									patch_size - patch.length1 - Patch_Margin));
								patch.length1 += diff_text.Length;
								start1 += diff_text.Length;
								if (diff_type == Operation.Equal) {
									patch.length2 += diff_text.Length;
									start2 += diff_text.Length;
								} else {
									empty = false;
								}
								patch.diffs.Add(new Diff(diff_type, diff_text));
								if (diff_text == bigpatch.diffs[0].text) {
									bigpatch.diffs.RemoveAt(0);
								} else {
									bigpatch.diffs[0].text =
										bigpatch.diffs[0].text.Substring(diff_text.Length);
								}
							}
						}
						// Compute the head context for the next patch.
						precontext = diff_text2(patch.diffs);
						precontext = precontext.Substring(Math.Max(0,
							precontext.Length - Patch_Margin));

						string postcontext;
						// Append the end context for this patch.
						if (diff_text1(bigpatch.diffs).Length > Patch_Margin) {
							postcontext = diff_text1(bigpatch.diffs)
								.Substring(0, Patch_Margin);
						} else {
							postcontext = diff_text1(bigpatch.diffs);
						}

						if (postcontext.Length != 0) {
							patch.length1 += postcontext.Length;
							patch.length2 += postcontext.Length;
							if (patch.diffs.Count != 0
								&& patch.diffs[patch.diffs.Count - 1].operation
								== Operation.Equal) {
								patch.diffs[patch.diffs.Count - 1].text += postcontext;
							} else {
								patch.diffs.Add(new Diff(Operation.Equal, postcontext));
							}
						}
						if (!empty) {
							patches.Splice(++x, 0, patch);
						}
					}
				}
			}
		}

		/**
		 * Take a list of patches and return a textual representation.
		 * @param patches List of Patch objects.
		 * @return Text representation of patches.
		 */
		public string patch_toText (List<Patch> patches) {
			var text = new StringBuilder();
			foreach (Patch aPatch in patches) {
				text.Append(aPatch);
			}
			return text.ToString();
		}

		/**
		 * Parse a textual representation of patches and return a List of Patch
		 * objects.
		 * @param textline Text representation of patches.
		 * @return List of Patch objects.
		 * @throws ArgumentException If invalid input.
		 */
		public List<Patch> patch_fromText (string textline) {
			var patches = new List<Patch>();
			if (textline.Length == 0) {
				return patches;
			}
			string[] text = textline.Split('\n');
			int textPointer = 0;
			Patch patch;
			var patchHeader = new Regex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$");
			Match m;
			string line;
			while (textPointer < text.Length) {
				m = patchHeader.Match(text[textPointer]);
				if (!m.Success) {
					throw new ArgumentException("Invalid patch string: "
						+ text[textPointer]);
				}
				patch = new Patch();
				patches.Add(patch);
				patch.start1 = Convert.ToInt32(m.Groups[1].Value);
				if (m.Groups[2].Length == 0) {
					patch.start1--;
					patch.length1 = 1;
				} else if (m.Groups[2].Value == "0") {
					patch.length1 = 0;
				} else {
					patch.start1--;
					patch.length1 = Convert.ToInt32(m.Groups[2].Value);
				}

				patch.start2 = Convert.ToInt32(m.Groups[3].Value);
				if (m.Groups[4].Length == 0) {
					patch.start2--;
					patch.length2 = 1;
				} else if (m.Groups[4].Value == "0") {
					patch.length2 = 0;
				} else {
					patch.start2--;
					patch.length2 = Convert.ToInt32(m.Groups[4].Value);
				}
				textPointer++;

				while (textPointer < text.Length) {
					char sign;
					try {
						sign = text[textPointer][0];
					} catch (IndexOutOfRangeException) {
						// Blank line?  Whatever.
						textPointer++;
						continue;
					}
					line = text[textPointer].Substring(1);
					line = line.Replace("+", "%2b");
					line = HttpUtility.UrlDecode(line, new UTF8Encoding(false, true));
					if (sign == '-') {
						// Deletion.
						patch.diffs.Add(new Diff(Operation.Delete, line));
					} else if (sign == '+') {
						// Insertion.
						patch.diffs.Add(new Diff(Operation.Insert, line));
					} else if (sign == ' ') {
						// Minor equality.
						patch.diffs.Add(new Diff(Operation.Equal, line));
					} else if (sign == '@') {
						// Start of next patch.
						break;
					} else {
						// WTF?
						throw new ArgumentException(
							"Invalid patch mode '" + sign + "' in: " + line);
					}
					textPointer++;
				}
			}
			return patches;
		}

		/**
		 * Unescape selected chars for compatability with JavaScript's encodeURI.
		 * In speed critical applications this could be dropped since the
		 * receiving application will certainly decode these fine.
		 * Note that this function is case-sensitive.  Thus "%3F" would not be
		 * unescaped.  But this is ok because it is only called with the output of
		 * HttpUtility.UrlEncode which returns lowercase hex.
		 *
		 * Example: "%3f" -> "?", "%24" -> "$", etc.
		 *
		 * @param str The string to escape.
		 * @return The escaped string.
		 */
		public static string unescapeForEncodeUriCompatability (string str) {
			return str.Replace("%21", "!").Replace("%7e", "~")
				.Replace("%27", "'").Replace("%28", "(").Replace("%29", ")")
				.Replace("%3b", ";").Replace("%2f", "/").Replace("%3f", "?")
				.Replace("%3a", ":").Replace("%40", "@").Replace("%26", "&")
				.Replace("%3d", "=").Replace("%2b", "+").Replace("%24", "$")
				.Replace("%2c", ",").Replace("%23", "#");
		}
	}
}
