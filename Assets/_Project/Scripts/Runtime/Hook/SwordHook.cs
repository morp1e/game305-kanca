using System;
using Game.Core;
using Game.Input;
using Game.Player;
using UnityEngine;

namespace Game.Hook
{
    /// <summary>
    /// Kılıç-kanca durum makinesi. SÖZLEŞME İSKELETİ: gövde Lane B tarafından yazılacak.
    /// Public API ve serileştirilmiş alan adları değişmez; eklenebilir.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class SwordHook : MonoBehaviour
    {
        [SerializeField] SwordHookTuning tuning;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor body;
        /// <summary>Kılıç görseli. Oyuncunun çocuğu değil, sahnede bağımsız bir obje olmalı.</summary>
        [SerializeField] Transform sword;
        [SerializeField] Camera aimCamera;
        [SerializeField] FloatEvent traumaEvent;

        public HookState State { get; private set; } = HookState.InHand;
        /// <summary>Nişan yardımının şu an seçtiği anchor; yoksa null.</summary>
        public HookAnchor AimTarget { get; private set; }

        public event Action<HookState> StateChanged;
    }
}
