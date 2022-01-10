using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Scripts.UserInterface
{
    public class DisableSelectableOnDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField]
        private Selectable _selectable;
        
        private void Awake()
        {
            Debug.Assert(_selectable);
        }

        private void OnEnable()
        {
            _selectable.interactable = true;
        }

        public void OnBeginDrag(PointerEventData _)
        {
            _selectable.interactable = false;
        }

        public void OnEndDrag(PointerEventData _)
        {
            _selectable.interactable = true;
        }
    }
}