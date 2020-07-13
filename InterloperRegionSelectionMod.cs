using MelonLoader;
using UnityEngine;

namespace InterloperRegionSelection {
	internal class InterloperRegionSelectionMod : MelonMod {

		public override void OnApplicationStart() {
			Debug.Log($"[{InfoAttribute.Name}] Version {InfoAttribute.Version} loaded!");
		}
	}
}
