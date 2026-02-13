// behavior of individual faces
// version 05 - buiding texture behavior
// version 06 - bounding box estimate
// boo
using UnityEngine;
using UnityEditor;

public enum Mood {Drift, RunDrift, ToPoint, RunToPoint, StartBoo, RunBoo};

public class faceBehaviour : MonoBehaviour
{
    public Mood mood; // enummy
    private Mood lastMood;
    // to the point params
    public Vector3 toPosition;
    public float toTripDuration;
    // swimming motion 
    public float vertScaleAmplitude;
    private float vertScaleAngle; 
    public float vertScaleAngleIncr;
    private Vector3 vertScaleStart;
    public Camera mainCamera;
    // boo
    private bool isBooOn;
    private Vector3 booLocation;   // where the boo happened
    private Vector3 booDestination; // destination computed for this face
    private float booJumpDistance = 4.0f;  // Distance to jump on boo
    private float booJumpTime = 0.25f;  // duration of jump on boo seconds
    // pool
    public GameObject PoolBase;
    public PoolThings poolie;
    private Drift drift;
    // flocking
    public GameObject FlockBase;
    public GameFace flockie;
    private Rigidbody rb;
    // to point
    private Vector3 toStartPosition;
    private Vector3 velocity;
    public float smoothTime;
  
    void Start()     {
        rb = GetComponent<Rigidbody>();
        if (rb == null)  {
            Debug.LogError("Rigidbody component not found on this GameObject.");
            enabled = false; // Disable script if no Rigidbody
        }
        drift = new Drift(this);
        drift.StartDrifting();
        if (drift != null) {
        	Debug.Log("made DRIFT  BBBBBBBBBBBBBBBBBBBBBB");
        } else {
        	Debug.Log("DRIFT not made   CCCCCCCCCCCCCCCCCCCCCCC");
        }
        // set a random drift
        mood =  Mood.Drift;
	lastMood = Mood.Drift;
        // find the faces for flocking
        FlockBase = GameObject.Find("GameFace");
        if (FlockBase) {
        	flockie = FlockBase.GetComponent<GameFace>();
        } else {
        	Debug.Log("GameFace not made   DDDDDDDDDDDDDDDDDDDD");
        }
        // catch cool pool events
        PoolBase = GameObject.Find("PoolThings");
        if (PoolBase) {
        	poolie = PoolBase.GetComponent<PoolThings>();
        } else {
        	Debug.Log("PoolThing not made   AAAAAAAAAAAAAAAAAAAAAAAAA");
        }        
	// swimming action
	vertScaleAngle = 0.0f; 
        vertScaleAngleIncr = Random.Range(0.5f, 1.0f); 
        vertScaleStart = transform.localScale;
        // bounding around box
        mainCamera = Camera.main;
 
    } // start
    
    // Update is called once per frame
    void Update() {
// catch things from the pool
        if (poolie.BooOn && mood != Mood.RunBoo) {  
            lastMood = mood;
            mood = Mood.RunBoo;
            StartBoo();
        }
        switch (mood) {
            case Mood.Drift:
                mood = Mood.RunDrift;
                drift.StartDrifting();
                break;
            case Mood.RunDrift:
                drift.Drifting();
                SwimmingMotion();
                break;
            case Mood.ToPoint:
                mood = Mood.RunToPoint;
                StartToPoint(3.0f);
                break;
           case Mood.RunToPoint:
                ToThePoint();
                SwimmingMotion();
                break;
          case Mood.RunBoo:
                RunningBoo();
                break;
            default:
                var outStr = string.Format(" bad mood face: {0}, transform: {1}", name,  transform.position);
                Debug.Log(outStr);
                break;
        } // mood switch
    } // update
    
    void StartToPoint(float reqTime ) {
         velocity = Vector3.zero; // The current velocity, required by SmoothDamp
         smoothTime = reqTime; // The time to reach the target
    } // start to the point
    
