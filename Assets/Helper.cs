using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour {

  public GameObject m_andyAndroidPrefab;

  public List<PlaneAttachment> andys = new List<PlaneAttachment>();

  void Start() {
    Transform holder = new GameObject("holder").transform;
    holder.position = new Vector3(0f, -.1f, 1f);
    PlaneAttachment andy = Instantiate(m_andyAndroidPrefab, holder.position, Quaternion.identity, holder).GetComponent<PlaneAttachment>();
    andys.Add(andy);
  }

  void Update() {
    if (!Input.GetMouseButtonDown(0)) {
      return;
    }

    RaycastHit raycastHit;
    // Ray ray = Camera.main.ScreenPointToRay(touch.position);
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    bool found = Physics.Raycast(ray, out raycastHit, 1000f);
    Debug.Log("found=" + found);

    if (found) {
      if (raycastHit.transform != null) {
        Debug.Log("raycastHit.transform=" + raycastHit.transform);
        PlaneAttachment a = raycastHit.transform.gameObject.GetComponent<PlaneAttachment>();
        Debug.Log("a.name=" + a.name);
        if (andys.Contains(a)) {
          Debug.Log("andys.Remove(a)");
          andys.Remove(a);
          Transform holder = a.transform.parent;
          Debug.Log("holder.name=" + holder.name);
          // Transform anchor = holder.parent;
          Destroy(holder.gameObject);
          return;
        }
      }
    }

  }

}