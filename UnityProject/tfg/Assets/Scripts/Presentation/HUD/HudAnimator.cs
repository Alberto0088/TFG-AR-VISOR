/*
 * HudAnimator.cs
 * ------------------------------------------------------------
 * Gestiona animaciones de color (pulse) para textos TMP del HUD.
 * Se añade como componente al mismo GameObject que HudController.
 * HudController lo registra y llama a StartPulse / StopPulse según el riesgo.
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TFG.ARVisor.Presentation.HUD
{
    public class HudAnimator : MonoBehaviour
    {
        private class PulseJob
        {
            public TMP_Text Target;
            public Color    ColorA;
            public Color    ColorB;
            public float    Speed;
        }

        private readonly List<PulseJob> _jobs = new List<PulseJob>();

        // ── Public API ────────────────────────────────────────────────

        public void StartPulse(TMP_Text text, Color colorA, Color colorB, float speed = 2.5f)
        {
            if (text == null) return;
            StopPulse(text);
            _jobs.Add(new PulseJob { Target = text, ColorA = colorA, ColorB = colorB, Speed = speed });
        }

        public void StopPulse(TMP_Text text)
        {
            if (text == null) return;
            _jobs.RemoveAll(j => j.Target == text);
        }

        public void StopAll()
        {
            _jobs.Clear();
        }

        // ── Unity ─────────────────────────────────────────────────────

        private void Update()
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                var job = _jobs[i];
                if (job.Target == null) { _jobs.RemoveAt(i); continue; }
                float t = (Mathf.Sin(Time.time * job.Speed) * 0.5f) + 0.5f;
                job.Target.color = Color.Lerp(job.ColorA, job.ColorB, t);
            }
        }
    }
}
