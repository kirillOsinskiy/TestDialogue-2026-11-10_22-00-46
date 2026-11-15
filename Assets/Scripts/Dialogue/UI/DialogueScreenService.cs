using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    public sealed class DialogueScreenService :
        MonoBehaviour, IDialogueScreenService
    {
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private GameObject _tradePanel;
        [SerializeField] private Button _closeButton;

        private TaskCompletionSource<bool> _pending;
        private bool _destroyed;

        private void Awake()
        {
            _tradePanel.SetActive(false);
            _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        public async Task OpenAsync(
            string screenId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_destroyed)
            {
                throw new ObjectDisposedException(
                    nameof(DialogueScreenService));
            }

            if (!string.Equals(
                screenId, "trade", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Неизвестный экран: '{screenId}'.",
                    nameof(screenId));
            }

            if (_pending != null)
            {
                throw new InvalidOperationException(
                    "Экран уже открыт.");
            }

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _pending = completion;
            bool restoreDialogue = _dialoguePanel.activeSelf;

            try
            {
                _dialoguePanel.SetActive(false);
                _tradePanel.SetActive(true);

                // Callback не обращается к Unity API:
                // отмена может прийти с другого потока.
                using (cancellationToken.Register(
                    () => completion.TrySetCanceled()))
                {
                    await completion.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                _pending = null;

                // После await продолжаем на основном потоке Unity.
                if (!_destroyed)
                {
                    _tradePanel.SetActive(false);

                    if (_dialoguePanel != null)
                    {
                        _dialoguePanel.SetActive(restoreDialogue);
                    }
                }
            }
        }

        private void HandleCloseClicked()
        {
            _pending?.TrySetResult(true);
        }

        private void OnDestroy()
        {
            _destroyed = true;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(
                    HandleCloseClicked);
            }

            _pending?.TrySetCanceled();
        }
    }
}