using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Dialogue
{
    public class DialogueChoiceButtonView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private TMP_Text _numberText;
        [SerializeField] private TMP_Text _choiceText;
        [SerializeField] private GameObject _selectionMarker;
        [SerializeField] private Button _button;

        private DialogueChoice _choice;
        private Action<DialogueChoice> _onClicked;

        private bool _hovered;
        private bool _selected;

        public void Bind(
            DialogueChoice choice,
            int number,
            Action<DialogueChoice> onClicked)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            if (onClicked == null)
            {
                throw new ArgumentNullException(nameof(onClicked));
            }

            _choice = choice;
            _onClicked = onClicked;

            _numberText.text = number.ToString();
            _choiceText.text = choice.Text ?? string.Empty;

            _hovered = false;
            _selected = false;
            RefreshMarker();

            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
            RefreshMarker();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshMarker();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshMarker();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            RefreshMarker();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetSelected(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetSelected(false);
        }

        private void RefreshMarker()
        {
            if (_selectionMarker == null)
            {
                return;
            }

            _selectionMarker.SetActive(
                isActiveAndEnabled &&
                _button != null &&
                _button.IsInteractable() &&
                (_hovered || _selected));
        }

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;

            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(false);
            }
        }

        private void HandleClicked()
        {
            if (_choice != null && _button.IsInteractable())
            {
                _onClicked?.Invoke(_choice);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }
    }
}