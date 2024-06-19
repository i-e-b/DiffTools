using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DocDiffStd;

/// <summary>
/// Create and use encodings of <see cref="Fragment"/>s, using the output of <see cref="Differences"/>.
/// <p/>
/// This can be used to make, store, and apply patch sets.
/// </summary>
public static class DiffCode
{
	#region Decode

	/// <summary>
	/// Build and return a revision by applying a set of DiffCodes to a base version.
	/// DiffCodes should be from CompressedDiffCode() or BuildDiffCode().
	/// </summary>
	public static string BuildRevision (string baseRevision, string diffCodes) {
		// Decide if it's compressed or not, and send to appropriate decoder.
		return diffCodes.StartsWith(CompressionMarker) 
			? BuildRevision(baseRevision, Convert.FromBase64String(diffCodes.Substring(CompressionMarker.Length))) 
			: DecodeRevision(baseRevision, diffCodes);
	}

	/// <summary>
	/// Build and return a revision by applying a set of DiffCodes to a base version.
	/// DiffCodes should be from StorageDiffCode().
	/// </summary>
	public static string BuildRevision (string baseRevision, byte[] diffCodes) {
		// Decompress and send to real decoder.
		var @in = new MemoryStream(diffCodes); // this will receive the compressed code
		var filter = new DeflateStream(@in, CompressionMode.Decompress);

		var @out = new List<byte>(diffCodes.Length);
		var b = filter.ReadByte();
		while (b >= 0) {
			@out.Add((byte)b);
			b = filter.ReadByte();
		}
		
		var bytes = @out.ToArray();
		return DecodeRevision(baseRevision, Encoding.UTF8.GetString(bytes, 0, bytes.Length));
	}


	/// <summary>
	/// Build and return a revision by applying a set of DiffCodes to a base version.
	/// DiffCodes should be from CompressedDiffCode() or BuildDiffCode().
	/// </summary>
	public static List<Fragment> BuildChanges (string baseRevision, string diffCodes) {
		// Decide if it's compressed or not, and send to appropriate decoder.
		return diffCodes.StartsWith(CompressionMarker) 
			? BuildChanges(baseRevision, Convert.FromBase64String(diffCodes.Substring(CompressionMarker.Length))) 
			: DecodeRevisionFragments(baseRevision, diffCodes);
	}

	/// <summary>
	/// Build and return a revision by applying a set of DiffCodes to a base version.
	/// DiffCodes should be from StorageDiffCode().
	/// </summary>
	public static List<Fragment> BuildChanges (string baseRevision, byte[] diffCodes) {
		// Decompress and send to real decoder.
		var @in = new MemoryStream(diffCodes); // this will receive the compressed code
		var filter = new DeflateStream(@in, CompressionMode.Decompress);

		var @out = new List<byte>(diffCodes.Length);
		var b = filter.ReadByte();
		while (b >= 0) {
			@out.Add((byte)b);
			b = filter.ReadByte();
		}

		var bytes = @out.ToArray();
		return DecodeRevisionFragments(baseRevision, Encoding.UTF8.GetString(bytes, 0, bytes.Length));
	}

	#region Inner Workings

	private static List<Fragment> DecodeRevisionFragments (string baseRevision, string diffCodes) {
		var @out = new List<Fragment>();
		var limit = baseRevision.Length - 1;

		if (string.IsNullOrEmpty(diffCodes)) {
			// no changes.
			@out.Add(new Fragment(Differences.FragmentType.Unchanged, baseRevision, 0));
			return @out;
		}
		
		var codes = SplitCodes(diffCodes);
		if (codes.Count < 1) {
			// no changes.
			@out.Add(new Fragment(Differences.FragmentType.Unchanged, baseRevision, 0));
			return @out;
		}


		DecodeDiffCode(codes[0], out var pos, out var del, out var ins);

		if (pos > 0) { // write leading text
			@out.Add(new Fragment(Differences.FragmentType.Unchanged, baseRevision.Substring(0, pos), 0));
		}

		var rightPos = pos;
		foreach (var code in codes) {
			var oldPos = pos;
			// get code
			DecodeDiffCode(code, out pos, out del, out ins);

			// write unchanged text
			if (pos > oldPos) {
				var l = (pos - oldPos);
				if (l + oldPos >= baseRevision.Length) {
					l = (baseRevision.Length - oldPos) - 1;
				}
				if (l >= 0) {
					var skip = baseRevision.Substring(oldPos, l);
					@out.Add(new Fragment(Differences.FragmentType.Unchanged, skip, rightPos));
					rightPos += l;
				} else {
					// error!
					throw new Exception("Diff code does not match text - Can't skip!");
				}
			}

			// Skip deleted text
			if (pos + del > limit)
			{
				if (pos > limit) throw new Exception($"Failed to decode changes. [{pos}..{pos + del}] is out of the input range [0..{limit}]");
				
				var delStr = baseRevision.Substring(pos);
				if (del > 0) @out.Add(new Fragment(Differences.FragmentType.Deleted, delStr, rightPos));
				pos += del;
			}
			else
			{
				var delStr = baseRevision.Substring(pos, del);
				if (del > 0) @out.Add(new Fragment(Differences.FragmentType.Deleted, delStr, rightPos));
				pos += del;
			}

			// Write any inserted text
			if (ins.Length > 0)
			{
				@out.Add(new Fragment(Differences.FragmentType.Inserted, ins, rightPos));
				rightPos += ins.Length;
			}
		}

		// Write any unchanged text at the end.
		if (pos < baseRevision.Length) {
			@out.Add(new Fragment(Differences.FragmentType.Unchanged, baseRevision.Substring(pos), rightPos));
		}

		return @out;
	}

