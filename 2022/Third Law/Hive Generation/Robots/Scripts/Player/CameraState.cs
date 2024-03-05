using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CameraState
{
    public static bool withinShip;
    public static bool inShip;
    public static bool flyingShip;
    public static bool playingTetris;
    public static bool inHive;
    public static bool isPaused;
    public static bool isDead;
    private static RoboVision roboVision;
    public enum LockState { locked, unlocked, changing }

    public static void Reset()
    {
        flyingShip = false;
        inHive = false;
        roboVision.Unlock();
        foreach (FlipSwitch flipSwitch in Object.FindObjectsOfType<FlipSwitch>())
            flipSwitch.Start();
        foreach (ShipDoorOpen shipDoor in Object.FindObjectsOfType<ShipDoorOpen>())
            shipDoor.Start();
        Object.FindObjectsOfType<Interface>().Where(computer => computer.shipComputer).First().StopInteracting();
    }

    public static bool CamIsInteractingW(Vector3 objectPos, Vector3 fwdNormal, float maxDist, float maxAngle)
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return false;

        return CanInteractW(objectPos, fwdNormal, maxDist, maxAngle);
    }

    public static bool CamIsInteractingW(Vector3 objectPos, float maxDist)
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return false;

        return !((Camera.main.transform.position - objectPos).sqrMagnitude > maxDist * maxDist);
    }

    public static bool CanInteractW(Vector3 objectPos, Vector3 fwdNormal, float maxDist, float maxAngle)
    {
        if ((Camera.main.transform.position - objectPos).sqrMagnitude > maxDist * maxDist)
            return false;

        Vector3 camDir = Camera.main.transform.forward;
        Vector3 objDir = (Camera.main.transform.position - objectPos).normalized;
        float angle = Mathf.Acos(Vector3.Dot(camDir, objDir)) * Mathf.Rad2Deg;

        return !(Mathf.Abs(angle) < 180 - maxAngle || Vector3.Dot(camDir, fwdNormal) < 0);
    }

    public static bool LockCamera(Transform cameraPos)
    {
        if (roboVision != null || Camera.main.TryGetComponent(out roboVision))
        {
            roboVision.Lock(cameraPos);
            return true;
        }
        return false;
    }

    public static bool UnlockCamera()
    {
        if (roboVision != null || Camera.main.TryGetComponent(out roboVision))
        {
            roboVision.Unlock();
            return true;
        }
        return false;
    }

    public static bool InLockState(LockState lockState)
    {
        if (roboVision != null || Camera.main.TryGetComponent(out roboVision))
            return roboVision.isLocked == lockState;

        return lockState == LockState.unlocked;
    }
}
