using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [SingletonScript("f541c203388111c4685d5b0a00787c80")] // Runtime/Prefabs/HUDManager.prefab
    public abstract class HUDManagerAPI : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>This function changes the parent of <paramref name="elementTransform"/>.</para>
        /// <para>The HUD manager manges the active state of <paramref name="elementTransform"/>, pass this
        /// function an inactive game object.</para>
        /// </summary>
        /// <param name="elementTransform"></param>
        /// <param name="order"></param>
        /// <param name="isShown"></param>
        public abstract void AddHUDElement(Transform elementTransform, string order, bool isShown);
        /// <summary>
        /// <para>This function does not change the parent of <paramref name="elementTransform"/>, the calling
        /// function must do so afterwards.</para>
        /// <para>The HUD manager manges the active state of <paramref name="elementTransform"/>, this
        /// function will return an inactive game object.</para>
        /// </summary>
        /// <param name="elementTransform"></param>
        public abstract void RemoveHUDElement(Transform elementTransform);
        /// <summary>
        /// <para>Use this function rather than manually changing the active state of
        /// <paramref name="elementTransform"/>'s game object.</para>
        /// </summary>
        /// <param name="elementTransform"></param>
        public abstract void ShowHUDElement(Transform elementTransform);
        /// <summary>
        /// <para>Use this function rather than manually changing the active state of
        /// <paramref name="elementTransform"/>'s game object.</para>
        /// </summary>
        /// <param name="elementTransform"></param>
        public abstract void HideHUDElement(Transform elementTransform);
    }
}