	/// <summary>
	/// Split codes, as written by <see cref="WriteDiffCode"/>.
	/// These two must agree on CodeMarker and the escape sequence
	/// </summary>
	private static List<string> SplitCodes(string diffCodes)
	{
		var sb = new StringBuilder();
		var output = new List<string>();
		for (var index = 0; index < diffCodes.Length; index++)
		{
			var c = diffCodes[index];
			if (c is CodeMarker)
			{
				// if the next character is also a CodeMarker, we have an escape sequence
				if (index < diffCodes.Length - 1 && diffCodes[index+1] == CodeMarker)
				{
					sb.Append(c); // write the escaped char
					index++; // skip the duplication
				}
				else
				{
					// send any waiting part
					if (sb.Length > 0) output.Add(sb.ToString());
					sb.Clear();
				}
			}
			else sb.Append(c);
		}

		// Write any trailing part
		if (sb.Length > 0) output.Add(sb.ToString());
		return output;
	}

	/// <summary>
	/// Build and return a revision by applying a set of DiffCodes to a base version
	/// </summary>
	private static string DecodeRevision (string baseRevision, string diffCodes) {
		var frags = DecodeRevisionFragments(baseRevision, diffCodes);

		var sb = new StringBuilder();
		foreach (var frag in frags) {
			if (frag.Type == Differences.FragmentType.Deleted) continue;
			sb.Append(frag.Content);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Work out the meaning of a diff code
	/// </summary>
	private static void DecodeDiffCode (string code, out int position, out int deletes, out string inserts) {
		if (string.IsNullOrEmpty(code)) {
			throw new Exception("Empty codes not allowed");
		}
		var s1 = code.IndexOf(RangeMarker,1); // pos/del
		var s2 = code.IndexOf(InsertMarker); // [pos/del] / ins

		if (s1 < 0 || s2 < 0) throw new ArgumentException("Code was invalid");

		var pStr = code.Substring(0, s1).Trim();
		var dStr = code.Substring(s1+1, (s2-s1)-1).Trim();
		var iStr = (s2+1 >= code.Length) ? (null) : (code.Substring(s2 + 1)); // Never trim this!!

		if (string.IsNullOrEmpty(pStr)) {
			position = 0;
		} else {
			position = int.Parse(pStr);
		}

		if (string.IsNullOrEmpty(dStr)) {
			deletes = 0;
		} else {
			deletes = int.Parse(dStr);
		}

		if (string.IsNullOrEmpty(iStr)) {
			inserts = "";
		} else {
			inserts = iStr!;
		}
	}
	#endregion

	#endregion


	#region Encode
	/// <summary>
	/// Returns a compacted version of BuildDiffCode()
	/// for textual storage and transmission.
	/// </summary>
	public static string CompressedDiffCode (Differences differences) {
		var @out = new MemoryStream(); // this will receive the compressed code
		var filter = new DeflateStream(@out, CompressionMode.Compress);

		var buffer = Encoding.UTF8.GetBytes(BuildDiffCode (differences));
		filter.Write(buffer, 0, buffer.Length);
		filter.Flush();

		return CompressionMarker + Convert.ToBase64String(@out.ToArray()); // this is 7-bit safe and robust for storage
	}

	/// <summary>
	/// Returns a compacted version of BuildDiffCode()
	/// for binary storage
	/// </summary>
	public static byte[] StorageDiffCode (Differences differences) {
		var @out = new MemoryStream(); // this will receive the compressed code
		var filter = new DeflateStream(@out, CompressionMode.Compress);

		var buffer = Encoding.UTF8.GetBytes(BuildDiffCode(differences));
		filter.Write(buffer, 0, buffer.Length);
		filter.Flush();

		return @out.ToArray();
	}

	/// <summary>
	/// Build a code that can be used to transform one version of a file into another.
	/// </summary>
	public static string BuildDiffCode (IEnumerable<Fragment> differences) {
		var sb = new StringBuilder();
		var pos = 0;
		var del = 0;
		var ins = "";

		foreach (var frag in differences)
		{
			switch (frag.Type)
			{
				case Differences.FragmentType.Unchanged:
				{
					pos += del;
					pos -= ins.Length;
					if (del > 0 || ins.Length > 0) {
						WriteDiffCode(sb, pos-del, del, ins);
					}
					pos += frag.Length;
					del = 0;
					ins = "";
					break;
				}
				case Differences.FragmentType.Deleted:
					del += frag.Length;
					break;
				case Differences.FragmentType.Inserted:
					pos += frag.Length;
					ins += frag.Content;
					break;
				default:
					throw new Exception("broken frag type");
			}
		}

		// Don't forget the last one!
		if (del > 0 || ins.Length > 0) {
			WriteDiffCode(sb, pos - del, del, ins);
		}

		return sb.ToString();
	}

	#region Inner Workings
	/// <summary>Used by BuildDiffCode()</summary>
	private static void WriteDiffCode (StringBuilder sb, int pos, int del, string ins) {
		if (del == 0 && string.IsNullOrEmpty(ins)) return;
		sb.Append(CodeMarker);
		var p = pos;
		sb.Append(p);
		sb.Append(RangeMarker);
		if (del > 0) {
			sb.Append(del);
		}
		sb.Append(InsertMarker);
		if (!string.IsNullOrEmpty(ins)) {
			foreach (var c in ins)
			{
				if (c == CodeMarker) sb.Append(CodeMarker); // escape the code marker by doubling. See `SplitCodes()`
				sb.Append(c);
			}
		}
	}

	#endregion
	#endregion

	private const string CompressionMarker = "[DCMP]";
	private const char CodeMarker = '^';
	private const char RangeMarker = '-';
	private const char InsertMarker = ';';
}