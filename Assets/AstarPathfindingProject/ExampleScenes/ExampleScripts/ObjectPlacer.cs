using Pathfinding;
using UnityEngine;

/** Small sample script for placing obstacles */
public class ObjectPlacer : MonoBehaviour {
	
	public GameObject go; /** GameObject to place. Make sure the layer it is in is included in the collision mask on the GridGraph settings (assuming a GridGraph) */
	public bool direct = false; /** Flush Graph Updates directly after placing. Slower, but updates are applied immidiately */
	public bool issueGUOs = true; /** Issue a graph update object after placement */
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetKeyDown ("p")) {
			PlaceObject ();
		}
		if (Input.GetKeyDown ("r")) {
			RemoveObject ();
		}
	}
	
	public void PlaceObject () {
		
		var ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if ( Physics.Raycast (ray, out hit, Mathf.Infinity)) {
			var p = hit.point;
			
			var obj = (GameObject)Instantiate (go,p,Quaternion.identity);
			
			if (issueGUOs) {
				var b = obj.GetComponent<Collider>().bounds;
				//Pathfinding.Console.Write ("// Placing Object\n");
				var guo = new GraphUpdateObject(b);
				AstarPath.active.UpdateGraphs (guo);
				if (direct) {
					//Pathfinding.Console.Write ("// Flushing\n");
					AstarPath.active.FlushGraphUpdates();
				}
			}
		}
	}
	
	public void RemoveObject () {
		var ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if ( Physics.Raycast (ray, out hit, Mathf.Infinity)) {
			if (hit.collider.isTrigger || hit.transform.gameObject.name == "Ground") return;
			
			var b = hit.collider.bounds;
			Destroy (hit.collider);
			Destroy (hit.collider.gameObject);
			
			//Pathfinding.Console.Write ("// Placing Object\n");
			if (issueGUOs) {
				var guo = new GraphUpdateObject(b);
				AstarPath.active.UpdateGraphs (guo,0.0f);
				if (direct) {
					//Pathfinding.Console.Write ("// Flushing\n");
					AstarPath.active.FlushGraphUpdates();
				}
			}
		}
	}
}
