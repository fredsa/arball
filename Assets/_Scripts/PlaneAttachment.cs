using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.Rendering;

public class PlaneAttachment : MonoBehaviour {
  public TrackedPlane m_AttachedPlane { get; private set; }

  private float m_planeYOffset;

  private MeshRenderer[] m_meshRenderers;

  private bool m_isVisible = true;

  /// <summary>
  /// Unity start.
  /// </summary>
  public void Start() {
    m_meshRenderers = GetComponentsInChildren<MeshRenderer>();
  }

  /// <summary>
  /// Unity update.
  /// </summary>
  public void Update() {
    if (m_AttachedPlane == null) {
      return;
    }
    
    // If the plane has been subsumed switch attachment to the subsuming plane.
    while (m_AttachedPlane.SubsumedBy != null) {
      m_AttachedPlane = m_AttachedPlane.SubsumedBy;
    }

    // Update visibility of the GameObject based on plane validity.
    if (!m_AttachedPlane.IsValid && m_isVisible) {
      for (int i = 0; i < m_meshRenderers.Length; i++) {
        m_meshRenderers[i].enabled = false;
      }

      m_isVisible = false;
    } else if (m_AttachedPlane.IsValid && !m_isVisible) {
      for (int i = 0; i < m_meshRenderers.Length; i++) {
        m_meshRenderers[i].enabled = true;
      }

      m_isVisible = true;
    }

    transform.position = new Vector3(transform.position.x, m_AttachedPlane.Position.y + m_planeYOffset,
      transform.position.z);
  }

  /// <summary>
  /// Have the GameObject maintain the y-offset to a plane.
  /// </summary>
  /// <param>The plane to attach to.</param>
  public void Attach(TrackedPlane plane) {
    m_AttachedPlane = plane;
    m_planeYOffset = transform.position.y - plane.Position.y;
  }
}