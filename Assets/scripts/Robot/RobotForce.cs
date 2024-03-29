using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RobotForce : MonoBehaviour
{
    private RobotIMU imu_script;
    public GameObject[] thrusters;
    public float forward_KGF = 2.36f;
    public float reverse_KGF = 1.85f;
    public bool forceVisual = true;
    public Material forceVisualMaterial;
    [HideInInspector] public Rigidbody m_rigidBody;
    const float KGF_TO_N = 9.80665f;
    float MAX_FORCE = 2.36f*KGF_TO_N;
    public int[] robot_unity_mapping; // whats the motor id in the real robot w.r.t. the robot in documentation
    public int[] unity_inverts; // whats the direction [1 or -1] of the force w.r.t. the robot in documentation
    public float[] thrust_strengths = {0f,0f,0f,0f,0f,0f,0f,0f};

    // unity_robot_transform:realrobot, [right:front,forward:left,up:up,r,p,y]
    public float[] other_control = {0f,0f,0f,0f,0f,0f}; // location, rotation (position(m, r), velocity(m/s, r/s), force_strength[-1,1])

    [HideInInspector] public int WDogStatus;
    [HideInInspector] public int MotorMode;
    float timer = 0;
    float random_thrust = 0;
    public enum controlMode : int {
        Raw = 0,
        Local = 1,
        Global = 2,
        motors = 0,
        ForceTorque =       3,  // 1st, 1st order
        ForceVelocity =     4,  // 1st, 2nd order
        ForceDegree =       5,  // 1st, 3rd order
        VelocityDegree =    6,  // 2nd, 1st order
        VelocityVelocity =  7,  // 2nd, 2th order
        VelocityTorque =    8,  // 2nd, 3rd order
        PositionDegree =    9,  // 1st, 1st order
        PositionVelocity =  10, // 1st, 2nd order
        PositionTorque =    11  // 1st, 3rd order
    }
    [HideInInspector]
    public controlMode controlMethod = controlMode.motors;
    // Start is called before the first frame update
    void Start()
    {
        imu_script = GetComponent<RobotIMU>();
        m_rigidBody = GetComponent<Rigidbody>();
        if (forceVisualMaterial != null){
            foreach (var thruster in thrusters) {
                thruster.transform.Find("force_visual").gameObject.GetComponent<MeshRenderer>().material = forceVisualMaterial;
            }
        }
        if (!forceVisual){
            foreach (var thruster in thrusters) {
                thruster.transform.Find("force_visual").gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        stop_thrust();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer > 2) {
            timer = 0;
            random_thrust = Random.Range(-1f,1f);
        }
        timer += Time.deltaTime;
    }
    public void set_motor_cfg(float f_KGF, float r_KGF){
        if (f_KGF > 0){
            forward_KGF = f_KGF;
            MAX_FORCE = forward_KGF * KGF_TO_N;
        }
        if (r_KGF > 0){
            reverse_KGF = r_KGF;
        }
    }
    // t1-8 from [-1,1] please
    private void set_thrusts_strengths(){
        //Debug.Log("Inverts: " + System.String.Join(",",unity_inverts) + ", Mapping: " + System.String.Join(",",robot_unity_mapping));
        add_thruster_force(0, thrust_strengths[robot_unity_mapping[0]-1] * forward_KGF * KGF_TO_N, unity_inverts[0]);
        add_thruster_force(1, thrust_strengths[robot_unity_mapping[1]-1] * forward_KGF * KGF_TO_N, unity_inverts[1]);
        add_thruster_force(2, thrust_strengths[robot_unity_mapping[2]-1] * forward_KGF * KGF_TO_N, unity_inverts[2]);
        add_thruster_force(3, thrust_strengths[robot_unity_mapping[3]-1] * forward_KGF * KGF_TO_N, unity_inverts[3]);
        add_thruster_force(4, thrust_strengths[robot_unity_mapping[4]-1] * forward_KGF * KGF_TO_N, unity_inverts[4]);
        add_thruster_force(5, thrust_strengths[robot_unity_mapping[5]-1] * forward_KGF * KGF_TO_N, unity_inverts[5]);
        add_thruster_force(6, thrust_strengths[robot_unity_mapping[6]-1] * forward_KGF * KGF_TO_N, unity_inverts[6]);
        add_thruster_force(7, thrust_strengths[robot_unity_mapping[7]-1] * forward_KGF * KGF_TO_N, unity_inverts[7]);
    }
    private void set_body_force(){
        var front_force = 4/Mathf.Sqrt(2)*limit_thruster_force(other_control[1] * forward_KGF * KGF_TO_N);
        var side_force = 4/Mathf.Sqrt(2)*limit_thruster_force(other_control[0] * forward_KGF * KGF_TO_N);
        var up_force = 4*limit_thruster_force(other_control[2] * forward_KGF * KGF_TO_N);
        m_rigidBody.AddForce(transform.right    * front_force); // go forward
        m_rigidBody.AddForce(-transform.forward * side_force); // go right
        m_rigidBody.AddForce(transform.up       * up_force); // go up
        //print(up_force);
    }
    private void set_body_torque(){
        var x_torque = 4*.2f*limit_thruster_force(other_control[3] * forward_KGF * KGF_TO_N);
        var y_torque = 4*.2f*limit_thruster_force(other_control[4] * forward_KGF * KGF_TO_N);
        var z_torque = 4*.25f*limit_thruster_force(other_control[5] * forward_KGF * KGF_TO_N);
        m_rigidBody.AddTorque(transform.right    * -y_torque); // rotate roll
        m_rigidBody.AddTorque(-transform.forward * -x_torque); // rotate pitch
        m_rigidBody.AddTorque(transform.up       * -z_torque); // rotate yaw
    }
    private void set_body_velocity(){
        // unity_robot:[x:front,z:left,y:up,r,p,y]
        
        Vector3 world_direction = transform.TransformDirection(new Vector3(other_control[1], other_control[2], -other_control[0]));// local -> world
        m_rigidBody.velocity = world_direction;
    }
    private void set_body_angular_velocity(){
        Vector3 world_direction = transform.TransformDirection(new Vector3(other_control[3], other_control[5], -other_control[4]));
        m_rigidBody.angularVelocity = world_direction;  // unity does not recommend setting angular velocity directly (recommend to set torque or rotation)
    }
    void set_body_position(){
        transform.position = new Vector3(other_control[1], other_control[2], -other_control[0]); // true world position (no good way of factoring imu noise)
    }
    void set_body_rotation(){
        //print(imu_script.imu.accumulatedGyroDrift.eulerAngles);
        transform.rotation = Quaternion.Euler(new Vector3(-other_control[4], -other_control[5], other_control[3]))
                            * imu_script.imu.accumulatedGyroDrift;  // imu drift factor
    }
    void stop_thrust(){
        add_thruster_force(0, 0);
        add_thruster_force(1, 0);
        add_thruster_force(2, 0);
        add_thruster_force(3, 0);
        add_thruster_force(4, 0);
        add_thruster_force(5, 0);
        add_thruster_force(6, 0);
        add_thruster_force(7, 0);
    }
    void submerge(float force){
        add_thruster_force(0, -force);
        add_thruster_force(1, -force);
        add_thruster_force(2, -force);
        add_thruster_force(3, -force);   
    }
    void forward(float force){
        add_thruster_force(4, force);
        add_thruster_force(5, force);
        add_thruster_force(6, -force);
        add_thruster_force(7, -force);
    }

    /// force: positive CCW
    void spin(float force){
        add_thruster_force(4, force );
        add_thruster_force(5, -force);
        add_thruster_force(6, force );
        add_thruster_force(7, -force);
    }
    float limit_thruster_force(float force){
        // max forward/reverse spec sheet: https://www.thingbits.in/products/t100-thruster-no-esc
        if (force < 0) {
            force = force * reverse_KGF / forward_KGF; 
        }
        if (force > MAX_FORCE) {
            force = MAX_FORCE;
        }
        if (force < -MAX_FORCE * reverse_KGF / forward_KGF) {
            force = -MAX_FORCE * reverse_KGF / forward_KGF;
        }
        return force;
    }
    void add_thruster_force(int id, float force, int unity_direction=1){
        force = unity_direction * limit_thruster_force(force);
        
        m_rigidBody.AddForceAtPosition(force * thrusters[id].transform.forward, thrusters[id].transform.position);
        if (forceVisual) {
            Transform force_visual = thrusters[id].transform.Find("force_visual");
            if (force_visual != null) {
                //print(force_visual.position);
                //if (force < 0){
                //    force_visual.localRotation = Quaternion.Euler(180,0,0);
                //}
                //force_visual.localPosition = new Vector3(0,0,(force/2)/MAX_FORCE);
                var arrow_scale = (force)/MAX_FORCE;
                force_visual.localScale = new Vector3(arrow_scale/5,arrow_scale/5,arrow_scale);
            }
        }
    }

    void FixedUpdate(){
        //print(transform.rotation.eulerAngles); // (roll,yaw,pitch)
        //SubmergeDepth = GetComponent<buoyancy_forces>().underwater;
        switch (controlMethod) {
            case controlMode.motors:    // Raw
                set_thrusts_strengths();
                break;
            case controlMode.ForceTorque: // local
                set_body_force();
                set_body_torque();
                break;
            case controlMode.VelocityDegree:
                set_body_velocity();
                set_body_rotation();
                break;
            case controlMode.Global:
                print("Global mode not implemented yet");
                break;
        }
            //print(thrust_strengths);
        //forward_force(strength * m_rigidBody.mass);

    }
}
