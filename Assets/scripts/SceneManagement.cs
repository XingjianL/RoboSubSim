using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
/// <summary>
/// This script handles all objects (with matching tagNames) in the scene so no calls to individual script should be needed
/// Therefore it grabs all objects in the scene and has the ability to copy/remove objects (that exists) in the scene
/// </summary>
public class TCPRobot{
    public GameObject tcpObject;
    public RobotCamera cameraScript;
    public RobotForce controlScript;
    public RobotIMU imuScript;
    public BuoyancyForces buoyScript;
    public Vector3 init_location = new Vector3(0,0,0); // real robot frame
    public Quaternion init_rotation = new Quaternion(0,0,0,1); // real robot frame
    public void setNewRobot(GameObject robot){
        tcpObject = robot;
        cameraScript = robot.GetComponent<RobotCamera>();
        controlScript = robot.GetComponent<RobotForce>();
        imuScript = robot.GetComponent<RobotIMU>();
        buoyScript = robot.GetComponent<BuoyancyForces>();
    }
        /// coords in real robot frame
    public void setRobotPos(){
        tcpObject.transform.position = new Vector3(init_location.y,init_location.z,-init_location.x);
    }
    public void setRobotRot(){
        tcpObject.transform.rotation = init_rotation;
    }
}

[RequireComponent (typeof(TCPServer))]
public class SceneManagement : MonoBehaviour
{
    public Robot_UI ui_script;
    public TextureRandomization textureRandomizationScript;
    const int ROBOT = 0;
    const int POOL = 1;
    const int POOLMESH = 4;
    string[] tagNames = {"Robot", "Pool", "2023Objective", "Environment", "PoolMesh"};
    List<GameObject[]> gameObjects = new List<GameObject[]>();
    List<TCPRobot> allRobots = new List<TCPRobot>();
    int tagSelect;
    int objectSelect;
    public TCPServer tcpServer;
    public TCPRobot tcpRobot;
    public int tcpTagSelect;
    public int tcpObjectSelect;
    public string[] sceneLists = new string[] { "Scenes/LobbyScene",
                                                "Scenes/OutdoorsScene",
                                                "Scenes/OutdoorsScene"};
    public bool sceneRefresh = false;
    public int sceneSelect = 0;
    public bool simCBRefresh = false;
    public bool simCBToggle = false;
    public bool robotCFGRefresh = false;
    public float[] robotCFG = {-1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f};
    public bool camCFGRefresh = false;
    public int[] camCFG = {640, 480, 0};
    public byte camCFG_Effects = 0;
    public bool rgbScreenResizeToggle;
    public bool ShowGUIToggle;
    public byte sceneToggles;
    public bool sceneTogglesRefresh;
    public bool refreshRobotPos;
    public short scatterColorBias;
    public List<string> allCommandsReceived = new List<string>();
    public IEnumerator ResetSceneCoroutine(){
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneLists[sceneSelect]);
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone){
            yield return new WaitForSeconds(0.1f);
        }

        ui_script.refresh();
    }
    public void ResetScene(){
        StartCoroutine(ResetSceneCoroutine());
        sceneRefresh = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        setupTCPServer();
    }
    // Update is called once per frame
    // Mark: Update
    void Update()
    {
        if (sceneRefresh) {
            ResetScene();
        }
        if (simCBRefresh){
            simCBRefresh = false;
            setupSimCBConnect(simCBToggle);
        }
        if (robotCFGRefresh){
            robotCFGRefresh = false;
            configRobotParams(
                mass:   robotCFG[0],
                volume: robotCFG[1],
                ldrag:  robotCFG[2],
                adrag:  robotCFG[3],
                f_KGF:  robotCFG[4],
                r_KGF:  robotCFG[5]
                );
        }
        if (camCFGRefresh){
            camCFGRefresh = false;
            configRobotCamera(height: camCFG[0], width: camCFG[1], mode: camCFG[2]);
            setPoolPostProcesses(camCFG_Effects);
        }
        if (sceneTogglesRefresh){
            sceneTogglesRefresh = false;
            configScene();
        }
        if (refreshRobotPos){
            refreshRobotPos = false;
            foreach (var _rob in allRobots)
            {
                _rob.setRobotPos();
                _rob.setRobotRot();
            }
        }
    }
    /// <summary>
    /// TCP Server stuff
    /// </summary>
    void loadTCPServer(){
        tcpServer = GetComponent<TCPServer>();
    }
    public void setupTCPServer( string IPAddr = "127.0.0.1", 
                                int port = 1234, 
                                bool runServer = false, 
                                float msPerTransmit = 5.0f){
        loadTCPServer();
        tcpServer.IPAddr = IPAddr;
        tcpServer.port = port;
        tcpServer.runServer = runServer;
        tcpServer.msPerTransmit = msPerTransmit;

        ui_script.IPaddr.text = IPAddr;
        ui_script.Port.text = port.ToString();
        ui_script.runServer.isOn = runServer;
        ui_script.SendFreq.text = msPerTransmit.ToString();
    }
    public void setupSimCBConnect(bool simCB_Connect){
        Debug.Log("SimCB Connect Attempt: " + simCB_Connect);
        tcpServer.simCB_Connect = simCB_Connect;
        ui_script.runSimCB.isOn = simCB_Connect;
    }
    /// <summary>
    /// Robot Dynamics Configurations
    /// </summary>
    public void configRobotControlMode(int mode, int robotID = 0, bool tcp = false){
        configRobotControlMode((RobotForce.controlMode) mode, robotID, tcp);
    }
    public void configRobotControlMode(RobotForce.controlMode mode, int robotID = 0, bool tcp = false){
        GameObject robot = selectObject(ROBOT, robotID);
        RobotForce script = allRobots[robotID].controlScript;
        script.controlMethod = mode;

        ui_script.controlModeDropdown.value = (int)mode;
    }
    public void configRobotParams(float mass = -1, 
                                    float volume = -1, 
                                    float ldrag = -1,
                                    float adrag = -1,
                                    float f_KGF = -1, 
                                    float r_KGF =-1, 
                                    int robotID = 0, 
                                    bool tcp = false){
            //GameObject robot = selectObject(ROBOT, robotID);
        if (mass > 0) {
            allRobots[robotID].controlScript.m_rigidBody.mass = mass;
            ui_script.Mass.text = mass.ToString();
        }
        if (volume > 0) {
            allRobots[robotID].buoyScript.volumeDisplaced = volume;
            ui_script.Volume.text = volume.ToString();
        }
        allRobots[robotID].controlScript.set_motor_cfg(f_KGF, r_KGF);
        if (ldrag > 0){
            allRobots[robotID].controlScript.m_rigidBody.drag = ldrag;
        }
        if (adrag > 0){
            allRobots[robotID].controlScript.m_rigidBody.angularDrag = adrag;
        }
    }
    public void configRobotCamera(int height = -1, int width = -1, int mode = -1, int robotID = 0){
        GameObject robot = selectObject(ROBOT, robotID);
        RobotCamera script = allRobots[robotID].cameraScript;
        if (rgbScreenResizeToggle){
            Screen.SetResolution(width, height, false);
        }
        if (height > 0 && width > 0){
            if (script.imgHeight != height || script.imgWidth != width){
                script.imgHeight = height;
                script.imgWidth = width;
                ui_script.ImageHeight.text = script.imgHeight.ToString();
                ui_script.ImageWidth.text = script.imgWidth.ToString();
                // check current tcp server (kill and re-enable on new robot)
                //bool hasServer = tcpServer.runServer;
                //if (hasServer) {setupTCPServer(tcpServer.IPAddr, tcpServer.port, false, tcpServer.msPerTransmit);}

                // create a new robot with proper perception camera resolutions
                GameObject newrobot = copyNewObject(robot);
                Destroy(robot);
                replaceObjectInArray(newrobot, ROBOT, robotID);
                script = newrobot.GetComponent<RobotCamera>();
                
                
            }
            //
            
            
        }
        
        if (ShowGUIToggle){
            script.ShowGUI = true;
        }
        if (mode > 0){
            script.configCommand(mode);
            ui_script.cameraModeDropdown.value = (int)script.currentCommand;
        }
        
        
    }
    public int getCameraMode(int robotID = 0){
        GameObject robot = selectObject(ROBOT, robotID);
        RobotCamera script = allRobots[robotID].cameraScript;
        return (int)script.currentCommand;
    }
    /// <summary>
    /// Robot Actions
    /// </summary>
    public void captureImage(int mode, int robotID = 0){
        //GameObject robot = selectObject(ROBOT, robotID);
        RobotCamera script = allRobots[robotID].cameraScript;
        script.generateData = true;
        //camera_script.configCommand(cameraModeDropdown.value);
        script.CommandTrigger(mode);
        //camera_script.renderState = RobotCamera.renderStatesEnum.PreRender;
    }
    public void triggerCapture(int mode, int robotID = 0){
        //GameObject robot = selectObject(ROBOT, robotID);
        RobotCamera script = allRobots[robotID].cameraScript;
        script.CommandTrigger(mode);
    }
    public void setMotorPower(  float m1, float m2, float m3, float m4,
                                float m5, float m6, float m7, float m8,
                                int robotID = 0){
        //GameObject robot = selectObject(ROBOT, robotID);
        RobotForce script = allRobots[robotID].controlScript;
        script.thrust_strengths[0] = m1;
        script.thrust_strengths[1] = m2;
        script.thrust_strengths[2] = m3;
        script.thrust_strengths[3] = m4;
        script.thrust_strengths[4] = m5;
        script.thrust_strengths[5] = m6;
        script.thrust_strengths[6] = m7;
        script.thrust_strengths[7] = m8;
        //print(allRobots.Count);
        //print(script.controlMethod);
    }
    public void setOtherControlPower(   float m1, float m2, float m3, float m4,
                                        float m5, float m6, int robotID = 0){
        //GameObject robot = selectObject(ROBOT, robotID);
        RobotForce script = allRobots[robotID].controlScript;
        script.other_control[0] = m1;
        script.other_control[1] = m2;
        script.other_control[2] = m3;
        script.other_control[3] = m4;
        script.other_control[4] = m5;
        script.other_control[5] = m6;
    }
    public IMU getRobotIMU(int robotID = 0){
        //GameObject robot = selectObject(ROBOT, robotID);
        RobotIMU script = allRobots[robotID].imuScript;
        return script.imu;
    }
    public RobotForce getRobotForces(int robotID = 0){
        RobotForce script = allRobots[robotID].controlScript;
        return script;
    }
    public void setRobotPos(float x, float y, float z, int robotID = 0){
        TCPRobot robot = allRobots[robotID];
        robot.init_location = new Vector3(x, y, z);
    }
    public void setRobotRot(float x, float y, float z, int robotID = 0){
        TCPRobot robot = allRobots[robotID];
        robot.init_rotation = Quaternion.Euler(x, y, z);
    }
    public void setRobotRandPose(float xp, float yp, float zp, float xr, float yr, float zr, int robotID = 0) {
        TCPRobot robot = allRobots[robotID];
        var rand = new System.Random();
        robot.init_location = new Vector3(2*xp*((float)rand.NextDouble()-0.5f), 2*yp*((float)rand.NextDouble()-0.5f), System.Math.Clamp(2*zp*((float)rand.NextDouble()-0.5f), -10.0f, -0.5f));
        robot.init_rotation = Quaternion.Euler(2*xr*((float)rand.NextDouble()-0.5f), 2*yr*((float)rand.NextDouble()-0.5f), 2*zr*((float)rand.NextDouble()-0.5f));
    }
    ///
    /// 
    /// 
    public void configScene(){
        if ((sceneToggles & 0b0000_0001) != 0){
            poolColorRandom();
        }
        if ((sceneToggles & 0b0000_0010) != 0){
            togglePhysics(true);
        } else {togglePhysics(false);}
        if ((sceneToggles & 0b0000_0100) != 0){
            // random pool textures
            RandomizeMaterial(selectObject(POOLMESH, 0), 1);
        }
        if ((sceneToggles & 0b0000_1000) != 0){
            // random caustics
        }
    }
    public void setPoolPostProcesses(byte toggles){
        GameObject pool = selectObject(POOL, 0);
        Volume poolPostProcess = pool.GetComponentInChildren<Volume>();
        Debug.Log(poolPostProcess);
        if ((toggles & 0b0000_0001) > 0){
            if (poolPostProcess.profile.TryGet<LensDistortion>(out var ld)){ld.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<LensDistortion>(out var ld)){ld.active = false;}
        }
        if ((toggles & 0b0000_0010) > 0){
            if (poolPostProcess.profile.TryGet<Fog>(out var f)){f.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<Fog>(out var f)){f.active = false;}
        }
        if ((toggles & 0b0000_0100) > 0){
            if (poolPostProcess.profile.TryGet<ChromaticAberration>(out var ca)){ca.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<ChromaticAberration>(out var ca)){ca.active = false;}
        }
        if ((toggles & 0b0000_1000) > 0){
            if (poolPostProcess.profile.TryGet<FilmGrain>(out var fg)){fg.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<FilmGrain>(out var fg)){fg.active = false;}
        }
        if ((toggles & 0b0001_0000) > 0){
            if (poolPostProcess.profile.TryGet<Bloom>(out var b)){b.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<Bloom>(out var b)){b.active = false;}
        }
        if ((toggles & 0b0010_0000) > 0){
            if (poolPostProcess.profile.TryGet<ScreenSpaceLensFlare>(out var lf)){lf.active = true;}
        } else {
            if (poolPostProcess.profile.TryGet<ScreenSpaceLensFlare>(out var lf)){lf.active = false;}
        }
    }

    /// <summary>
    /// Load and Debug Scene
    /// </summary>
    public void registerAllSceneObjects(){
        //int tagNum = 0;
        gameObjects.Clear();
        foreach (string tag in tagNames){
            gameObjects.Add(GameObject.FindGameObjectsWithTag(tag));
            if (System.String.Equals(tag, tagNames[ROBOT])) {
                allRobots.Clear();
                foreach (GameObject rob in GameObject.FindGameObjectsWithTag(tag)){
                    TCPRobot cur = new TCPRobot();
                    cur.setNewRobot(rob);
                    allRobots.Add(cur);
                }

            }
        }
    }
    public void displayAllRegisteredObjectsNames(){
        int tagNum = 0;
        foreach (GameObject[] objects in gameObjects){
            foreach(GameObject obj in objects){
                print(tagNames[tagNum] + ": " + obj);
            }
            tagNum+=1;
        }
    }
    public int getRobotCount(){
        return allRobots.Count;
    }
    /// <summary>
    /// Scene Object Utility
    /// </summary>
    GameObject copyNewObject(GameObject existingObject){
        GameObject newObject = Instantiate( existingObject, 
                                            existingObject.transform.position, 
                                            existingObject.transform.rotation);
        return newObject;
    }
    GameObject selectObject(int tagID, int objectID){
        
        if (tagID < gameObjects.Count && objectID < gameObjects[tagID].Length){
            tagSelect = tagID;
            objectSelect = objectID;
            return gameObjects[tagID][objectID];
        }
        return null;
    }
    GameObject selectObject(string tag, int objectID){
        int tagID = System.Array.IndexOf(tagNames, tag);
        if (tagID < 0) {return null;}
        return selectObject(tagID, objectID);
    }
    void replaceObjectInArray(GameObject newObject, int tagID, int objectID){
        if(tagID == ROBOT){
            allRobots[objectID].setNewRobot(newObject);
        }
        gameObjects[tagID][objectID] = newObject;
    }
    void replaceObjectInArray(GameObject newObject, string tag, int objectID){
        int tagID = System.Array.IndexOf(tagNames, tag);
        replaceObjectInArray(newObject, tagID, objectID);
    }
    public void poolColorRandom(){
        var waterBodies = GameObject.FindGameObjectsWithTag("WaterColor");
        foreach(GameObject waterBody in waterBodies){
            waterBody.GetComponent<WaterRandomization>().RandomizeWater();
            waterBody.GetComponent<WaterRandomization>().scatterColorBias = scatterColorBias;
        }
    }
    public void setPoolWaterColor(int blue, int green, float brightness){
        var waterBodies = GameObject.FindGameObjectsWithTag("WaterColor");
        foreach(GameObject waterBody in waterBodies){
            waterBody.GetComponent<WaterRandomization>().SetWaterColor(blue, green, brightness);
        }
    }
    public void RandomizeMaterial(GameObject obj, int typeMat){
        textureRandomizationScript.RandomizeMaterial(obj, typeMat);
    }
    public void togglePhysics(bool On){
        Rigidbody[] allRigidBodies = GameObject.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach(Rigidbody rigidbody in allRigidBodies){
            rigidbody.isKinematic = !On;
        }
    }
    public void setDisplayCamera(bool ShowGUI){
        allRobots[0].cameraScript.ShowGUI = ShowGUI;
    }
}
