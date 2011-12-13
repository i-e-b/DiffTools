namespace DocDiff {
	internal class DiffData {
		internal int Length;
		internal int[] Data;
		internal bool[] Modified;

		internal DiffData (int[] initData) {
			Data = initData;
			Length = initData.Length;
			Modified = new bool[Length + 2];
		}
	}
}