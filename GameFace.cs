// manage the faces in the game
using UnityEngine;
using UnityEditor;
using System.Collections.Generic; // Required for List
using System.IO; // Required for file operations

public class GameFace: MonoBehaviour {
    public List<Rigidbody> faceBook = new List<Rigidbody>(); 
    private int maxNumberFaces = 8;
    public Material faceMaterial;
    public string faceDBPath; 
    
    void Start()
    {
    	Debug.LogWarning("starting start");
    	
    	for (int i = 1; i < Display.displays.Length; i++) {
            Display.displays[i].Activate();
        }
        faceDBPath = "/media/paulvr/LaCie/activeProjects/faceDB";
        // make faces after 2 seconds, then every 1 second thereafter
//        InvokeRepeating("MakeFacesLoop", 2f, 1f);
        // look for a share file from the python components
        InvokeRepeating("LoopReadUpdates", 2.1f, 1.0f);
        faceMaterial = Resources.Load<Material>("faceMaterial");
    }
    private float newFaceWaitTime = 2.17f;
    private float newFaceTimer = 1.17f;
    private GameObject thisFace;
    private faceBehaviour thisFaceBehavior;
    private Rigidbody rb;

    void Update() { 
    // quit on esc key
        if (Input.GetKeyDown(KeyCode.Escape))   {
            Application.Quit();
        }
    // remove inactive faces
	newFaceTimer -= Time.deltaTime;
	if (newFaceTimer < 0f) {
		newFaceTimer = newFaceWaitTime;
	// check the faces for an inactive one
        	foreach(Rigidbody bookFace in faceBook) {
        		if (bookFace.gameObject.activeSelf == false) {
        		        int faceCount = faceBook.Count;
                                // make sure we leave a few floating around
				if (faceCount < (maxNumberFaces - 1)) {
				    bookFace.gameObject.SetActive(true);
				} else { // delete the file and clean up face book
				    string filePath = faceDBPath + "/currentPhotos/" + bookFace.gameObject.name + ".png";
				    filePath.Replace("clr", string.Empty);
				    filePath.Replace("facePlane", string.Empty);
				    Debug.LogWarning("deleting " + bookFace.gameObject.name + " and file " + filePath);
				    File.Delete(filePath);
        			    faceBook.Remove(bookFace);
        			    Destroy(bookFace.gameObject);
        			    Destroy(bookFace);
        			}
        			break; // only do one at a time
        		}
        	}
	}// timer to add faces
    } // Update

    public Material planeMaterial; 

    GameObject GenerateFacePlane(string objName)  {
        // Create a new GameObject for the plane
        GameObject facePlaneObject = new GameObject(objName);
        facePlaneObject.transform.parent = this.transform; // Make it a child of this object

        // Add MeshFilter and MeshRenderer components
        MeshFilter meshFilter = facePlaneObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = facePlaneObject.AddComponent<MeshRenderer>();

        // Assign the material
        if (planeMaterial != null)  {
            meshRenderer.material = planeMaterial;
        } else {
            Debug.LogWarning("No face plane material assigned " + objName);
        }
        //  Assign the Plane mesh
        GameObject tempPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        meshFilter.mesh = tempPlane.GetComponent<MeshFilter>().mesh;
        Destroy(tempPlane); // Clean up the temporary object
        
        return(facePlaneObject);
    } // end GenerateFacePlane
    
    public string newPortraitsDir; 
    // read the new portraits directory to find the next face
    void LoopReadUpdates()   {
        string listDir = "/newList";
        string saveListDir = listDir + "Save";
        newPortraitsDir =  faceDBPath + listDir;
        DirectoryInfo dirInfo = new DirectoryInfo(newPortraitsDir);
        FileInfo[] files = dirInfo.GetFiles();
        if (files.Length > 0) {
            string faceID = Path.GetFileNameWithoutExtension(files[0].Name);
            string imagePath = files[0].FullName;
            rb = MakeFace(faceID, imagePath);
            if (rb != null) {
	// put the face into the FaceBook
	        faceBook.Add(rb);
                Debug.Log("added face file " + faceID);
                // move the new image to save location
                string targetPath = imagePath.Replace(listDir, saveListDir);
                System.IO.File.Move(imagePath, targetPath);
            } else {
                Debug.Log("could NOT add face file " + faceID);
            }
        } else {
	    Debug.Log("no new faces");
        }
    } // end loopReadUpdates
    // make a face warts and all
    Rigidbody MakeFace(string faceID, string imagePath) {
        string objName = "facePlane" + faceID;
        GameObject newFace = GenerateFacePlane(objName);
        if (newFace != null) {
        	newFace.SetActive(true);
                newFace.AddComponent<faceBehaviour>();
                MeshRenderer meshRenderer = newFace.GetComponent<MeshRenderer>();
		meshRenderer.material = faceMaterial;
		Debug.Log("trying meshing with " + imagePath);
                byte[] bytes = File.ReadAllBytes(imagePath);
                Texture2D faceTexture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(faceTexture, bytes)) {
//                Texture2D faceTexture = Resources.Load<Texture2D>(imageName);
        	    if (faceTexture != null) {
		        meshRenderer.material.mainTexture = faceTexture;
		    } else {
		        Debug.Log("could not make a texture " + imagePath);
		        return(null);
		    }
		}
		// rotate to face viewer
		newFace.transform.rotation = Quaternion.Euler(new Vector3(-90.0f, 0.0f, 0.0f));
		// make a rigidbody to allow physics
		Rigidbody rb = newFace.AddComponent<Rigidbody>();
		if (rb != null) {
        		rb.mass = 1.0f;
        		rb.linearDamping = 0.0f;
        		rb.angularDamping = 0.00f;
        		rb.useGravity = false;
        		rb.isKinematic = false;
        		return(rb);
        	}
	} // if new face
	return(null); // failed to make a face
    } // end MakeFace
    
} // end GameFace 
