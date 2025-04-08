using MelonLoader;
using System.Collections;
using ScheduleOne.PlayerScripts;
using UnityEngine;
using ScheduleOne.ObjectScripts.Cash;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using HarmonyLib;
using System.Reflection;
using ScheduleOne.NPCs;
using ScheduleOne.Employees;

[assembly: MelonInfo(typeof(CashDrops.CashDrops), CashDrops.BuildInfo.Name, CashDrops.BuildInfo.Version, CashDrops.BuildInfo.Author, CashDrops.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace CashDrops
{
    public static class BuildInfo
    {
        public const string Name = "CashDrops";
        public const string Description = "NPC Cash Drops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.0";
        public const string DownloadLink = null;
    }

    public class CashDrops : MelonMod
    {
        private static HarmonyLib.Harmony harmonyInstance;
        public static List<object> coros = new();
        public static GameObject basePrefab;

        #region Unity
        public override void OnApplicationStart()
        {
            harmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            MelonLogger.Msg("NPC Cash Drops enabled");

        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                // MelonLogger.Msg("Start State");
                coros.Add(MelonCoroutines.Start(this.Setup()));
            }
            else
            {
                // MelonLogger.Msg("Clear State");
                foreach (object coro in coros)
                {
                    MelonCoroutines.Stop(coro);
                }
                coros.Clear();
            }
        }

        #region Harmony
        [HarmonyPatch(typeof(NPCHealth), "KnockOut")]
        public static class NPC_KnockOut_Patch
        {
            public static bool Prefix(NPCHealth __instance)
            {
                coros.Add(MelonCoroutines.Start(PreNPCKnockOut(__instance)));
                return true;
            }
        }

        private static IEnumerator PreNPCKnockOut(NPCHealth __instance)
        {
            yield return new WaitForSeconds(2f);
            NPC npc = __instance.GetComponent<NPC>();
            if (npc != null && !(npc is Employee))
            {
                Vector3 topNpc = new(__instance.transform.position.x, __instance.transform.position.y + 2f, __instance.transform.position.z);
                GameObject go = GameObject.Instantiate(CashDrops.basePrefab, topNpc, Quaternion.identity);

                if (!go.activeInHierarchy)
                    go.SetActive(true);

                MelonCoroutines.Start(CashDespawnHandler(go));
            } else
            {
                //MelonLogger.Msg("EmployeeNPC");
            }

            yield return null;
        }
        private static IEnumerator CashDespawnHandler(GameObject go)
        {
            yield return new WaitForSeconds(60f);
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }

            yield return null;
        }
        #endregion

        public class CashCollisionHandler : MonoBehaviour
        {

            private void OnTriggerEnter(Collider collision)
            {
                GameObject other = collision.gameObject;
                int otherLayer = other.layer;

                if (otherLayer != 6) return;

                Player player = other.GetComponentInParent<Player>();
                if (player == null)
                {
                    return;
                }

                var inventory = PlayerSingleton<PlayerInventory>.Instance;
                if (inventory == null)
                {
                    return;
                }

                CashInstance cashInstance = inventory.cashInstance;

                if (cashInstance == null)
                {
                    return;
                }

                float amountToAdd;
                if (UnityEngine.Random.Range(0, 100) > 80)
                    amountToAdd = Mathf.Round(UnityEngine.Random.Range(200f, 1000f));
                else
                    amountToAdd = Mathf.Round(UnityEngine.Random.Range(25f, 200f));

                cashInstance.ChangeBalance(amountToAdd);

                Destroy(this.transform.parent.gameObject);
            }
        }

        public class CashPickup : MonoBehaviour
        {
            public void SetupPickup()
            {
                GameObject triggerChild = new GameObject("CashPickupTrigger");
                triggerChild.transform.SetParent(this.transform);
                triggerChild.transform.localPosition = Vector3.zero;

                BoxCollider trigger = triggerChild.AddComponent<BoxCollider>();
                trigger.size = new Vector3(0.5f, 0.5f, 0.5f);
                trigger.isTrigger = true;

                triggerChild.AddComponent<CashCollisionHandler>();
            }
        }

        private IEnumerator Setup()
        {
            yield return new WaitForSeconds(10f);
            CashStackVisuals[] csv = UnityEngine.Object.FindObjectsOfType<CashStackVisuals>(true);
            if (csv.Length > 0)
            {
                foreach (CashStackVisuals inst in csv)
                {
                    yield return new WaitForSeconds(1f);
                    if (inst.Visuals_Over100)
                    {
                        CashDrops.basePrefab = inst.Visuals_Over100;
                        break;
                    }
                }
            }

            if (!CashDrops.basePrefab) { yield return null; }

            CashPickup cashPickupComponent = CashDrops.basePrefab.AddComponent<CashPickup>();
            cashPickupComponent.SetupPickup();
            BoxCollider box;
            Rigidbody rb;
            if (!CashDrops.basePrefab.TryGetComponent<BoxCollider>(out box))
            {
                box = CashDrops.basePrefab.AddComponent<BoxCollider>();
                box.size = new Vector3(0.3f, 0.3f, 0.3f);
            }
            if (!CashDrops.basePrefab.TryGetComponent<Rigidbody>(out rb))
            {
                rb = CashDrops.basePrefab.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }

            yield return null;
        }
        #endregion

        
    }
}
