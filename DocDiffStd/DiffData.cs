namespace DocDiffStd;

internal class DiffData {
	internal readonly int Length;
	internal readonly int[] Data;
	internal readonly bool[] Modified;

	internal DiffData (int[] initData) {
		Data = initData;
		Length = initData.Length;
		Modified = new bool[Length + 2];
	}
}