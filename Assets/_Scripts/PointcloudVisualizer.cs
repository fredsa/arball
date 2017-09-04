using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Visualize the point cloud.
/// </summary>
public class PointcloudVisualizer : MonoBehaviour {
  private const int MAX_POINT_COUNT = 61440;

  private Mesh m_mesh;

  private Vector3[] m_points = new Vector3[MAX_POINT_COUNT];

  private double m_lastPointCloudTimestamp;

  /// <summary>
  /// Unity start.
  /// </summary>
  public void Start() {
    m_mesh = GetComponent<MeshFilter>().mesh;
    m_mesh.Clear();
  }

  /// <summary>
  /// Unity update.
  /// </summary>
  public void Update() {
    // Do not update if ARCore is not tracking.
    if (Frame.TrackingState != FrameTrackingState.Tracking) {
      return;
    }

    // Fill in the data to draw the point cloud.
    PointCloud pointcloud = Frame.PointCloud;
    if (pointcloud.PointCount > 0 && pointcloud.Timestamp > m_lastPointCloudTimestamp) {
      // Copy the point cloud points for mesh verticies.
      for (int i = 0; i < pointcloud.PointCount; i++) {
        m_points[i] = pointcloud.GetPoint(i);
      }

      // Update the mesh indicies array.
      int[] indices = new int[pointcloud.PointCount];
      for (int i = 0; i < pointcloud.PointCount; i++) {
        indices[i] = i;
      }

      m_mesh.Clear();
      m_mesh.vertices = m_points;
      m_mesh.SetIndices(indices, MeshTopology.Points, 0);
      m_lastPointCloudTimestamp = pointcloud.Timestamp;
    }
  }
}