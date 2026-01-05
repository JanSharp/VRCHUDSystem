using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HUDManager : HUDManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private BoneAttachmentManager boneAttachment;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;
        public Transform hudCanvas;
        public Transform vrRoot;
        public GameObject vrRootGo;
        public Transform vrScaleTransform;
        private Vector3 defaultVRScaleTransformPosition;
        private Vector3 defaultVRScaleTransformScale;
        public GameObject desktopCanvasGo;
        public RectTransform desktopCanvasRect;
        public Transform desktopPivot;
        public Transform desktopScaleTransform;
        private float prevScreenHeight = -1f;

        private const float VREyeHeightScaleMultiplier = 0.5f;
        private const float DesktopScreenHeightScaleMultiplier = 1f / 1080f;

        private bool isInitialized;
        private bool isInVR;
        private VRCPlayerApi localPlayer;

        private const int ElementIndex = 0;
        private const int ElementTransform = 1;
        private const int ElementOrder = 2;
        private const int ElementIsShown = 3;
        private const int ElementSize = 4;

        private object[][] elements = new object[ArrList.MinCapacity][];
        private int elementsCount = 0;
        /// <summary>
        /// <para><see cref="Transform"/> => <see cref="object[]"/></para>
        /// </summary>
        private DataDictionary elementsLut = new DataDictionary();
        private int shownCount = 0;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();
            if (isInVR)
                InitializeVR();
            else
                InitializeDesktop();
        }

        private void InitializeVR()
        {
            Destroy(desktopCanvasGo);
            defaultVRScaleTransformPosition = vrScaleTransform.localPosition;
            defaultVRScaleTransformScale = vrScaleTransform.localScale;
        }

        private void InitializeDesktop()
        {
            hudCanvas.SetParent(desktopPivot, worldPositionStays: false);
            Destroy(vrRootGo);
        }

        private int InsertSort(object[] element, string order)
        {
            elementsCount++;
            ArrList.EnsureCapacity(ref elements, elementsCount);
            int index = elementsCount - 2;
            while (index >= 0)
            {
                object[] other = elements[index];
                string otherOrder = (string)other[ElementOrder];
                if (otherOrder.CompareTo(order) <= 0)
                    break;
                other[ElementIndex] = index + 1;
                elements[index + 1] = other;
                index--;
            }
            index++;
            element[ElementIndex] = index;
            elements[index] = element;
            return index;
        }

        private void ShowHUD()
        {
            Initialize();
            if (isInVR)
            {
                boneAttachment.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, vrRoot);
                vrRoot.localPosition = Vector3.zero;
                vrRoot.localRotation = Quaternion.identity;
                vrRootGo.SetActive(true);
            }
            else
            {
                updateManager.Register(this);
                CustomUpdate();
                desktopCanvasGo.SetActive(true);
            }
        }

        private void HideHUD()
        {
            Initialize();
            if (isInVR)
            {
                vrRootGo.SetActive(false);
                boneAttachment.DetachFromLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, vrRoot);
            }
            else
            {
                updateManager.Deregister(this);
                desktopCanvasGo.SetActive(false);
            }
        }

        private void ShowElement(Transform elementTransform)
        {
            elementTransform.gameObject.SetActive(true);
            if (++shownCount == 1)
                ShowHUD();
        }

        private void HideElement(Transform elementTransform)
        {
            if (--shownCount == 0)
                HideHUD();
            elementTransform.gameObject.SetActive(false);
        }

        public override void AddHUDElement(Transform elementTransform, string order, bool isShown)
        {
#if HUD_SYSTEM_DEBUG
            Debug.Log($"[HUDSystemDebug] HUDManager  AddHUDElement - elementTransform.name: {elementTransform.name}");
#endif
            if (elementsLut.ContainsKey(elementTransform))
            {
                Debug.LogError($"[HUDSystem] Attempt to add the {elementTransform.name} hud element twice.");
                return;
            }
            object[] element = new object[ElementSize];
            elementsLut.Add(elementTransform, new DataToken(element));
            element[ElementTransform] = elementTransform;
            element[ElementOrder] = order;
            element[ElementIsShown] = isShown;
            int index = InsertSort(element, order);
            elementTransform.SetParent(hudCanvas, worldPositionStays: false);
            elementTransform.SetSiblingIndex(index);
            if (isShown)
                ShowElement(elementTransform);
            else
                elementTransform.gameObject.SetActive(false);
        }

        public override void RemoveHUDElement(Transform elementTransform)
        {
#if HUD_SYSTEM_DEBUG
            Debug.Log($"[HUDSystemDebug] HUDManager  RemoveHUDElement - elementTransform.name: {elementTransform.name}");
#endif
            if (!elementsLut.Remove(elementTransform, out DataToken elementToken))
            {
                Debug.LogError($"[HUDSystem] Attempt to remove the {elementTransform.name} hud element which was not added prior.");
                return;
            }
            object[] element = (object[])elementToken.Reference;
            int index = (int)element[ElementIndex];
            ArrList.RemoveAt(ref elements, ref elementsCount, index);
            elements[elementsCount] = null; // Free memory.
            for (int i = index; i < elementsCount; i++)
                elements[i][ElementIndex] = i;
            if ((bool)element[ElementIsShown])
                HideElement(elementTransform);
        }

        public override void ShowHUDElement(Transform elementTransform)
        {
#if HUD_SYSTEM_DEBUG
            Debug.Log($"[HUDSystemDebug] HUDManager  ShowHUDElement - elementTransform.name: {elementTransform.name}");
#endif
            if (!elementsLut.TryGetValue(elementTransform, out DataToken elementToken))
            {
                Debug.LogError($"[HUDSystem] Attempt to show the {elementTransform.name} hud element which was not added prior.");
                return;
            }
            object[] element = (object[])elementToken.Reference;
            if ((bool)element[ElementIsShown])
                return;
            element[ElementIsShown] = true;
            ShowElement(elementTransform);
        }

        public override void HideHUDElement(Transform elementTransform)
        {
#if HUD_SYSTEM_DEBUG
            Debug.Log($"[HUDSystemDebug] HUDManager  HideHUDElement - elementTransform.name: {elementTransform.name}");
#endif
            if (!elementsLut.TryGetValue(elementTransform, out DataToken elementToken))
            {
                Debug.LogError($"[HUDSystem] Attempt to hide the {elementTransform.name} hud element which was not added prior.");
                return;
            }
            object[] element = (object[])elementToken.Reference;
            if (!(bool)element[ElementIsShown])
                return;
            element[ElementIsShown] = false;
            HideElement(elementTransform);
        }

        public override void OnAvatarChanged(VRCPlayerApi player)
        {
            if (player.isLocal)
                UpdateVRScale();
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (player.isLocal)
                UpdateVRScale();
        }

        private void UpdateVRScale()
        {
            Initialize();
            if (!isInVR)
                return;
            float eyeHeight = localPlayer.GetAvatarEyeHeightAsMeters();
            float scale = eyeHeight * VREyeHeightScaleMultiplier;
            // Cannot change the scale of the parent of this transform, for some reason it only ends up
            // affecting the position while the sizing (scale) does not propagate through to the canvas.
            // So we have to calculate the position and scale like this.
            vrScaleTransform.localPosition = defaultVRScaleTransformPosition * scale;
            vrScaleTransform.localScale = defaultVRScaleTransformScale * scale;
        }

        public void CustomUpdate()
        {
            float height = desktopCanvasRect.sizeDelta.y;
            if (prevScreenHeight == height)
                return;
            prevScreenHeight = height;
            desktopScaleTransform.localScale = Vector3.one * (height * DesktopScreenHeightScaleMultiplier);
        }
    }
}
