using System;
using UnityEngine;

namespace AnchorDefense
{
    public enum GameState
    {
        Playing,
        GameOver
    }

    public sealed class GameFlowController : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;
        public event Action<GameState> StateChanged;

        public void BeginGame()
        {
            Time.timeScale = 1f;
            State = GameState.Playing;
            StateChanged?.Invoke(State);
        }

        public void EndGame()
        {
            if (!IsPlaying)
            {
                return;
            }

            State = GameState.GameOver;
            StateChanged?.Invoke(State);
            Time.timeScale = 0f;
        }
    }
}
