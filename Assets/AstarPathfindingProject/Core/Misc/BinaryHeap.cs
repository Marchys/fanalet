//#define ASTARDEBUG
#define TUPLE
#pragma warning disable 162
#pragma warning disable 429
using System;

namespace Pathfinding {
	/** Binary heap implementation. Binary heaps are really fast for ordering nodes in a way that makes it possible to get the node with the lowest F score. Also known as a priority queue.
	 * 
	 * This has actually been rewritten as an n-ary heap (by default a 4-ary heap) for performance, but it's the same principle.
	 * 
	 * \see http://en.wikipedia.org/wiki/Binary_heap
	 */
	public class BinaryHeapM { 

		public int numberOfItems; 
		
		public float growthFactor = 2;

		public const int D = 4;

		const bool SortGScores = true;

		private Tuple[] binaryHeap; 

		private struct Tuple {
			public uint F;
			public PathNode node;

			public Tuple ( uint F, PathNode node ) {
				this.F = F;
				this.node = node;
			}
		}

		public BinaryHeapM ( int numberOfElements ) { 
			binaryHeap = new Tuple[numberOfElements]; 
			numberOfItems = 0;
		}
		
		public void Clear () {
			numberOfItems = 0;
		}
		
		internal PathNode GetNode (int i) {
			return binaryHeap[i].node;
		}

		internal void SetF (int i, uint F) {
			binaryHeap[i].F = F;
		}

		/** Adds a node to the heap */
		public void Add(PathNode node) {
			
			if (node == null) throw new ArgumentNullException ("Sending null node to BinaryHeap");

			if (numberOfItems == binaryHeap.Length) {
				var newSize = Math.Max(binaryHeap.Length+4,(int)Math.Round(binaryHeap.Length*growthFactor));
				if (newSize > 1<<18) {
					throw new Exception ("Binary Heap Size really large (2^18). A heap size this large is probably the cause of pathfinding running in an infinite loop. " +
						"\nRemove this check (in BinaryHeap.cs) if you are sure that it is not caused by a bug");
				}

				var tmp = new Tuple[newSize];

				for (var i=0;i<binaryHeap.Length;i++) {
					tmp[i] = binaryHeap[i];
				}
				binaryHeap = tmp;
				
				//Debug.Log ("Forced to discard nodes because of binary heap size limit, please consider increasing the size ("+numberOfItems +" "+binaryHeap.Length+")");
				//numberOfItems--;
			}

			var obj = new Tuple(node.F,node);
			binaryHeap[numberOfItems] = obj;

			//node.heapIndex = numberOfItems;//Heap index

			var bubbleIndex = numberOfItems;
			var nodeF = node.F;
			var nodeG = node.G;

			//Debug.Log ( "Adding node with " + nodeF + " to index " + numberOfItems);
			
			while (bubbleIndex != 0 ) {
				var parentIndex = (bubbleIndex-1) / D;

				//Debug.Log ("Testing " + nodeF + " < " + binaryHeap[parentIndex].F);

				if (nodeF < binaryHeap[parentIndex].F || (nodeF == binaryHeap[parentIndex].F && nodeG > binaryHeap[parentIndex].node.G)) {

				   	
					//binaryHeap[bubbleIndex].f <= binaryHeap[parentIndex].f) { /* \todo Wouldn't it be more efficient with '<' instead of '<=' ? * /
					//Node tmpValue = binaryHeap[parentIndex];
					
					//tmpValue.heapIndex = bubbleIndex;//HeapIndex
					
					binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
					binaryHeap[parentIndex] = obj;
					
					//binaryHeap[bubbleIndex].heapIndex = bubbleIndex; //Heap index
					//binaryHeap[parentIndex].heapIndex = parentIndex; //Heap index
					
					bubbleIndex = parentIndex;
				} else {
					break;
				}
			}

			numberOfItems++;

			//Validate();
		}
		
