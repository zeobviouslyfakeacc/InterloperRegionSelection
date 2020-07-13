using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;
using IL2CPP = Il2CppSystem.Collections.Generic;

namespace InterloperRegionSelection {
	internal class Patches {

		private static Panel_SelectRegion real_Panel_SelectRegion;
		private static Panel_SelectRegion fake_Panel_SelectRegion;
		private static bool overrideSceneToLoad = false;

		[HarmonyPatch(typeof(Panel_MainMenu), "RegionLockedBySelectedMode", new Type[0])]
		private static class UnlockInterloperRegionSelection {
			private static bool Prefix(ref bool __result) {
				if (ExperienceModeManager.GetCurrentExperienceModeType() == ExperienceModeType.Interloper) {
					__result = false;
					return false;
				} else {
					return true;
				}
			}
		}

		[HarmonyPatch(typeof(Panel_MainMenu), "OnSelectExperienceContinue", new Type[0])]
		private static class UseModifiedRegionSelectPanel {
			private static void Prefix() {
				if (ExperienceModeManager.GetCurrentExperienceModeType() == ExperienceModeType.Interloper) {
					InterfaceManager.m_Panel_SelectRegion = fake_Panel_SelectRegion;
					overrideSceneToLoad = true;
				} else {
					InterfaceManager.m_Panel_SelectRegion = real_Panel_SelectRegion;
					overrideSceneToLoad = false;
				}
			}
		}

		[HarmonyPatch(typeof(GameManager), "LoadSceneWithLoadingScreen")]
		private static class SetSceneToBeLoaded {
			private static void Prefix(ref string sceneName) {
				if (!overrideSceneToLoad) return;

				GameRegion startRegion = InterfaceManager.m_Panel_OptionsMenu.m_State.m_StartRegion;
				if (startRegion != GameRegion.RandomRegion && startRegion != GameRegion.FutureRegion) {
					sceneName = InterfaceManager.m_Panel_OptionsMenu.m_State.m_StartRegion.ToString();
				}
				overrideSceneToLoad = false;
			}
		}

		[HarmonyPatch(typeof(InterfaceManager), "Awake", new Type[0])]
		private static class CreateModifiedRegionSelectPanel {
			private static void Postfix(InterfaceManager __instance) {
				real_Panel_SelectRegion = InterfaceManager.m_Panel_SelectRegion;

				Transform camera = InterfaceManager.m_CommonGUI.transform.Find("Camera");
				Transform anchor = camera.Find("Anchor");
				MethodInfo instantiatePanel = AccessTools.Method(typeof(InterfaceManager), "InstantiatePanel");
				GameObject panel = (GameObject) instantiatePanel.Invoke(__instance, new object[] { "Panel_SelectRegion", anchor });
				fake_Panel_SelectRegion = panel.GetComponent<Panel_SelectRegion>();

				Modify(fake_Panel_SelectRegion);
			}

			private static void Modify(Panel_SelectRegion panel) {
				GameRegion[] interloperRegions = InterfaceManager.m_Panel_MainMenu.m_InterloperRegions;
				int len = interloperRegions.Length;

				List<GameRegion> regionOrder = new List<GameRegion>(len);
				List<GameObject> regionDescriptionsUnlocked = new List<GameObject>(len);
				IL2CPP.List<GameObject> regionScrollListItems = new IL2CPP.List<GameObject>(len);

				for (int i = 0; i < panel.m_RegionOrder.Length; ++i) {
					GameRegion region = panel.m_RegionOrder[i];
					if (interloperRegions.Contains(region) || region == GameRegion.RandomRegion) {
						regionOrder.Add(region);
						regionScrollListItems.Add(panel.m_RegionScrollListItems[i]);
						regionDescriptionsUnlocked.Add(panel.m_RegionDescriptionsUnlocked[i]);
					} else {
						UnityEngine.Object.Destroy(panel.m_RegionScrollListItems[i]);
						UnityEngine.Object.Destroy(panel.m_RegionDescriptionsUnlocked[i]);
					}
				}

				panel.m_RegionOrder = regionOrder.ToArray();
				panel.m_RegionDescriptionsUnlocked = regionDescriptionsUnlocked.ToArray();
				panel.m_RegionScrollListItems = regionScrollListItems;
			}
		}

		[HarmonyPatch(typeof(Panel_SelectRegion), "Enable", new Type[] { typeof(bool) })]
		private static class EnsureSomeRegionSelectedInFakePanel {

			private static void Prefix(Panel_SelectRegion __instance) {
				if (__instance != fake_Panel_SelectRegion)
					return;

				GameRegion regionLastPlayed = InterfaceManager.m_Panel_OptionsMenu.m_State.m_StartRegion;
				if (!__instance.m_RegionOrder.Contains(regionLastPlayed)) {
					// Make sure that *some* region is selected
					__instance.SelectRegion(0, false);
				}
			}
		}
	}
}
