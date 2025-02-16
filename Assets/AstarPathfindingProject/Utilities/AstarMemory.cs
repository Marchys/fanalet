using System;

namespace Pathfinding.Util
{
	public static class Memory
	{
		/** Sets all values in an array to a specific value faster than a loop.
		* Only faster for large arrays. Slower for small ones.
		* Tests indicate it becomes faster somewhere when the length of the array grows above around 100.
		* For large arrays this can be magnitudes faster. Up to 40 times faster has been measured.
		* 
		* \see System.Buffer.BlockCopy
		*/
		public static void MemSet(byte[] array, byte value) {
			if (array == null) {
				throw new ArgumentNullException("array");
			}
			
			int block = 32, index = 0;
			var length = Math.Min(block, array.Length);
			
			//Fill the initial array
			while (index < length) {
				array[index++] = value;
			}
			
			length = array.Length;
			while (index < length) {
				Buffer.BlockCopy(array, 0, array, index, Math.Min(block, length-index));
				index += block;
				block *= 2;
			}
		}
		
		/** Sets all values in an array to a specific value faster than a loop.
		* Only faster for large arrays. Slower for small ones.
		* Tests indicate it becomes faster somewhere when the length of the array grows above around 100.
		* For large arrays this can be magnitudes faster. Up to 40 times faster has been measured.
		* 
		* \note Only works on primitive value types such as int, long, float, etc.
		* 
		* \param array the array to fill
		* \param value the value to fill the array with
		* \param byteSize size in bytes of every element in the array. e.g 4 bytes for an int, or 8 bytes for a long.
		* It can be efficiently got using the sizeof built-in function.
		* 
\code
//Set all values to 8 in the array
int[] arr = new int[20000];
Pathfinding.Util.Memory.MemSet<int> (arr, 8, sizeof(int));
\endcode
		* \see System.Buffer.BlockCopy
		*/
		public static void MemSet<T> (T[] array, T value, int byteSize) where T : struct {
			if (array == null) {
				throw new ArgumentNullException("array");
			}
			
			int block = 32, index = 0;
			var length = Math.Min(block, array.Length);
			
			//Fill the initial array
			while (index < length) {
				array[index] = value;
				index++;
			}
			
			length = array.Length;
			while (index < length) {
				Buffer.BlockCopy(array, 0, array, index*byteSize, Math.Min(block, length-index)*byteSize);
				index += block;
				block *= 2;
			}
		}
		
		/** Sets all values in an array to a specific value faster than a loop.
		* Only faster for large arrays. Slower for small ones.
		* Tests indicate it becomes faster somewhere when the length of the array grows above around 100.
		* For large arrays this can be magnitudes faster. Up to 40 times faster has been measured.
		* 
		* \note Only works on primitive value types such as int, long, float, etc.
		* 
		* \param array the array to fill
		* \param value the value to fill the array with
		* \param byteSize size in bytes of every element in the array. e.g 4 bytes for an int, or 8 bytes for a long.
		* \param totalSize all indices in the range [0, totalSize-1] will be set
		* 
		* It can be efficiently got using the sizeof built-in function.
		* 
\code
//Set all values to 8 in the array
int[] arr = new int[20000];
Pathfinding.Util.Memory.MemSet<int> (arr, 8, sizeof(int));
\endcode
		* \see System.Buffer.BlockCopy
		*/
		public static void MemSet<T> (T[] array, T value, int totalSize, int byteSize) where T : struct {
			if (array == null) {
				throw new ArgumentNullException("array");
			}
			
			int block = 32, index = 0;
			var length = Math.Min(block, totalSize);
			
			//Fill the initial array
			while (index < length) {
				array[index] = value;
				index++;
			}
			
			length = totalSize;
			while (index < length) {
				Buffer.BlockCopy(array, 0, array, index*byteSize, Math.Min(block, totalSize-index)*byteSize);
				index += block;
				block *= 2;
			}
		}
	}
}

