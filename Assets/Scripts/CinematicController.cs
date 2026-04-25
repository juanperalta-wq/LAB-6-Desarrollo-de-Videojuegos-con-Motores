using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

public class CinematicController : MonoBehaviour
{
    public CinemachineCamera camA;
    public CinemachineCamera camB;
    public CinemachineCamera camC;
    public CinemachineCamera camD;
    private int currentCam = 0;
    public FirstPersonController player;
    public float timePerCamera = 10f;
    public CinemachineCamera playerCam;

    void Start()
    {
        player.canMove = false;
        StartCoroutine(PlayCinematic());
    }

    [Button]
    public void SwitchCameras()
    {
        currentCam++;

        if (currentCam > 4)
            currentCam = 0;

        ActivateCamera(currentCam);
    }

    void ActivateCamera(int index)
    {
        camA.Priority = 0;
        camB.Priority = 0;
        camC.Priority = 0;
        camD.Priority = 0;

        switch (index)
        {
            case 0:
                camA.Priority = 20;
                break;
            case 1:
                camB.Priority = 20;
                break;
            case 2:
                camC.Priority = 20;
                break;
            case 3: 
                camD.Priority = 20;
                break;
        }
    }
    IEnumerator PlayCinematic()
    {
        ActivateCamera(0);
        yield return new WaitForSeconds(timePerCamera);

        ActivateCamera(1);
        yield return new WaitForSeconds(timePerCamera);

        ActivateCamera(2);
        yield return new WaitForSeconds(timePerCamera);

        ActivateCamera(3);
        yield return new WaitForSeconds(timePerCamera);


        playerCam.Priority = 40;   // Return control to the player
        player.canMove = true;
    }

}