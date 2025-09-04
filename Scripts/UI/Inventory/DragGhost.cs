using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xianxia.UI.Inventory
{
    public class DragGhost : MonoBehaviour
    {
        private Image _image;

        public static DragGhost Create(Canvas topCanvas)
        {
            var go = new GameObject("DragGhost");
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            go.transform.SetParent(topCanvas != null ? topCanvas.transform : null, worldPositionStays: false);
            var ghost = go.AddComponent<DragGhost>();
            ghost._image = img;
            ghost.gameObject.SetActive(false);
            return ghost;
        }

        public void Show(Sprite s, Vector2 screenPos)
        {
            _image.sprite = s;
            _image.color = new Color(1, 1, 1, 0.9f);
            _image.enabled = s != null;
            gameObject.SetActive(true);
            Move(screenPos);
        }

        public void Move(Vector2 screenPos)
        {
            var rt = (RectTransform)transform;
            rt.position = screenPos;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _image.sprite = null;
        }

        public static List<RaycastResult> RaycastUI(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(eventData, results);
            return results;
        }
    }
}