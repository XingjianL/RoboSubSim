using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class Robot_UI : MonoBehaviour
{
    private GameObject Robot;
    TCPServer tcp_script;
    RobotForce control_script;
    BuoyancyForces buoyancy_script;
    RobotCamera camera_script;
    RobotIMU imu_script;
    public TMPro.TMP_InputField IPaddr;
    public TMPro.TMP_InputField Port;
    public TMPro.TMP_InputField SendFreq;
    public TMPro.TMP_Text TCPMessage;
    public Toggle runServer;
    public TMPro.TMP_Dropdown controlModeDropdown;
    public TMPro.TMP_InputField Mass;
    public TMPro.TMP_InputField Volume;
    public TMPro.TMP_InputField ImageWidth;
    public TMPro.TMP_InputField ImageHeight;
    public TMPro.TMP_Dropdown cameraModeDropdown;
    
    void Start(){
        refresh();
    }
    public void setNewRobot(){
        if (Robot == null){
            Robot = GameObject.FindGameObjectWithTag("Robot");
            if (Robot == null) {
                return;
            }
        }
        
        tcp_script = Robot.GetComponent<TCPServer>();
        control_script = Robot.GetComponent<RobotForce>();
        buoyancy_script = Robot.GetComponent<BuoyancyForces>();
        camera_script = Robot.GetComponent<RobotCamera>();
        imu_script = Robot.GetComponent<RobotIMU>();
    }
    public void refresh(){
        setNewRobot();

        if (Robot == null) {
            return;
        }
        configTCPScript();
        configRobotParams();
        configControlMode();
        configCamera();
        changeTCPMessage();
    }
    public void changeTCPMessage(){
        TCPMessage.text = tcp_script.ui_message;
    }
    public void configTCPScript(){
        print("UI TOGGLE");
        tcp_script.IPAddr = IPaddr.text;
        tcp_script.port = int.Parse(Port.text);
        tcp_script.runServer = runServer.isOn;
        tcp_script.msPerTransmit = int.Parse(SendFreq.text);
    }
    public void configControlMode(){
        control_script.controlMethod = (RobotForce.controlMode)controlModeDropdown.value;
    }
    public void configRobotParams(){
        control_script.m_rigidBody.mass = float.Parse(Mass.text);
        buoyancy_script.volumeDisplaced = float.Parse(Volume.text);
    }
    public void captureImage(){
        camera_script.generateData = true;
        //camera_script.configCommand(cameraModeDropdown.value);
        camera_script.CommandTrigger(cameraModeDropdown.value);
        //camera_script.renderState = RobotCamera.renderStatesEnum.PreRender;
    }
    public void configCamera(){
        if (camera_script.imgHeight != int.Parse(ImageHeight.text) || camera_script.imgWidth != int.Parse(ImageWidth.text)){
            camera_script.imgHeight = int.Parse(ImageHeight.text);
            camera_script.imgWidth = int.Parse(ImageWidth.text);
            
            // check current tcp server (kill and re-enable on new robot)
            bool hasServer = tcp_script.runServer;
            if (hasServer) {runServer.isOn = false;}
            
            // create a new robot with proper perception camera resolutions
            var tempRobot = Instantiate(Robot, Robot.transform.position, Robot.transform.rotation);
            Destroy(Robot);
            Robot = tempRobot;
            setNewRobot();

            if (hasServer) {runServer.isOn = true;}
            // copy settings to new robot
             
        }
        camera_script.configCommand(cameraModeDropdown.value);
    }
    // Update is called once per frame
    void Update()
    {
        if (Robot != null){
            changeTCPMessage();
        }
    }
}
