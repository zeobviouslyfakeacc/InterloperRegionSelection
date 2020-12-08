using MelonLoader;
using UnityEngine;

namespace InterloperRegionSelection {
	internal class InterloperRegionSelectionMod : MelonMod {

		public override void OnApplicationStart() {
			Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
		}
	}
}
