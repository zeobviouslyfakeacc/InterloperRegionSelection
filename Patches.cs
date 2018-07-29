using System;
using Old = System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace InterloperRegionSelection {
	internal class Patches {

		private static Panel_SelectRegion real_Panel_SelectRegion;
		private static Panel_SelectRegion fake_Panel_SelectRegion;

		[HarmonyPatch(typeof(Panel_MainMenu), "RegionLockedBySelectedMode", new Type[0])]
		private static class UnlockInterloperRegionSelection {
			private static bool Prefix(ref bool __result) {
				if (GameManager.GetExperienceModeManagerComponent().GetCurrentExperienceModeType() == ExperienceModeType.Interloper) {
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
				if (GameManager.GetExperienceModeManagerComponent().GetCurrentExperienceModeType() == ExperienceModeType.Interloper) {
					InterfaceManager.m_Panel_SelectRegion = fake_Panel_SelectRegion;
				} else {
					InterfaceManager.m_Panel_SelectRegion = real_Panel_SelectRegion;
				}
			}
		}

		[HarmonyPatch(typeof(Panel_MainMenu), "OnSandboxFinal", new Type[0])]
		private static class UseSelectedInterloperRegion {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr) {
				List<CodeInstruction> instructions = new List<CodeInstruction>(instr);
				MethodInfo method = AccessTools.Method(typeof(ExperienceModeManager), "GetCurrentExperienceModeType");
				int count = 0;

				// Add && gameRegion == GameRegion.RandomRegion to the second
				// GameManager.GetExperienceModeManagerComponent().GetCurrentExperienceModeType() == ExperienceModeType.Interloper
				// check that would reset the spawnRegion string to a random interloper region

				yield return instructions[0];
				for (int i = 1; i < instructions.Count; ++i) {
					CodeInstruction last = instructions[i - 1];
					CodeInstruction curr = instructions[i];
					yield return curr;

					if (last.opcode == OpCodes.Callvirt && last.operand == method
						&& curr.opcode == OpCodes.Ldc_I4_S && (sbyte) curr.operand == (sbyte) ExperienceModeType.Interloper) {
						if (++count == 2) {
							CodeInstruction branch = instructions[i + 1];
							yield return branch;
							yield return new CodeInstruction(OpCodes.Ldloc_1);
							yield return new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte) GameRegion.RandomRegion);
						}
					}
				}
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
				List<GameObject> regionScrollListItems = new List<GameObject>(len);
				List<GameObject> regionDescriptionsLocked = new List<GameObject>(len);
				List<GameObject> regionDescriptionsUnlocked = new List<GameObject>(len);

				for (int i = 0; i < panel.m_RegionOrder.Length; ++i) {
					GameRegion region = panel.m_RegionOrder[i];
					if (interloperRegions.Contains(region) || region == GameRegion.RandomRegion) {
						regionOrder.Add(region);
						regionScrollListItems.Add(panel.m_RegionScrollListItems[i]);
						regionDescriptionsLocked.Add(panel.m_RegionDescriptionsLocked[i]);
						regionDescriptionsUnlocked.Add(panel.m_RegionDescriptionsUnlocked[i]);
					} else {
						UnityEngine.Object.Destroy(panel.m_RegionScrollListItems[i]);
						UnityEngine.Object.Destroy(panel.m_RegionDescriptionsLocked[i]);
						UnityEngine.Object.Destroy(panel.m_RegionDescriptionsUnlocked[i]);
					}
				}

				panel.m_RegionOrder = regionOrder.ToArray();
				panel.m_RegionScrollListItems = regionScrollListItems;
				panel.m_RegionDescriptionsLocked = regionDescriptionsLocked.ToArray();
				panel.m_RegionDescriptionsUnlocked = regionDescriptionsUnlocked.ToArray();
			}
		}

		[HarmonyPatch(typeof(Panel_SelectRegion), "Start", new Type[0])]
		private static class FixDelegatesList {
			private static void Postfix(Panel_SelectRegion __instance) {
				Old.IList delegates = (Old.IList) AccessTools.Field(typeof(Panel_SelectRegion), "m_RegionLockedDelegates").GetValue(__instance);
				Type delegateType = AccessTools.Inner(typeof(Panel_SelectRegion), "OnBoolMethod");
				object returnFalse = Delegate.CreateDelegate(delegateType, AccessTools.Method(typeof(FixDelegatesList), "ReturnFalse"));

				delegates.Clear();
				for (int i = 0; i < __instance.m_RegionOrder.Length; ++i) {
					delegates.Add(returnFalse);
				}
			}

			private static bool ReturnFalse() => false;
		}

		[HarmonyPatch(typeof(Panel_SelectRegion), "Enable", new Type[] { typeof(bool) })]
		private static class PreventDelegatesListChange {

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr) {
				List<CodeInstruction> instructions = new List<CodeInstruction>(instr);

				int i = 0;
				// Take until m_RegionLockedDelegates.Clear();
				for (; i < instructions.Count - 2; ++i) {
					CodeInstruction op = instructions[i + 2];
					if (op.opcode == OpCodes.Callvirt && ((MethodInfo) op.operand).Name == "Clear") {
						break;
					}
					yield return instructions[i];
				}
				// Skip until m_RegionLockedDelegates.AddRange(...);
				for (; i < instructions.Count; ++i) {
					CodeInstruction op = instructions[i];
					if (op.opcode == OpCodes.Callvirt && ((MethodInfo) op.operand).Name == "AddRange") {
						++i;
						break;
					}
				}
				// Take the rest
				for (; i < instructions.Count; ++i) {
					yield return instructions[i];
				}
			}
		}
	}
}
