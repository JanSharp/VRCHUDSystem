using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HUDTest : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;
        public Transform testImage;

        private void Start()
        {
            hudManager.AddHUDElement(testImage, "c[hud]-c[test]", isShown: true);
        }
    }
}
