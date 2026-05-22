#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CorePro.Utils.Extensions
{
    public static partial class GizmoExtensions
    {
        /// <summary>.
        /// Draws a hitscan line with start position, direction and length.
        /// </summary>.
        /// <param name="origin">Start point (usually barrel.FirePoint.position)</param>.
        /// <param name="direction">Direction (most often barrel.FirePoint.forward)</param>.
        /// <param name="range">Range length (e.g. 100f)</param>.
        /// <param name="color">Color of the gizmo (optional)</param>.
        public static void DrawHitscanRange(Vector3 origin, Vector3 direction, float range, Color? color = null)
        {
#if UNITY_EDITOR
            Color prevColor = Gizmos.color;
            Gizmos.color = color ?? Color.red;
            Gizmos.DrawLine(origin, origin + direction.normalized * range);
            Gizmos.DrawWireSphere(origin + direction.normalized * range, 0.08f);
            Gizmos.color = prevColor;
#endif
        }

        public static void DrawHitScanRay(Transform firePoint, float maxLength, Color debugRayColor, 
            LayerMask hitLayer, float hitPointSize = 0.1f)
        {
            Vector3 origin = firePoint.position;
            Vector3 direction = firePoint.forward;
            Vector3 end = origin + direction * maxLength;

            // HIT DETECTION IN EDITOR MODE
            bool hitDetected = Physics.Raycast(origin, direction, out RaycastHit hit, maxLength, hitLayer);

            if (hitDetected)
            {
                end = hit.point;

                // Rycast line to hit point - full colour
                Gizmos.color = debugRayColor;
                Gizmos.DrawLine(origin, end);

                // Hit point
                Gizmos.color = debugRayColor;
                Gizmos.DrawSphere(hit.point, hitPointSize);

                // Optional: Name of the object we are targeting
#if UNITY_EDITOR
                UnityEditor.Handles.Label(hit.point + Vector3.up * 0.5f, hit.collider.name);
#endif
            }
            else
            {
                // No hit - translucent line
                Gizmos.color = new Color(debugRayColor.r, debugRayColor.g, debugRayColor.b, 0.3f);
                Gizmos.DrawLine(origin, end);

                // Arrow at the end showing the direction
                Vector3 arrowHead1 = end - direction * 0.5f + firePoint.right * 0.2f;
                Vector3 arrowHead2 = end - direction * 0.5f - firePoint.right * 0.2f;
                Gizmos.DrawLine(end, arrowHead1);
                Gizmos.DrawLine(end, arrowHead2);
            }
        }

        public static void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments = 24)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            tangent.Normalize();

            Vector3 biTangent = Vector3.Cross(normal, tangent).normalized;

            float angleStep = 360f / segments;
            Vector3 prevPoint = center + radius * tangent;

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + radius * Mathf.Cos(angle) * tangent + radius * Mathf.Sin(angle) * biTangent;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        public static void DrawSpreadCone(Vector3 apex, Vector3 endPoint, float radius, int segments = 8)
        {
            Vector3 direction = (endPoint - apex).normalized;
            Vector3 tangent = Vector3.Cross(direction, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(direction, Vector3.right);
            tangent.Normalize();

            Vector3 biTangent = Vector3.Cross(direction, tangent).normalized;
            float angleStep = 360f / segments;
            Vector3[] points = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                points[i] = endPoint + radius * Mathf.Cos(angle) * tangent + radius * Mathf.Sin(angle) * biTangent;
                Gizmos.DrawLine(apex, points[i]);
            }

            // Circle base
            for (int i = 0; i < segments; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % segments]);
            }
        }

        /// <summary>.
        /// Draws arc (solid + wire) and boundary lines.
        /// </summary>.
        /// <param name="center">The center of the arc</param>.
        /// <param name="normal">Normal plane (axis of rotation)</param>.
        /// <param name="from">Initial direction</param>.
        /// <param name="angle">Angle width (degrees)</param>.
        /// <param name="radius">explosionRadius of arc</param>.
        /// <param name="arcColor">Color of the arc (solid + wire)</param>.
        /// <param name="alphaSolidArc">Transparency of solid arc</param>.
        /// <param name="drawBorders">Whether to draw border lines</param>.
        /// <param name="borderColor">Border color</param>.
        public static void DrawArcWithBorders(
            Vector3 center, Vector3 normal, Vector3 from, float angle, float radius,
            Color arcColor, float alphaSolidArc = 0.1f,
            bool drawBorders = true, Color? borderColor = null)
        {
            // Wire arc (contour)
            Handles.color = arcColor.WithAlpha(1f);
            Handles.DrawWireArc(center, normal, from, angle, radius);

            // Solid arc (fill)
            Handles.color = arcColor.WithAlpha(alphaSolidArc);
            Handles.DrawSolidArc(center, normal, from, angle, radius);

            // Boundary lines
            if (drawBorders)
            {
                Color bc = borderColor ?? arcColor;
                Handles.color = bc.WithAlpha(1f);
                Handles.DrawLine(center, center + Quaternion.AngleAxis(0, normal) * from.normalized * radius);
                Handles.DrawLine(center, center + Quaternion.AngleAxis(angle, normal) * from.normalized * radius);
            }
        }

        /// <summary>
        /// Helper for rotated line (one border).
        /// </summary>
        public static void DrawRotatedLine(Vector3 origin, Vector3 axis, Vector3 forward, float angle, float length, Color? color = null)
        {
            if (color.HasValue)
                Handles.color = color.Value;
            Vector3 dir = Quaternion.AngleAxis(angle, axis) * forward;
            Handles.DrawLine(origin, origin + dir.normalized * length);
        }

        /// <summary>
        /// Extension - quick change of alpha color.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
        
        /// <summary>
        /// Common logic to determine if gizmos should be drawn based on runtime settings
        /// </summary>
        /// <param name="showInRuntime">Whether to show in play mode</param>
        /// <returns>True if gizmos should be drawn</returns>
        public static bool ShouldDrawGizmos(bool showInRuntime)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) 
                return true; // Always show in edit mode
            
            return showInRuntime; // In play mode, respect the setting
