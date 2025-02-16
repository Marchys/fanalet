using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = System.Object;
using Random = System.Random;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/Modifiers/Alternative Path")]
	[Serializable]
	/** Applies penalty to the paths it processes telling other units to avoid choosing the same path.
	 * 
	 * Note that this might not work properly if penalties are modified by other actions as well (e.g graph update objects which reset the penalty to zero).
	 * It will only work when all penalty modifications are relative, i.e adding or subtracting penalties, but not when setting penalties
	 * to specific values.
	 * 
	 * When destroyed, it will correctly remove any added penalty. However it will only be done before the next
	 * path request is calculated, so if this was the last path requesting unit to be destroyed, the penalty will stay
	 * in the graph until the next path request is made.
	 * 
	 * \ingroup modifiers
	 */
	public class AlternativePath : MonoModifier {
		
	#if UNITY_EDITOR
		[MenuItem ("CONTEXT/Seeker/Add Alternative Path Modifier")]
		public static void AddComp (MenuCommand command) {
			(command.context as Component).gameObject.AddComponent (typeof(AlternativePath));
		}
	#endif
		
		public override ModifierData input {
			get { return ModifierData.Original; }
		}
		
		public override ModifierData output {
			get { return ModifierData.All; }
		}
		
		/** How much penalty (weight) to apply to nodes */
		public int penalty = 1000;
		
		/** Max number of nodes to skip in a row */
		public int randomStep = 10;
		
		/** The previous path */
		GraphNode[] prevNodes;
		
		int prevSeed; /**< Previous seed. Used to figure out which nodes to revert penalty on without storing them in an array */
		int prevPenalty = 0; /**< The previous penalty used. Stored just in case it changes during operation */
		
		bool waitingForApply = false;
		
		Object lockObject = new Object ();
		
		/** A random object */
		private Random rnd = new Random ();
		
		/** A random object generating random seeds for other random objects */
		private Random seedGenerator = new Random ();
		
		private bool destroyed = false;
		
		/** The nodes waiting to have their penalty changed */
		GraphNode[] toBeApplied;
		public override void Apply (Path p, ModifierData source) {
			
			if (this == null) return;
			
			lock (lockObject) {
				toBeApplied = p.path.ToArray();
				
				if (!waitingForApply) {
					waitingForApply = true;
					AstarPath.OnPathPreSearch += ApplyNow;
				}
			}
		}
		
		public new void OnDestroy () {
			destroyed = true;
			lock (lockObject) {
				if (!waitingForApply) {
					waitingForApply = true;
					AstarPath.OnPathPreSearch += ClearOnDestroy;
				}
			}
			(this as MonoModifier).OnDestroy();
		}
		
		void ClearOnDestroy (Path p) {
			lock (lockObject) {
				AstarPath.OnPathPreSearch -= ClearOnDestroy;
				waitingForApply = false;
				InversePrevious ();
			}
		}
			
		void InversePrevious () {
			var seed = prevSeed;
			rnd = new Random (seed);
			
			//Add previous penalty
			if (prevNodes != null) {
				var warnPenalties = false;
				var rndStart = rnd.Next (randomStep);
				for (var i=rndStart;i<prevNodes.Length;i+= rnd.Next (1,randomStep)) {
					if (prevNodes[i].Penalty < prevPenalty) {
						warnPenalties = true;
					}
					prevNodes[i].Penalty = (uint)(prevNodes[i].Penalty-prevPenalty);
				}
				if (warnPenalties) {
					Debug.LogWarning ("Penalty for some nodes has been reset while this modifier was active. Penalties might not be correctly set.");
				}
			}
		}
		
		void ApplyNow (Path somePath) {
			lock (lockObject) {
				waitingForApply = false;
				AstarPath.OnPathPreSearch -= ApplyNow;
				
				InversePrevious ();
				
				if (destroyed) return;
				
				//Calculate a new seed
				var seed = seedGenerator.Next ();
				rnd = new Random (seed);
				
				if (toBeApplied != null) {
					var rndStart = rnd.Next (randomStep);
					for (var i=rndStart;i<toBeApplied.Length;i+= rnd.Next (1,randomStep)) {
						toBeApplied[i].Penalty = (uint)(toBeApplied[i].Penalty+penalty);
					}
				}
				
				prevPenalty = penalty;
				prevSeed = seed;
				prevNodes = toBeApplied;
			}
		}
	}
}