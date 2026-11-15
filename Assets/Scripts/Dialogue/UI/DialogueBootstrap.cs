using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    public sealed class DialogueBootstrap : MonoBehaviour
    {
        [SerializeField] private DialogueAsset _testDialogue;
        [SerializeField] private DialogueView _view;
        [SerializeField] private DialogueScreenService _screenService;
        [SerializeField] private Button _startButton;

        private DialogueRunner _runner;

        private void Start()
        {
            // Start выполняется после Awake компонентов сцены.
            var state = new GameState();
            var evaluator = new DialogueConditionEvaluator(state);
            var executor = new DialogueEffectExecutor(state, _screenService);

            _runner = new DialogueRunner(state, evaluator, executor);

            _view.Initialize(_runner);

            _runner.StateChanged += RefreshStartButton;
            _startButton.onClick.AddListener(HandleStartClicked);

            RefreshStartButton();
        }

        private void HandleStartClicked()
        {
            if (_runner == null ||
                _runner.IsActive ||
                _runner.IsBusy)
            {
                return;
            }

            try
            {
                _runner.StartDialogue(_testDialogue);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void RefreshStartButton()
        {
            if (_startButton != null)
            {
                _startButton.interactable =
                    _runner != null &&
                    !_runner.IsActive &&
                    !_runner.IsBusy &&
                    _testDialogue != null;
            }
        }

        private void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(
                    HandleStartClicked);
            }

            if (_runner != null)
            {
                _runner.StateChanged -= RefreshStartButton;
                _runner.CancelDialogue();
            }
        }
    }
}