		/** Returns the node with the lowest F score from the heap */
		public PathNode Remove() {
			numberOfItems--;
			var returnItem = binaryHeap[0].node;

		 	//returnItem.heapIndex = 0;//Heap index
			
			binaryHeap[0] = binaryHeap[numberOfItems];
			//binaryHeap[1].heapIndex = 1;//Heap index
			
			int swapItem = 0, parent = 0;
			
			do {

				if (D == 0) {
					parent = swapItem;
					var p2 = parent * D;
					if (p2 + 1 <= numberOfItems) {
						// Both children exist
						if (binaryHeap[parent].F > binaryHeap[p2].F) {
							swapItem = p2;//2 * parent;
						}
						if (binaryHeap[swapItem].F > binaryHeap[p2 + 1].F) {
							swapItem = p2 + 1;
						}
					} else if ((p2) <= numberOfItems) {
						// Only one child exists
						if (binaryHeap[parent].F > binaryHeap[p2].F) {
							swapItem = p2;
						}
					}
				} else {
					parent = swapItem;
					var swapF = binaryHeap[swapItem].F;
					var pd = parent * D + 1;
					
					if (D >= 1 && pd+0 <= numberOfItems && (binaryHeap[pd+0].F < swapF || (SortGScores && binaryHeap[pd+0].F == swapF && binaryHeap[pd+0].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+0].F;
						swapItem = pd+0;
					}
					
					if (D >= 2 && pd+1 <= numberOfItems && (binaryHeap[pd+1].F < swapF  || (SortGScores && binaryHeap[pd+1].F == swapF && binaryHeap[pd+1].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+1].F;
						swapItem = pd+1;
					}
					
					if (D >= 3 && pd+2 <= numberOfItems && (binaryHeap[pd+2].F < swapF  || (SortGScores && binaryHeap[pd+2].F == swapF && binaryHeap[pd+2].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+2].F;
						swapItem = pd+2;
					}
					
					if (D >= 4 && pd+3 <= numberOfItems && (binaryHeap[pd+3].F < swapF  || (SortGScores && binaryHeap[pd+3].F == swapF && binaryHeap[pd+3].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+3].F;
						swapItem = pd+3;
					}
					
					if (D >= 5 && pd+4 <= numberOfItems && binaryHeap[pd+4].F < swapF ) {
						swapF = binaryHeap[pd+4].F;
						swapItem = pd+4;
					}
					
					if (D >= 6 && pd+5 <= numberOfItems && binaryHeap[pd+5].F < swapF ) {
						swapF = binaryHeap[pd+5].F;
						swapItem = pd+5;
					}
					
					if (D >= 7 && pd+6 <= numberOfItems && binaryHeap[pd+6].F < swapF ) {
						swapF = binaryHeap[pd+6].F;
						swapItem = pd+6;
					}
					
					if (D >= 8 && pd+7 <= numberOfItems && binaryHeap[pd+7].F < swapF ) {
						swapF = binaryHeap[pd+7].F;
						swapItem = pd+7;
					}
					
					if (D >= 9 && pd+8 <= numberOfItems && binaryHeap[pd+8].F < swapF ) {
						swapF = binaryHeap[pd+8].F;
						swapItem = pd+8;
					}
				}
				
				// One if the parent's children are smaller or equal, swap them
				if (parent != swapItem) {
					var tmpIndex = binaryHeap[parent];
					//tmpIndex.heapIndex = swapItem;//Heap index
					
					binaryHeap[parent] = binaryHeap[swapItem];
					binaryHeap[swapItem] = tmpIndex;
					
					//binaryHeap[parent].heapIndex = parent;//Heap index
				} else {
					break;
				}
			} while (true);//parent != swapItem);

			//Validate ();

			return returnItem;
		}

		void Validate () {
			for ( var i = 1; i < numberOfItems; i++ ) {
				var parentIndex = (i-1)/D;
				if ( binaryHeap[parentIndex].F > binaryHeap[i].F ) {
					throw new Exception ("Invalid state at " + i + ":" +  parentIndex + " ( " + binaryHeap[parentIndex].F + " > " + binaryHeap[i].F + " ) " );
				}
			}
		}

		/** Rebuilds the heap by trickeling down all items.
		 * Usually called after the hTarget on a path has been changed */
		public void Rebuild () {
			
			for (var i=2;i<numberOfItems;i++) {
				var bubbleIndex = i;
				var node = binaryHeap[i];
				var nodeF = node.F;
				while (bubbleIndex != 1) {
					var parentIndex = bubbleIndex / D;
					
					if (nodeF < binaryHeap[parentIndex].F) {
						//Node tmpValue = binaryHeap[parentIndex];
						binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
						binaryHeap[parentIndex] = node;
						bubbleIndex = parentIndex;
					} else {
						break;
					}
				}
				
			}
			
			
		}
	}
}