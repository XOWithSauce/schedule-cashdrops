using MelonLoader;
using System.Collections;
using UnityEngine;
using ScheduleOne.ObjectScripts.Cash;
using HarmonyLib;
using ScheduleOne.NPCs;
using ScheduleOne.Employees;
using ScheduleOne.Persistence;
using ScheduleOne.Money;

[assembly: MelonInfo(typeof(CashDrops.CashDrops), CashDrops.BuildInfo.Name, CashDrops.BuildInfo.Version, CashDrops.BuildInfo.Author, CashDrops.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace CashDrops
{
    public static class BuildInfo
    {
        public const string Name = "CashDrops";
        public const string Description = "NPC Cash Drops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.2";
        public const string DownloadLink = null;
    }

    public class CashDrops : MelonMod
    {
        public static List<object> coros = new();
        public static GameObject Visuals_Over100 = null;
        public static GameObject Visuals_Under100 = null;
        public static GameObject Bill = null;
        public static GameObject Note = null;
        private bool registered = false;
        private void OnLoadCompleteCb()
        {
            if (registered) return;
            coros.Add(MelonCoroutines.Start(this.Setup()));
            registered = true;
        }
        #region Unity
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                // MelonLogger.Msg("Start State");
                if (LoadManager.Instance != null && !registered)
                {
                    LoadManager.Instance.onLoadComplete.AddListener(OnLoadCompleteCb);
                }
            }
            else
            {
                registered = false;
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
        [HarmonyPatch(typeof(NPCHealth), "Die")]
        public static class NPC_Die_Patch
        {
            public static bool Prefix(NPCHealth __instance)
            {
                coros.Add(MelonCoroutines.Start(PreNPCKnockOut(__instance)));
                return true;
            }
        }
        #endregion
        private static IEnumerator PreNPCKnockOut(NPCHealth __instance)
        {
            yield return new WaitForSeconds(1f);
            NPC npc = __instance.GetComponent<NPC>();

            int npcStatus = 0;
            switch (npc.Region)
            {
                case ScheduleOne.Map.EMapRegion.Northtown:
                    npcStatus = 0;
                    break;

                case ScheduleOne.Map.EMapRegion.Westville:
                    npcStatus = 1;
                    break;

                case ScheduleOne.Map.EMapRegion.Downtown:
                    npcStatus = 2;
                    break;

                case ScheduleOne.Map.EMapRegion.Docks:
                    npcStatus = 3;
                    break;

                case ScheduleOne.Map.EMapRegion.Suburbia:
                    npcStatus = 4;
                    break;

                case ScheduleOne.Map.EMapRegion.Uptown:
                    npcStatus = 5;
                    break;

                default:
                    break;
            }

            if (npc != null && !(npc is Employee))
            {
                Vector3 topNpc = new(__instance.transform.position.x, __instance.transform.position.y + 1f, __instance.transform.position.z);
                int leastMaxRoll = 3 + npcStatus;
                int roll = UnityEngine.Random.Range(npcStatus, leastMaxRoll);
                switch (roll)
                {
                    case 0:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Note, 1, 2, 1));
                        break;

                    case 1:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Note, 1, 3, 1));
                        break;

                    case 2:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Note, 4, 8, 1));
                        break;

                    case 3:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Note, 10, 20, 1));
                        break;

                    case 4:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Bill, 1, 3, 2));
                        break;

                    case 5:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Bill, 3, 6, 2));
                        break;

                    case 6:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Visuals_Under100, 1, 4, 3));
                        break;

                    case 7:
                        MelonCoroutines.Start(CashSpawnRoutine(topNpc, CashDrops.Visuals_Over100, 1, 3, 4));
                        break;

                    default:
                        break;
                }
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
        private static IEnumerator CashPickupDelay(GameObject go, int value)
        {
            yield return new WaitForSeconds(2f);
            CashPickup pickupComp = go.GetComponent<CashPickup>();
            if (pickupComp != null)
                pickupComp.SetupPickup(value);
            //MelonLogger.Msg("Added collision handler");

        }
        private static IEnumerator CashSpawnRoutine(Vector3 pos, GameObject baseObj, int lowerAmnt, int upperAmnt, int value)
        {
            GameObject bunchBase = new GameObject("CashBunchBase");
            for (int i = 0; i < UnityEngine.Random.Range(lowerAmnt, upperAmnt); i++)
            {
                //MelonLogger.Msg("Spawning Cash");
                yield return new WaitForSeconds(0.05f);
                GameObject go = GameObject.Instantiate(baseObj, pos, Quaternion.identity, bunchBase.transform);
                if (!go.activeInHierarchy)
                    go.SetActive(true);
                Rigidbody rb = go.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 force = Vector3.up * 6f;
                    rb.AddForce(force, ForceMode.Impulse);
                    yield return new WaitForSeconds(0.1f);
                    if (UnityEngine.Random.Range(0, 100) >= 50)
                        force = Vector3.right;
                    else
                        force = Vector3.left;

                    rb.AddForce(force, ForceMode.Impulse);

                    float torqueX = UnityEngine.Random.Range(-5f, 5f);
                    float torqueZ = UnityEngine.Random.Range(-5f, 5f);
                    rb.AddTorque(new Vector3(torqueX, 0f, torqueZ), ForceMode.Impulse);
                    MelonCoroutines.Start(CashPickupDelay(go, value));
                }
            }
            MelonCoroutines.Start(CashDespawnHandler(bunchBase));
        }

        public class CashCollisionHandler : MonoBehaviour
        {
            public int value;
            private void OnTriggerEnter(Collider collision)
            {
                GameObject other = collision.gameObject;
                int otherLayer = other.layer;

                if (otherLayer != 6) return;

                float amountToAdd;
                switch (value)
                {
                    case 1:
                        if (UnityEngine.Random.Range(0, 100) > 80)
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(10f, 40f));
                        else
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(1f, 20f));
                        break;

                    case 2:
                        if (UnityEngine.Random.Range(0, 100) > 80)
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(30f, 60f));
                        else
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(10f, 40f));
                        break;

                    case 3:
                        if (UnityEngine.Random.Range(0, 100) > 80)
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(60f, 100f));
                        else
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(30f, 60f));
                        break;

                    case 4:
                        if (UnityEngine.Random.Range(0, 100) > 80)
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(100f, 200f));
                        else
                            amountToAdd = Mathf.Round(UnityEngine.Random.Range(60f, 100f));
                        break;

                    default:
                        amountToAdd = 1f;
                        break;
                }

                MoneyManager.Instance.ChangeCashBalance(amountToAdd, true, false);
                Destroy(this.transform.parent.gameObject);
            }
        }
        public class CashPickup : MonoBehaviour
        {
            public void SetupPickup(int value)
            {
                //MelonLogger.Msg("SetupPickup");
                GameObject triggerChild = new GameObject("CashPickupTrigger");
                triggerChild.transform.SetParent(this.transform);
                triggerChild.transform.localPosition = Vector3.zero;

                BoxCollider trigger = triggerChild.AddComponent<BoxCollider>();
                trigger.size = new Vector3(0.5f, 0.5f, 0.5f);
                trigger.isTrigger = true;

                CashCollisionHandler cch = triggerChild.AddComponent<CashCollisionHandler>();
                cch.value = value;
            }
        }
        private bool IsAssignedObjects()
        {
            return CashDrops.Note != null && CashDrops.Bill != null && CashDrops.Visuals_Under100 != null && CashDrops.Visuals_Over100 != null;
        }
        private IEnumerator Setup()
        {
            if (IsAssignedObjects()) { yield break; }

            yield return new WaitForSeconds(10f);
            CashStackVisuals[] csv = UnityEngine.Object.FindObjectsOfType<CashStackVisuals>(true);
            if (csv.Length > 0)
            {
                foreach (CashStackVisuals inst in csv)
                {
                    yield return new WaitForSeconds(0.3f);
                    if (CashDrops.Visuals_Over100 == null && inst.Visuals_Over100)
                    {
                        CashDrops.Visuals_Over100 = GameObject.Instantiate(inst.Visuals_Over100.gameObject, Vector3.zero, Quaternion.identity, null);
                        CashDrops.Visuals_Over100.SetActive(false);
                        PrepareObject(CashDrops.Visuals_Over100);
                        //MelonLogger.Msg("Over100 assigned");

                    }
                    if (CashDrops.Visuals_Under100 == null && inst.Visuals_Under100)
                    {
                        CashDrops.Visuals_Under100 = GameObject.Instantiate(inst.Visuals_Under100.gameObject, Vector3.zero, Quaternion.identity, null);
                        CashDrops.Visuals_Under100.SetActive(false);
                        PrepareObject(CashDrops.Visuals_Under100);
                        //MelonLogger.Msg("Under100 assigned");

                    }
                    if (CashDrops.Bill == null && inst.Bills.FirstOrDefault())
                    {
                        CashDrops.Bill = GameObject.Instantiate(inst.Bills.FirstOrDefault().gameObject, Vector3.zero, Quaternion.identity, null);
                        CashDrops.Bill.SetActive(false);
                        PrepareObject(CashDrops.Bill);
                        //MelonLogger.Msg("Bill assigned");
                    }
                    if (CashDrops.Note == null && inst.Notes.FirstOrDefault())
                    {
                        CashDrops.Note = GameObject.Instantiate(inst.Notes.FirstOrDefault().gameObject, Vector3.zero, Quaternion.identity, null);
                        CashDrops.Note.SetActive(false);
                        PrepareObject(CashDrops.Note);
                        //MelonLogger.Msg("Note assigned");
                    }

                    if (IsAssignedObjects()) { yield break; }
                }
            }


            yield return null;
        }
        private void PrepareObject(GameObject go)
        {
            CashPickup cashPickupComponent = go.AddComponent<CashPickup>();
            BoxCollider box;
            Rigidbody rb;
            if (!go.TryGetComponent<BoxCollider>(out box))
            {
                box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(0.2f, 0.2f, 0.2f);
            }
            if (!go.TryGetComponent<Rigidbody>(out rb))
            {
                rb = go.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.excludeLayers = 6;
            }
        }
        #endregion
    }
}
