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
        public Transform vrRoot;
        public GameObject vrRootGo;
        public GameObject desktopCanvas;
        public Transform desktopPivot;
        public Transform hudCanvas;

        private bool isInitialized;
        private bool isInVR;

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
            isInVR = Networking.LocalPlayer.IsUserInVR();
            if (isInVR)
                InitializeVR();
            else
                InitializeDesktop();
        }

        private void InitializeVR()
        {
            Destroy(desktopCanvas);
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
                if (otherOrder.CompareTo(order) > 0)
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
            if (!isInVR)
                desktopCanvas.SetActive(true);
            else
            {
                boneAttachment.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, vrRoot);
                vrRoot.localPosition = Vector3.zero;
                vrRoot.localRotation = Quaternion.identity;
                vrRootGo.SetActive(true);
            }
        }

        private void HideHUD()
        {
            Initialize();
            if (!isInVR)
                desktopCanvas.SetActive(false);
            else
            {
                vrRootGo.SetActive(false);
                boneAttachment.DetachFromLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, vrRoot);
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
        }

        public override void RemoveHUDElement(Transform elementTransform)
        {
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
    }
}
