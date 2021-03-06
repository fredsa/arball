﻿using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.Rendering;

public class AndysController : MonoBehaviour {

  public GameObject planeVisualizerPrefab;
  List<GameObject> visualizedPlanes = new List<GameObject>();

  /// The first-person camera being used to render the passthrough camera.
  public Camera m_firstPersonCamera;

  /// A prefab for tracking and visualizing detected planes.
  public GameObject m_trackedPlanePrefab;

  /// A model to place when a raycast from a user touch hits a plane.
  public GameObject m_andyAndroidPrefab;

  public List<PlaneAttachment> andys = new List<PlaneAttachment>();

  /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
  public GameObject m_searchingForPlaneUI;

  int andyLayermask;

  private List<TrackedPlane> m_newPlanes = new List<TrackedPlane>();

  private List<TrackedPlane> m_allPlanes = new List<TrackedPlane>();

  private Color[] m_planeColors = new Color[] {
    new Color(1.0f, 1.0f, 1.0f),
    new Color(0.956f, 0.262f, 0.211f),
    new Color(0.913f, 0.117f, 0.388f),
    new Color(0.611f, 0.152f, 0.654f),
    new Color(0.403f, 0.227f, 0.717f),
    new Color(0.247f, 0.317f, 0.709f),
    new Color(0.129f, 0.588f, 0.952f),
    new Color(0.011f, 0.662f, 0.956f),
    new Color(0f, 0.737f, 0.831f),
    new Color(0f, 0.588f, 0.533f),
    new Color(0.298f, 0.686f, 0.313f),
    new Color(0.545f, 0.764f, 0.290f),
    new Color(0.803f, 0.862f, 0.223f),
    new Color(1.0f, 0.921f, 0.231f),
    new Color(1.0f, 0.756f, 0.027f)
  };

  void Awake() {
    andyLayermask = LayerMask.GetMask(new string[] { "Andy" });
  }

  void OnApplicationPause(bool pause) {
    if (pause) {
      // All anchors are lost on pause.
      foreach (PlaneAttachment andy in andys) {
        Destroy(andy.transform.parent.gameObject);
      }
      andys.Clear();
    }
  }

  public void Update() {
    _QuitOnConnectionErrors();

    // The tracking state must be FrameTrackingState.Tracking in order to access the Frame.
    if (Frame.TrackingState != FrameTrackingState.Tracking) {
      const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
      Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
      return;
    }

    Screen.sleepTimeout = SleepTimeout.NeverSleep;
    Frame.GetNewPlanes(ref m_newPlanes);

    // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
    for (int i = 0; i < m_newPlanes.Count; i++) {
      // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
      // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
      // coordinates.
      GameObject planeObject = Instantiate(m_trackedPlanePrefab, Vector3.zero, Quaternion.identity,
        transform);
      planeObject.GetComponent<TrackedPlaneVisualizer>().SetTrackedPlane(m_newPlanes[i]);

      // Apply a random color and grid rotation.
      planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", m_planeColors[Random.Range(0,
        m_planeColors.Length - 1)]);
      planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
    }

    foreach(GameObject go in visualizedPlanes) {
      Destroy(go);
    }
    visualizedPlanes.Clear();


    // Disable the snackbar UI when no planes are valid.
    bool showSearchingUI = true;
    Frame.GetAllPlanes(ref m_allPlanes);
    for (int i = 0; i < m_allPlanes.Count; i++) {
      if (m_allPlanes[i].IsValid) {
        showSearchingUI = false;
        // break;
        GameObject planeVisualizer = Instantiate(planeVisualizerPrefab, m_allPlanes[i].Position, m_allPlanes[i].Rotation);
        planeVisualizer.transform.localScale = new Vector3(m_allPlanes[i].Bounds.x, 1f, m_allPlanes[i].Bounds.y);
        visualizedPlanes.Add(planeVisualizer);
      }
    }

    m_searchingForPlaneUI.SetActive(showSearchingUI);

    Touch touch;
    if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began) {
      UpdateLastAndy();
      return;
    }

    RaycastHit raycastHit;
    Ray ray = Camera.main.ScreenPointToRay(touch.position);
    bool found = Physics.Raycast(ray, out raycastHit, 1000f, andyLayermask);

    if (found) {
      if (raycastHit.transform != null) {
        PlaneAttachment a = raycastHit.transform.gameObject.GetComponent<PlaneAttachment>();
        if (andys.Contains(a)) {
          andys.Remove(a);
          Transform holder = a.transform.parent;
          // Transform anchor = holder.parent;
          Destroy(holder.gameObject);
          return;
        }
      }
    }

    TrackableHit hit;
    TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

    if (Session.Raycast(m_firstPersonCamera.ScreenPointToRay(touch.position), raycastFilter, out hit)) {
      // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
      // world evolves.
      var anchor = Session.CreateAnchor(hit.Point, Quaternion.identity);

      // Intanstiate a holder object as a child of the anchor; it's transform will now benefit
      // from the anchor's tracking.
      Transform holder = new GameObject("holder").transform;
      holder.SetParent(anchor.transform, false);

      PlaneAttachment andy = Instantiate(m_andyAndroidPrefab, hit.Point, Quaternion.identity,
        holder).GetComponent<PlaneAttachment>();
      andys.Add(andy);

      // Andy should look at the camera but still be flush with the plane.
      andy.transform.LookAt(m_firstPersonCamera.transform);
      andy.transform.rotation = Quaternion.Euler(0.0f,
        andy.transform.rotation.eulerAngles.y, andy.transform.rotation.z);

      // Use a plane attachment component to maintain Andy's y-offset from the plane
      // (occurs after anchor updates).
      andy.Attach(hit.Plane);
    }
  }

  void UpdateLastAndy() {
    TrackableHit hit;
    TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

    Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
    if (Session.Raycast(m_firstPersonCamera.ScreenPointToRay(center), raycastFilter, out hit)) {
      foreach (PlaneAttachment andy in andys) {
        // if (hit.Plane != andy.m_AttachedPlane) {
        //   continue;
        // }

        // Andy should look at the hit point but still be flush with the plane.
        andy.transform.LookAt(hit.Point);
        andy.transform.rotation = Quaternion.Euler(0.0f,
          andy.transform.rotation.eulerAngles.y, andy.transform.rotation.z);

        if (Vector3.Distance(hit.Point, andy.transform.position) > .1f) {
          andy.GetComponent<Rigidbody>().velocity = andy.transform.forward.normalized * 1f;
        } else {
          andy.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
      }
    }
  }

  /// Quit the application if there was a connection error for the ARCore session.
  private void _QuitOnConnectionErrors() {
    // Do not update if ARCore is not tracking.
    if (Session.ConnectionState == SessionConnectionState.DeviceNotSupported) {
      _ShowAndroidToastMessage("This device does not support ARCore.");
      Application.Quit();
    } else if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission) {
      _ShowAndroidToastMessage("Camera permission is needed to run this application.");
      Application.Quit();
    } else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed) {
      _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
      Application.Quit();
    }
  }

  /// Show an Android toast message.
  private static void _ShowAndroidToastMessage(string message) {
    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    if (unityActivity != null) {
      AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
      unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
        AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
          message, 0);
        toastObject.Call("show");
      }));
    }
  }
}