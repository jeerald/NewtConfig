using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
namespace xpcybic
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.xpcybic.NewtConfig", "NewtConfig", "1.0.0")]
    public class NewtConfig : BaseUnityPlugin
    {
        public static ConfigEntry<float> NewtAltarChance;
        public static ConfigEntry<float> BazaarPortalChance;
        public static ConfigEntry<bool> EnableGuaranteedNewtAltars;
        public static ConfigEntry<bool> ScalePortalChanceOnPortalSpawn;

        public void Awake()
        {
            SetUpConfig();

            On.RoR2.SceneDirector.Start += SceneDirector_Start;
            On.RoR2.TeleporterInteraction.Start += TeleporterInteraction_Start;
        }

        private void SetUpConfig()
        {
            NewtAltarChance = Config.Bind("Main", "Newt altar chance", 0.5f,
                "Chance of each individual newt altar spawning on a stage. If set to 1, every newt altar will always spawn. If set to 0, newt altars will never spawn (except the guaranteed ones, which are handled separately).");
            BazaarPortalChance = Config.Bind("Main", "Bazaar portal chance", 0.375f,
                "Chance of a bazaar portal naturally appearing after a teleporter has finished charging. If set to 1, every teleporter will spawn a bazaar portal. If set to 0, the bazaar portal will never appear unles a newt altar is activated.");
            EnableGuaranteedNewtAltars = Config.Bind("Main", "Enable guaranteed newt altars", true,
                "Some stages have newt altars which will always spawn (e.g. under the giant skull on Abandoned Aqueduct). Set this to false to prevent these from spawning.");
            ScalePortalChanceOnPortalSpawn = Config.Bind("Main", "Scale portal chance on portal spawn", true,
                "By default, the chance of a blue orb/bazaar portal naturally spawning is halved every time a bazaar portal spawns. Set this to false to never reduce the portal spawn chance.");
        }

        private void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector director)
        {
            orig(director);

            if (NetworkServer.active && SceneInfo.instance.sceneDef.baseSceneName != "bazaar")
            {
                //sky meadow and a couple other stages name them like this. we grab them by exact name so we don't accidentally disable an object we don't want to
                var newts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue" || obj.name == "NewtStatue (1)" || obj.name == "NewtStatue (2)" || obj.name == "NewtStatue (3)" || obj.name == "NewtStatue (4)").ToList();
                foreach (var newt in newts)
                {
                    if (NewtAltarChance.Value >= 1 || director.rng.nextNormalizedFloat <= NewtAltarChance.Value)
                        newt.SetActive(true);
                    else
                        newt.SetActive(false);
                }

                if (!EnableGuaranteedNewtAltars.Value)
                {
                    //naming conventions??? NAHH
                    var guaranteedNewts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue, Guarantee" || obj.name == "NewtStatue, Guaranteed" || obj.name == "NewtStatue (Permanent)").ToList();
                    foreach (var newt in guaranteedNewts)
                        newt.SetActive(false);
                }
            }
        }

        private void TeleporterInteraction_Start(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction tele)
        {
            orig(tele);

            if (NetworkServer.active)
            {
                float portalChance = BazaarPortalChance.Value;
                if (ScalePortalChanceOnPortalSpawn.Value)
                    portalChance /= (float)(Run.instance.shopPortalCount + 1);

                if (tele.rng.nextNormalizedFloat <= portalChance)
                    tele.shouldAttemptToSpawnShopPortal = true;
                else
                    tele.shouldAttemptToSpawnShopPortal = false;
            }
        }
    }
}
