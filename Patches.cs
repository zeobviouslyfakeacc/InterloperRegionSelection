using HarmonyLib;
using Il2Cpp;

namespace InterloperRegionSelection
{
	internal class Patches {
		[HarmonyPatch(typeof(Panel_SelectExperience), nameof(Panel_SelectExperience.Enable), new Type[] { typeof(bool) })]
		private static class InterlopersCanChooseWhereTheyInterlope {
			private static void Postfix(Panel_SelectExperience __instance) {
				foreach (var item in __instance.m_MenuItems) {
					if (item.m_SandboxConfig.m_XPMode.m_ModeType == ExperienceModeType.Interloper) {
						item.m_SandboxConfig.m_StartRegionSelectionBlocked = false;
					}
				}
			}
		}
	}
}
