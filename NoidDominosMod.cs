using Pepperoni;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using NoidDominos.Properties;
using Resources = NoidDominos.Properties.Resources;

namespace NoidDominos
{
    public class NoidDominos : Mod
    {
        const string _modVersion = "1.0.0";
        public NoidDominos() : base("NoidDominos")
        {
        }

        public override string GetVersion() => _modVersion;

        private readonly Vector3 npcPos = new Vector3(789.7f, 63.1f, 453.2f);

        public override void Initialize()
        {
            GameObject go = new GameObject();
            go.AddComponent<OrderUI>();
            Object.DontDestroyOnLoad(go);

            SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnSceneChange(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "void" &&
                (SaveScript.isLevelDone("LeviLevle") || SaveScript.isLevelDone("dungeon") || SaveScript.isLevelDone("PZNTv5")))
            {
                Basic_NPC pizzaNPC = null;
                var npcs = Object.FindObjectsOfType<Basic_NPC>();

                foreach (var npc in npcs)
                {
                    var textAsset = npc.GetComponentInChildren<TalkVolume>().Dialogue;
                    if (DialogueUtils.GetNPCName(textAsset.text) == "Oleia")
                    {
                        pizzaNPC = npc;
                        break;
                    }
                }

                if (pizzaNPC != null)
                {
                    LogDebug("Found Oleia (Birb)!");
                    Object.Instantiate(pizzaNPC, npcPos, Quaternion.identity);
                }
            }
        }
    }
}
