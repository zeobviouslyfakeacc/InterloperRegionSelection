using MelonLoader;
using UnityEngine;

namespace InterloperRegionSelection {
	internal class InterloperRegionSelectionMod : MelonMod {

		public override void OnInitializeMelon() {
			Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
		}
	}
}
