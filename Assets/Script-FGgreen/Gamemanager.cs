using UnityEngine;

public class Gamemanager : MonoBehaviour
{
    public int CurrentFloor;
    //public bool TimePause = true;
    void Start()
    {
        CurrentFloor = 1;
    }

    // Update is called once per frame
    void Update()
    {
        //Time.timeScale = TimePause
        //    ? 0f
        //    : 1f;
    }
}
