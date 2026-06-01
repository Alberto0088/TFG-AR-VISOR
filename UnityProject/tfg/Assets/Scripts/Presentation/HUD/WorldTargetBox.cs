/*
 * WorldTargetBox.cs
 * ------------------------------------------------------------
 * Caja pseudo-AR para marcar visualmente el target en el campo de visión.
 *
 * No modifica la retícula central.
 * No es texto flotante del HUD 2D.
 * Es un Canvas en World Space que se coloca delante de la cámara en la dirección
 * aproximada del target.
 */

using System;
using TFG.ARVisor.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TFG.ARVisor.Presentation.HUD
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class WorldTargetBox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform viewerCamera;

        [Header("Visibility")]
        [SerializeField] private bool showInLowRisk = false;
        [SerializeField] private float maxVisibleAngleDegrees = 70f;

        [Header("World Placement")]
        [SerializeField] private float markerDistanceMeters = 12f;
        [SerializeField] private float verticalOffsetMeters = -0.15f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Box Style")]
        [SerializeField] private Vector2 mediumBoxSize = new Vector2(180f, 90f);
        [SerializeField] private Vector2 highBoxSize = new Vector2(220f, 105f);
        [SerializeField] private float worldScale = 0.0035f;
        [SerializeField] private float cornerLength = 34f;
        [SerializeField] private float cornerThickness = 4f;
        [SerializeField] private float backgroundAlpha = 0.12f;
        [SerializeField] private float highPulseSpeed = 4f;
        [SerializeField] private float highPulseAmount = 0.18f;

        [Header("Label")]
        [SerializeField] private int mediumFontSize = 20;
        [SerializeField] private int highFontSize = 24;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform rootRect;

        private RectTransform boxRoot;
        private RawImage background;

        private RawImage topLeftH;
        private RawImage topLeftV;
        private RawImage topRightH;
        private RawImage topRightV;
        private RawImage bottomLeftH;
        private RawImage bottomLeftV;
        private RawImage bottomRightH;
        private RawImage bottomRightV;

        private TMP_Text label;

        private Vector3 targetWorldPosition;
        private Vector2 targetBoxSize;
        private RiskLevel currentRisk;
        private bool visible;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            rootRect = GetComponent<RectTransform>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            if (viewerCamera == null && Camera.main != null)
            {
                viewerCamera = Camera.main.transform;
                canvas.worldCamera = Camera.main;
            }

            transform.localScale = Vector3.one * worldScale;

            BuildBox();
            HideImmediate();
        }

        private void Update()
        {
            if (!visible || viewerCamera == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(
                transform.position,
                targetWorldPosition,
                Time.deltaTime * smoothSpeed
            );

            Vector3 lookDirection = transform.position - viewerCamera.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            boxRoot.sizeDelta = Vector2.Lerp(
                boxRoot.sizeDelta,
                targetBoxSize,
                Time.deltaTime * smoothSpeed
            );

            if (currentRisk == RiskLevel.High)
            {
                float pulse = 1f + Mathf.Sin(Time.time * highPulseSpeed) * highPulseAmount;
                boxRoot.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                boxRoot.localScale = Vector3.Lerp(
                    boxRoot.localScale,
                    Vector3.one,
                    Time.deltaTime * smoothSpeed
                );
            }
        }

        /// <summary>
        /// Recibe los datos actuales y decide si la caja debe mostrarse.
        /// </summary>
        public void RenderBox(TrafficSnapshot snapshot)
        {
            if (snapshot == null || !HasTarget(snapshot))
            {
                Hide();
                return;
            }

            if (snapshot.RiskLevel == RiskLevel.Low && !showInLowRisk)
            {
                Hide();
                return;
            }

            if (!snapshot.TargetViewOffsetDegrees.HasValue)
            {
                Hide();
                return;
            }

            double offset = snapshot.TargetViewOffsetDegrees.Value;

            if (Math.Abs(offset) > maxVisibleAngleDegrees)
            {
                Hide();
                return;
            }

            currentRisk = snapshot.RiskLevel;

            Color riskColor = GetRiskColor(snapshot.RiskLevel);
            ApplyColor(riskColor, snapshot.RiskLevel);

            targetWorldPosition = CalculateWorldPosition(offset);
            targetBoxSize = snapshot.RiskLevel == RiskLevel.High
                ? highBoxSize
                : mediumBoxSize;

            if (label != null)
            {
                label.text = BuildLabel(snapshot);
                label.color = riskColor;
                label.fontSize = snapshot.RiskLevel == RiskLevel.High
                    ? highFontSize
                    : mediumFontSize;
            }

            Show();
        }

        /// <summary>
        /// Calcula una posición en el mundo delante de la cámara según el ángulo horizontal del target.
        /// </summary>
        private Vector3 CalculateWorldPosition(double offsetDegrees)
        {
            Quaternion horizontalRotation = Quaternion.AngleAxis((float)offsetDegrees, Vector3.up);
            Vector3 direction = horizontalRotation * viewerCamera.forward;

            return viewerCamera.position +
                   direction.normalized * markerDistanceMeters +
                   Vector3.up * verticalOffsetMeters;
        }

        /// <summary>
        /// Construye visualmente la caja con esquinas y etiqueta.
        /// </summary>
        private void BuildBox()
        {
            rootRect.sizeDelta = highBoxSize;

            boxRoot = CreateRect("TargetLockBox", transform);
            boxRoot.anchorMin = new Vector2(0.5f, 0.5f);
            boxRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boxRoot.pivot = new Vector2(0.5f, 0.5f);
            boxRoot.anchoredPosition = Vector2.zero;
            boxRoot.sizeDelta = mediumBoxSize;

            background = CreateRawImage("Background", boxRoot);
            StretchToParent(background.rectTransform);

            topLeftH = CreateRawImage("TopLeft_H", boxRoot);
            topLeftV = CreateRawImage("TopLeft_V", boxRoot);
            topRightH = CreateRawImage("TopRight_H", boxRoot);
            topRightV = CreateRawImage("TopRight_V", boxRoot);
            bottomLeftH = CreateRawImage("BottomLeft_H", boxRoot);
            bottomLeftV = CreateRawImage("BottomLeft_V", boxRoot);
            bottomRightH = CreateRawImage("BottomRight_H", boxRoot);
            bottomRightV = CreateRawImage("BottomRight_V", boxRoot);

            LayoutCorners();

            label = CreateLabel("TargetLabel", boxRoot);
        }

        private void LayoutCorners()
        {
            SetCorner(topLeftH.rectTransform, 0f, 1f, cornerLength, cornerThickness, cornerLength * 0.5f, -cornerThickness * 0.5f);
            SetCorner(topLeftV.rectTransform, 0f, 1f, cornerThickness, cornerLength, cornerThickness * 0.5f, -cornerLength * 0.5f);

            SetCorner(topRightH.rectTransform, 1f, 1f, cornerLength, cornerThickness, -cornerLength * 0.5f, -cornerThickness * 0.5f);
            SetCorner(topRightV.rectTransform, 1f, 1f, cornerThickness, cornerLength, -cornerThickness * 0.5f, -cornerLength * 0.5f);

            SetCorner(bottomLeftH.rectTransform, 0f, 0f, cornerLength, cornerThickness, cornerLength * 0.5f, cornerThickness * 0.5f);
            SetCorner(bottomLeftV.rectTransform, 0f, 0f, cornerThickness, cornerLength, cornerThickness * 0.5f, cornerLength * 0.5f);

            SetCorner(bottomRightH.rectTransform, 1f, 0f, cornerLength, cornerThickness, -cornerLength * 0.5f, cornerThickness * 0.5f);
            SetCorner(bottomRightV.rectTransform, 1f, 0f, cornerThickness, cornerLength, -cornerThickness * 0.5f, cornerLength * 0.5f);
        }

        private void SetCorner(
            RectTransform rect,
            float anchorX,
            float anchorY,
            float width,
            float height,
            float posX,
            float posY)
        {
            rect.anchorMin = new Vector2(anchorX, anchorY);
            rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(posX, posY);
        }

        private TMP_Text CreateLabel(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-10f, -10f);

            TMP_Text text = child.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.raycastTarget = false;

            return text;
        }

        private RawImage CreateRawImage(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            child.transform.SetParent(parent, false);

            RawImage image = child.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.raycastTarget = false;

            return image;
        }

        private RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

       private string BuildLabel(TrafficSnapshot snapshot)
{
    string callsign = string.IsNullOrWhiteSpace(snapshot.RelevantCallsign)
        ? "TARGET"
        : snapshot.RelevantCallsign;

    string distance = string.IsNullOrWhiteSpace(snapshot.NearestDistance)
        ? "--"
        : snapshot.NearestDistance;

    string timeToConflict = string.IsNullOrWhiteSpace(snapshot.TimeToClosestApproach) ||
                            snapshot.TimeToClosestApproach == "--"
        ? ""
        : snapshot.TimeToClosestApproach;

    if (snapshot.RiskLevel == RiskLevel.High)
    {
        if (!string.IsNullOrWhiteSpace(timeToConflict))
        {
            return $"{callsign}\n{distance}\nIN {timeToConflict}";
        }

        return $"{callsign}\n{distance}";
    }

    if (snapshot.RiskLevel == RiskLevel.Medium)
    {
        if (!string.IsNullOrWhiteSpace(timeToConflict))
        {
            return $"{callsign}\n{distance}\nIN {timeToConflict}";
        }

        return $"{callsign}\n{distance}";
    }

    return $"{callsign}\n{distance}";
}

        private void ApplyColor(Color color, RiskLevel risk)
        {
            SetColor(topLeftH, color);
            SetColor(topLeftV, color);
            SetColor(topRightH, color);
            SetColor(topRightV, color);
            SetColor(bottomLeftH, color);
            SetColor(bottomLeftV, color);
            SetColor(bottomRightH, color);
            SetColor(bottomRightV, color);

            if (background != null)
            {
                Color bg = color;
                bg.a = risk == RiskLevel.High
                    ? backgroundAlpha * 1.8f
                    : backgroundAlpha;

                background.color = bg;
            }
        }

        private void SetColor(RawImage image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private Color GetRiskColor(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return new Color(1f, 0.2f, 0.2f);

                case RiskLevel.Medium:
                    return new Color(1f, 0.85f, 0.25f);

                default:
                    return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        private bool HasTarget(TrafficSnapshot snapshot)
        {
            return snapshot != null &&
                   !string.IsNullOrWhiteSpace(snapshot.RelevantCallsign);
        }

        private void Show()
        {
            visible = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Hide()
        {
            visible = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void HideImmediate()
        {
            visible = false;
            Hide();
        }
    }
}
