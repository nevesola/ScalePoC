using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerScript : MonoBehaviour
{
    public ControllerScript OtherController;

    private SteamVR_TrackedObject _trackedObj;
    public Transform ColliderPoint;
    private GameObject objectInHand;

    public Transform ResizeLine;
    public Resizer ResizeBubble;

    private bool isResizing; //Only happens if we grab a 'resizer'
    private float _startDist;
    private Resizer _resizer;

    private Vector3 _baseScale;
    public float _scaleDelta;
    public LayerMask InteractMask;

    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)_trackedObj.index); }
    }

    void Awake()
    {
        _trackedObj = GetComponent<SteamVR_TrackedObject>();

    }

    private void GrabObject(GameObject collidingObject)
    {
            if (OtherController && OtherController.objectInHand != null && OtherController.objectInHand == collidingObject)
                return;
            objectInHand = collidingObject;
    
            var joint = AddFixedJoint();
            joint.connectedBody = objectInHand.GetComponent<Rigidbody>();

            //ResizeLine.gameObject.SetActive(true);
            ResizeBubble.gameObject.SetActive(true);
            ResetScaleBubble();
    }

    public void ResetScaleBubble()
    {
        //Move it just to the left of the controller. 
        ResizeBubble.transform.position = transform.position + (transform.TransformDirection(Vector3.left)*0.25f);
    }

    private FixedJoint AddFixedJoint()
    {
        FixedJoint fx = gameObject.AddComponent<FixedJoint>();
        fx.breakForce = 20000;
        fx.breakTorque = 20000;
        return fx;
    }

    private void ReleaseObject()
    {
        if(isResizing)
        {
            _resizer.MyController.ResetScaleBubble();
            _resizer = null;
            isResizing = false;
            return;
        }
        if (GetComponent<FixedJoint>())
        {
            GetComponent<FixedJoint>().connectedBody = null;
            Destroy(GetComponent<FixedJoint>());
            objectInHand.GetComponent<Rigidbody>().velocity = Controller.velocity;
            objectInHand.GetComponent<Rigidbody>().angularVelocity = Controller.angularVelocity;
        }
        objectInHand = null;
        ResizeBubble.gameObject.SetActive(false);
    }

    public void DoResize(float delta)
    {
        if (!objectInHand)
            return;
        _scaleDelta = delta;
        objectInHand.transform.localScale = _baseScale * (1+_scaleDelta);
        ResizeBubble.transform.position = transform.position + (transform.TransformDirection(Vector3.left) * (0.25f + _scaleDelta));
    }

    public void StartResize()
    {
        if (!objectInHand)
            return;
        _baseScale = objectInHand.transform.localScale;
        _scaleDelta = 0;
    }

    public void Update()
    {
        if(isResizing)
        {
            var delta = Vector3.Distance(OtherController.transform.position, transform.position);
            _resizer.MyController.DoResize(delta - _startDist);
        }
        else if (objectInHand)
        {
            SetResizerTransparency();
        }

        if (Controller.GetHairTriggerDown())
        {
            CheckForObject();
        }

        if (Controller.GetHairTriggerUp())
        {
            if (isResizing || objectInHand)
            {
                ReleaseObject();
            }
        }
    }
    private void CheckForObject()
    {
        var hitObjects = Physics.OverlapSphere(ColliderPoint.position, 0.25f,InteractMask); //Check to see if we hit something.
        Collider bestHit = null;
        float bestDist = 1000;

        foreach(var obj in hitObjects)
        {
            if (bestHit == null)
                bestHit = obj;
            else
            {
                var res = bestHit.GetComponent<Resizer>();
                if (res != null && res != ResizeBubble)
                {
                    _resizer = res;
                    isResizing = true;
                    _startDist = Vector3.Distance(transform.position, OtherController.transform.position);
                    _resizer.MyController.StartResize();
                    return;
                }
                else
                {
                    var dist = Vector3.Distance(obj.transform.position, ColliderPoint.position);
                    if(dist < bestDist)
                    {
                        bestHit = obj;
                        bestDist = dist;
                    }
                }
            }
        }
        if (bestHit != null)
        {
            GrabObject(bestHit.gameObject);
        }
            
    }

    private void SetResizerTransparency()
    {
        if (!OtherController)
            return;
        //Get the distance
        var dist = Vector3.Distance(ResizeBubble.transform.position, OtherController.transform.position);
        //At 0.05f we are 1. At 0.25f we are 0.
        var scale = -2 * (dist - 0.5f);
        scale = Mathf.Clamp(scale, 0, 1);

        var t = ResizeBubble.transform;
        var rend = t.GetComponent<Renderer>();
        var color = rend.material.color;
        
        color.a = scale;
        rend.material.color = color;
    }
}