#else
            return false;
#endif
        }
        
        /// <summary>
        /// Draws raycast gizmo with hit detection - extracted from ModuleShotProjectile and FireModeContinuous`
        /// </summary>
        /// <param name="firePoint">Transform of fire point (barrel.FirePoint)</param>
        /// <param name="maxLength">Maximum raycast distance</param>
        /// <param name="raycastColor">Color of the raycast line</param>
        /// <param name="hitLayer">LayerMask for hit detection</param>
        /// <param name="hitPointSize">Size of hit point sphere</param>
        /// <param name="showLabels">Whether to show hit object labels</param>
        public static void DrawRaycastGizmo(Transform firePoint, LayerMask hitLayer, Color raycastColor,float maxLength = 1000,
                    float hitPointSize = 0.1f, bool showLabels = true)
        {
#if UNITY_EDITOR
            if (firePoint == null) return;

            Vector3 origin = firePoint.position;
            Vector3 direction = firePoint.forward;
            Vector3 end = origin + direction * maxLength;

            // HIT DETECTION IN EDITOR MODE
            bool hitDetected = Physics.Raycast(origin, direction, out RaycastHit hit, maxLength, hitLayer);

            if (hitDetected)
            {
                end = hit.point;

                // Raycast line to hit point - full colour
                Gizmos.color = raycastColor;
                Gizmos.DrawLine(origin, end);

                // Hit point
                Gizmos.color = raycastColor;
                Gizmos.DrawSphere(hit.point, hitPointSize);

                // Optional: Name of the object we are targeting
                if (showLabels && hit.collider != null)
                {
                    UnityEditor.Handles.Label(hit.point + Vector3.up * 0.5f, hit.collider.name);
                }
            }
            else
            {
                // No hit - translucent line
                Gizmos.color = new Color(raycastColor.r, raycastColor.g, raycastColor.b, 0.3f);
                Gizmos.DrawLine(origin, end);

                // Arrow at the end showing the direction
                DrawDirectionArrow(end, direction, firePoint.right, 0.5f, 0.2f);
            }
#endif
        }
                
        public static void DrawDirectionArrow(Vector3 end, Vector3 direction, Vector3 right, 
            float arrowLength = 0.5f, float arrowWidth = 0.2f)
        {
#if UNITY_EDITOR
            Vector3 arrowHead1 = end - direction * arrowLength + right * arrowWidth;
            Vector3 arrowHead2 = end - direction * arrowLength - right * arrowWidth;
            Gizmos.DrawLine(end, arrowHead1);
            Gizmos.DrawLine(end, arrowHead2);
#endif
        }
    }
}
#endif