    void ToThePoint() {
        transform.position = Vector3.SmoothDamp(transform.position, toPosition, ref velocity, smoothTime);
        toStartPosition = transform.position;

        if (Vector3.Distance(transform.position, toPosition) < 0.3f) {
             mood = Mood.Drift;
        }
    } //End ToThePoint
        // implement the swimming motion
        // speed and intensity set via glow balls
    void SwimmingMotion() {
    // use a sin wave for now - should make a curve
        vertScaleAngle += (vertScaleAngleIncr*Time.deltaTime);
        if (vertScaleAngle >= Mathf.PI) {
                vertScaleAngle = 0.0f;
            }
            var scale = transform.localScale;
            var sinVal = Mathf.Sin(vertScaleAngle);
            scale.z = sinVal * (vertScaleStart.z*0.3f) + (vertScaleStart.z*0.6f);// 60 percent static - 30 percent variable height
            transform.localScale = scale;
    } // swimming motion
    private Vector3 booVelocity;
    private float booSmoothTime;
    void StartBoo() {
        // start at current location 
        //  passed from poolie
        booLocation = poolie.BooVector;
        // compute the end
        Vector3 nowLocation = transform.position;
        Vector3 direction = (nowLocation - booLocation ).normalized;
        booDestination =  nowLocation + (direction * booJumpDistance); // was -
        booVelocity = Vector3.zero; // The current velocity, required by SmoothDamp
        booSmoothTime = booJumpTime; // The time to reach the target
    }
    void RunningBoo() {
        transform.position = Vector3.SmoothDamp(transform.position, booDestination, ref booVelocity, booSmoothTime);

        if (Vector3.Distance(transform.position, booDestination) < 0.9f) {
 //           drift.StartDrifting();
//             mood = lastMood;
		mood = Mood.Drift;
        }
    }
// put behaviours into nested classes
public class Drift {
    public float xDriftNow;
    public float yDriftNow;
    public float zDriftNow;
    private float my_x;
    private float my_y;
    private float my_z;
    public float my_vx;
    public float my_vy;
    private float my_vz;
    // constructor to pass in the face behaviour class
    private faceBehaviour faceB;
    // flocking
    private float flockSight = 13.9f; // distance to see or join a flock
    private float flockingTooClose = 09.5f; // collision avoidance distance 2.5
    public Vector3 flockVector; // vector decided by the flock 
    public float maxv = 0.20f;
    public float driftLife = 35.5f; // minimum drift life in seconds

public Drift(faceBehaviour outerInstance) {
         faceB = outerInstance;
    }

