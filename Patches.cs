using System;
using System.Linq;
using HarmonyLib;

namespace InterloperRegionSelection {
	internal class Patches {

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
					overrideSceneToLoad = true;
				} else {
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

		[HarmonyPatch(typeof(Panel_SelectRegion_Map), "Enable", new Type[] { typeof(bool) })]
		private static class EnsureOnlyInterloperRegionsSelectable {
			private static void Postfix(Panel_SelectRegion_Map __instance) {
				if (ExperienceModeManager.GetCurrentExperienceModeType() == ExperienceModeType.Interloper) {
					GameRegion[] interloperRegions = InterfaceManager.m_Panel_MainMenu.m_InterloperRegions;
					foreach (SelectRegionItem item in __instance.m_Items) {
						item.gameObject.SetActive(interloperRegions.Contains(item.m_Region));
					}
				} else {
					foreach (SelectRegionItem item in __instance.m_Items) {
						item.gameObject.SetActive(true);
					}
				}
			}
		}

		[HarmonyPatch(typeof(Panel_SelectRegion_Map), "SelectItem")]
		private static class DisableControllerSelectionOfInvalidInterloperRegions {
			private static bool Prefix(SelectRegionItem item) {
				return ExperienceModeManager.GetCurrentExperienceModeType() != ExperienceModeType.Interloper
					|| !item || InterfaceManager.m_Panel_MainMenu.m_InterloperRegions.Contains(item.m_Region);
			}
		}
	}
}
