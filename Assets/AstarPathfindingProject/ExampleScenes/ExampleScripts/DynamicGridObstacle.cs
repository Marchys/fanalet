//#define ASTARDEBUG //Enable for some debug lines showing the DynamicGridObstacle activity

using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

/** Attach this script to any obstacle with a collider to enable dynamic updates of the graphs around it.
 * When the object has moved a certain distance (or actually when it's bounding box has changed by a certain amount) defined by #updateError
 * it will call AstarPath.UpdateGraphs and update the graph around it.
 * 
 * \note This script does only work with GridGraph, PointGraph and LayerGridGraph
 * 
 * \see AstarPath.UpdateGraphs
 */
public class DynamicGridObstacle : MonoBehaviour {
	
	Collider col;
	public float updateError = 1; /**< The minimum change along one of the axis of the bounding box of collider to trigger a graph update */
	public float checkTime = 0.2F; /**< Time in seconds between bounding box checks */
	
	/** Use this for initialization */
	void Start () {
		col = GetComponent<Collider>();
		if (GetComponent<Collider>() == null) {
			Debug.LogError ("A collider must be attached to the GameObject for DynamicGridObstacle to work");
		}
		StartCoroutine (UpdateGraphs ());
	}
	
	Bounds prevBounds;
	bool isWaitingForUpdate = false;
	
	/** Coroutine which checks for changes in the collider's bounding box */
	IEnumerator UpdateGraphs () {
		
		if (col == null || AstarPath.active == null) {
			Debug.LogWarning ("No collider is attached to the GameObject. Canceling check");
			yield break;
		}
		
		//Perform update checks while there is a collider attached to the GameObject
		while (col) {
			
			while (isWaitingForUpdate) {
				yield return new WaitForSeconds (checkTime);
			}
			
			//The current bounds
			var newBounds = col.bounds;
			
			//The combined bounds of the previous bounds and the new bounds
			var merged = newBounds;
			merged.Encapsulate (prevBounds);
			
			var minDiff = merged.min - newBounds.min;
			var maxDiff = merged.max - newBounds.max;
			
			//If the difference between the previous bounds and the new bounds is greater than some value, update the graphs
			if (Mathf.Abs (minDiff.x) > updateError || Mathf.Abs (minDiff.y) > updateError || Mathf.Abs (minDiff.z) > updateError ||
			   	Mathf.Abs (maxDiff.x) > updateError || Mathf.Abs (maxDiff.y) > updateError || Mathf.Abs (maxDiff.z) > updateError) {
				
				//Update the graphs as soon as possible
				isWaitingForUpdate = true;
				/** \bug Fix Update Graph Passes */
				DoUpdateGraphs ();
			}
			
			yield return new WaitForSeconds (checkTime);
		}
		
		//The collider object has been removed from the GameObject, pretend the object has been destroyed
		OnDestroy ();
	}
	
	/** Revert graphs when destroyed.
	 * When the DynamicObstacle is destroyed, a last graph update should be done to revert nodes to their original state */
	public void OnDestroy () {
		if (AstarPath.active != null) {
			var guo = new GraphUpdateObject (prevBounds);
			AstarPath.active.UpdateGraphs (guo);
		}
	}
	
	public void DoUpdateGraphs () {
		if (col == null) { return; }
		
		isWaitingForUpdate = false;
		var newBounds = col.bounds;
		
		var merged = newBounds;
		merged.Encapsulate (prevBounds);
		
		// Check what seems to be fastest, to update the union of prevBounds and newBounds in a single request
		// or to update them separately, the smallest volume is usually the fastest
		if (BoundsVolume (merged) < BoundsVolume (newBounds)+BoundsVolume(prevBounds)) {
			// Send an update request to update the nodes inside the 'merged' volume
			AstarPath.active.UpdateGraphs (merged);
		} else {
			// Send two update request to update the nodes inside the 'prevBounds' and 'newBounds' volumes
			AstarPath.active.UpdateGraphs (prevBounds);
			AstarPath.active.UpdateGraphs (newBounds);
		}
		
		
		prevBounds = newBounds;
	}
	
	/** Returns the volume of a Bounds object. X*Y*Z */
	static float BoundsVolume (Bounds b) {
		return Math.Abs (b.size.x * b.size.y * b.size.z);
	}
}
