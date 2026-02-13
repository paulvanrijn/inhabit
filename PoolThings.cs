
using UnityEngine;
using UnityEditor;

public class PoolThings: MonoBehaviour 
{
    public bool BooOn;  // boo behavior is on
    public Vector3 BooVector;
    public  EventList eventState ;
    private float booTimer;
    private float booTime;
    private float nextBoo;
    private float nextBooTimer;
    
    public enum EventList {boo, booRun, face, flock, idle};
    
    public Material sky;

    void Start()  {
        float booy;
        float boox;
        float booz;
        eventState = EventList.idle;
        nextBoo = Random.Range(30.0f, 60.0f);
        nextBooTimer = 0.0f;
        booTime = 0.1f;
        booy = Random.Range(-4f, 4.0f);
        booz = Random.Range(25f, 60.0f);
        boox = Random.Range(-5f, 5.0f);
        BooVector = new Vector3(boox, booy, booz);
        BooOn = false;
        if (sky != null)
        {
            // Set the scene's skybox material at runtime
            RenderSettings.skybox = sky;
        }
    }
    void Update() {
    	switch (eventState) {
    	case EventList.idle:
    	    nextBooTimer += Time.deltaTime;
            if (nextBooTimer > nextBoo) {
                eventState = EventList.boo;
            }
    	    break;
    	case EventList.boo:
    	    doBoo();
    	    break;
    	default:
    	    break;
    	} // end switch eventState
    } // end Update
    
    void doBoo() {
        float booy;
        float boox;
        float booz;
         booTimer += Time.deltaTime;
        if (booTimer > booTime) {
            booTimer = 0.0f;
            BooOn = false;
           booy = Random.Range(-1f, 1.0f);
           booz = Random.Range(-1f, 1.0f);
           boox = Random.Range(-1f, 1.0f);
           BooVector = new Vector3(boox, booy, booz);
            nextBoo = Random.Range(100.0f, 200.0f);
            nextBooTimer = 0.0f;
            eventState = EventList.idle; // stop it!
        } else {
            BooOn = true;
            eventState = EventList.boo;
         }
    } // end doBoo
}// end poolThings