   public void StartDrifting() {  
       driftLife = Random.Range(22.0f, 35.5f); // minimum drift life in seconds
   // start at a random spot outside the frame with a vector 
   	int side = Random.Range(0, 4);
   	if (side < 1) { // top
   		faceB.transform.position = new Vector3(Random.Range(-10f, 10.0f), Random.Range(15f, 10.0f), Random.Range(-8f, 8.0f));
	} else if (side < 2) { // bottom
   		faceB.transform.position = new Vector3(Random.Range(-10f, 10.0f), Random.Range(-15f, -10.0f), Random.Range(-8f, 8.0f));
	}  else if (side < 3) { // right
   		faceB.transform.position = new Vector3(Random.Range(-10f, -12.0f), Random.Range(-15f, 10.0f), Random.Range(-8f, 8.0f));
   	} else { // left
   	   	faceB.transform.position = new Vector3(Random.Range(10f, 12.0f), Random.Range(-15f, 10.0f), Random.Range(-8f, 8.0f));
   	}
    // randomize the drift parameters 
    	xDriftNow = Random.Range(0.1f, 2.9f);
        yDriftNow = Random.Range(0.3f, 1.9f);
	zDriftNow = Random.Range(-1.3f, 1.31f);
	Vector3 directions = new Vector3(xDriftNow, yDriftNow, zDriftNow);
	faceB.rb.linearVelocity = directions;
    } // end start drifting
    public void Drifting() {
    	if  (driftLife > 0f) {
		driftLife -= Time.deltaTime;
	} 
// add randome bursts of NRG
//		Vector3 movement = new Vector3(0.35f, 0f, 0f) ;
//		faceB.rb.AddForce(movement, ForceMode.Impulse); 
	
         // general motion -  make a camera oriented bounding box
        Vector3 screenPos = Camera.main.WorldToViewportPoint(faceB.transform.position);
        //  x and y are normalized screen coordinates while z is world coordinates
        my_x = faceB.transform.position.x;
        my_y = faceB.transform.position.y;
        my_z = faceB.transform.position.z;
        	
        my_vx = faceB.rb.linearVelocity.x;
        my_vy = faceB.rb.linearVelocity.y;
        my_vz = faceB.rb.linearVelocity.z; 
        	
        	bool setVelocity = false;
     		if (my_vx > maxv) {
       		      my_vx = maxv;
       		 }
       		if (my_vx < -maxv) {
       		      my_vx = -maxv;
       		 }       		 
       		if (my_vy > maxv) {
       		      my_vy = maxv;
       		 }
       		if (my_vy < -maxv) {
       		      my_vy = -maxv;
       		 }
       		 if (my_vz > maxv) {
       		      my_vz = maxv;
       		 }
       		if (my_vz < -maxv) {
       		      my_vz = -maxv;
       		 }       	
        	if (screenPos.x > 1.0f  && xDriftNow > 0f){
            		xDriftNow = -Mathf.Abs(my_vx);
            		setVelocity = true;
            		
            		if (driftLife < 0f) {
            			faceB.mood = Mood.Drift;
            			faceB.gameObject.SetActive(false);
            		}
        	}
        	if( screenPos.x  < -1.0f && xDriftNow < 0f) {
            		setVelocity = true;
            		xDriftNow = Mathf.Abs(my_vx);
            		if (driftLife < 0f) {
                		faceB.gameObject.SetActive(false);
            			faceB.mood = Mood.Drift;
			}
        	}
        	if (screenPos.y > 1.0f  && yDriftNow > 0f) {
 //       	       	Debug.LogFormat("pos {0} my velocity {1}", screenPos, faceB.rb.linearVelocity);
            		setVelocity = true;
            		yDriftNow = -Mathf.Abs(my_vy);
                    	if (driftLife < 0f) {
            			faceB.mood = Mood.Drift;
            			faceB.gameObject.SetActive(false);
            		}
        	}
        	if (screenPos.y  < -1.1f && yDriftNow < 0f) {
            		setVelocity = true;
            		yDriftNow = Mathf.Abs(my_vy);
            		if (driftLife < 0f) {
            			faceB.mood = Mood.Drift;
            			faceB.gameObject.SetActive(false);
            		}
        	}
        	if (my_z > 8.5f ) {
            		setVelocity = true;
            		zDriftNow = -Mathf.Abs(my_vz);
        	}
        	if ( my_z  < -1.9) {
            		setVelocity = true;
            		zDriftNow = Mathf.Abs(my_vz);
        	}
        	if (setVelocity) {
       			faceB.rb.linearVelocity = new Vector3(xDriftNow, yDriftNow, zDriftNow);
       		}
         	Flock();
 		faceB.rb.linearVelocity += (flockVector * Time.deltaTime);

// 		Debug.LogFormat("Flock Vector, {0} my velocity {1}", flockVector, faceB.rb.linearVelocity);
    	} // Drifting
    	
    	private Vector3 centroid;
	private Vector3 collision;	
	private Vector3 flockV;	
	private float faceCount;
//medium.com/@pramodayajayalath/flocking-algorithm-simulating-collective-behavior-in-nature-inspired-systems-dc6d7fb884cc
    	public void Flock() {
    		faceCount = 0f;
    		centroid = Vector3.zero;
    		collision = Vector3.zero;
    		flockV = Vector3.zero;
		foreach (Rigidbody flockFace in faceB.flockie.faceBook) {
    			float flockDistance = Vector3.Distance(faceB.transform.position, flockFace.transform.position);
// centroid of flock
//    		        Debug.LogFormat("me, {0} distance {1} other{2}",faceB.transform.position , flockDistance, flockFace.transform.position);
			if (flockDistance < flockSight) {
				if (flockFace.name != faceB.name) {
					faceCount = faceCount +1f;
					centroid = centroid + flockFace.transform.position;
					if (flockDistance < flockingTooClose) {
						collision = collision - (flockFace.transform.position - faceB.transform.position) ;//.125

					}
					flockV =  flockV + flockFace.linearVelocity;
				} // if not this face
    			} // if within sight
    		} // for each face


    		if (faceCount > 0f) {
 //   			centroid = centroid / faceCount;
//    			centroid = (centroid - faceB.transform.position) * 0.01F; // test this factor
//    			flockV= flockV/faceCount;
//    			flockV = (flockV - faceB.rb.linearVelocity)	* 0.025f; // test this factor
//    			flockVector = centroid + collision + flockV;
    			flockVector = collision;
    		}
//    		Debug.LogFormat("collision, {0} face count {1}   centroid {2} flockV {3}", collision, faceCount, centroid, flockV);
    	} // end flock()
    } // end class Drift
} // faceBehaviour class
